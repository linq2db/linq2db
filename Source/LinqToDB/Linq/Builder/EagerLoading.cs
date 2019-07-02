using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Security;
using System.Threading;
using LinqToDB.Common;
using LinqToDB.Common.Internal;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;
using LinqToDB.Tools;

namespace LinqToDB.Linq.Builder
{
	class EagerLoading
	{
		private static readonly MethodInfo[] _tupleConstructors = 
		{
			MemberHelper.MethodOf(() => Tuple.Create(0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
			MemberHelper.MethodOf(() => Tuple.Create(0, 0, 0, 0, 0, 0, 0, 0)).GetGenericMethodDefinition(),
		};

		private static readonly MethodInfo EnlistEagerLoadingFunctionalityMethodInfo = MemberHelper.MethodOf(() =>
			EnlistEagerLoadingFunctionality<int, int, int>(null, null, null, null, null)).GetGenericMethodDefinition();

		private static readonly MethodInfo EnlistEagerLoadingFunctionalityDetachedMethodInfo = MemberHelper.MethodOf(() =>
			EnlistEagerLoadingFunctionalityDetached<int>(null, null)).GetGenericMethodDefinition();

		//TODO: move to common static class
		static readonly MethodInfo _whereMethodInfo =
			MemberHelper.MethodOf(() => LinqExtensions.Where<int,int,object>(null,null)).GetGenericMethodDefinition();

		//TODO: move to common static class
		static readonly MethodInfo _getTableMethodInfo =
			MemberHelper.MethodOf(() => DataExtensions.GetTable<object>(null)).GetGenericMethodDefinition();

		static readonly MethodInfo _queryableMethodInfo =
			MemberHelper.MethodOf<IQueryable<bool>>(n => n.Where(a => a)).GetGenericMethodDefinition();

		static readonly MethodInfo _selectManyMethodInfo =
			MemberHelper.MethodOf<IQueryable<object>>(n => n.SelectMany(a => new object[0], (m, d) => d)).GetGenericMethodDefinition();

		static readonly MethodInfo _asQueryableMethodInfo =
			MemberHelper.MethodOf<IEnumerable<object>>(n => n.AsQueryable()).GetGenericMethodDefinition();

		static int _masterParamcounter;

		static string GetMasterParamName(string prefix)
		{
			var idx = Interlocked.Increment(ref _masterParamcounter);
			return prefix + idx;
		}

		class EagerLoadingContext<T, TKey>
		{
			private Dictionary<TKey, List<T>> _items;
			private TKey _prevKey;
			private List<T> _prevList;

			public void Add(TKey key, T item)
			{
				List<T> list;

				if (_prevList != null && _prevKey.Equals(key))
				{
					list = _prevList;
				}
				else
				{
					if (_items == null)
					{
						_items = new Dictionary<TKey, List<T>>();
						list = new List<T>();
						_items.Add(key, list);
					}
					else if (!_items.TryGetValue(key, out list))
					{
						list = new List<T>();
						_items.Add(key, list);
					}

					_prevKey = key;
					_prevList = list;
				}

				list.Add(item);
			}

			public List<T> GetList(TKey key)
			{
				if (_items == null || !_items.TryGetValue(key, out var list))
					return new List<T>();
				return list;
			}
		}

		private static Expression GenerateKeyExpression(Expression[] members, int startIndex)
		{
			var count = members.Length - startIndex;
			if (count == 0)
				throw new ArgumentException();

			if (count == 1)
				return members[startIndex];

			Expression[] arguments;

			if (count > 8)
			{
				count = 8;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var constructor = _tupleConstructors[count - 2];

			var typedConstructorPlain = constructor.MakeGenericMethod(arguments.Select(a => a.Type).ToArray());

			return Expression.Call(typedConstructorPlain, arguments);
		}

		public static bool IsDetailsMember(MemberInfo memberInfo)
		{
			var memberType = memberInfo.GetMemberType();
			if (memberType != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(memberType))
				return true;
			return false;
		}

		public static Type GetEnumerableElementType(Type type)
		{
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return type;
			if (type.IsArray)
				return type.GetElementType();
			return type.GetGenericArguments()[0];
		}

		static Expression GenerateKeyExpressionForDetails(IBuildContext context, ParameterExpression mainObj)
		{
			var sql = context.ConvertToSql(null, 0, ConvertFlags.Key);

			//TODO: more correct memberchain processing

			var members = sql.Where(s => s.MemberChain.Count == 1)
				.Select(s => Expression.MakeMemberAccess(mainObj, s.MemberChain[0]))
				.ToArray();

			if (members.Length == 0)
				throw new InvalidOperationException();

			var expr = GenerateKeyExpression(members, 0);

			return expr;
		}


		static Expression ConstructMemberPath(List<MemberInfo> memberPath, Expression ob)
		{
			if (memberPath.Count == 0)
				return null;

			Expression result = ob;
			for (int i = 0; i < memberPath.Count; i++)
			{
				var memberInfo = memberPath[i];
				if (result.Type != memberInfo.DeclaringType)
					return null;
				result = Expression.MakeMemberAccess(result, memberInfo);
			}

			return result;
		}

		static Tuple<Expression, Expression> GenerateKeyExpressions(List<Tuple<Expression, MemberExpression>> foundMembers, ParameterExpression param, ExpressionBuilder builder, params IBuildContext[] contexts)
		{
			var memberOfProjection = new List<Expression>();
			var memberOfDetail = new List<Expression>();

			// try to find fields
			foreach (var member in foundMembers)
			{
				IBuildContext ctx = null;
				foreach (var context in contexts)
				{
					ctx = builder.GetContext(context, member.Item1);
					if (ctx != null)
						break;
				}
				if (ctx == null)
					continue;

				var fieldsSql = ctx.ConvertToIndex(member.Item1, 0, ConvertFlags.Field);
				if (fieldsSql.Length == 1)
				{
					var s = fieldsSql[0];

					Expression forDetail = member.Item2;

					var forProjection = ctx.Builder.BuildSql(s.Sql.SystemType, s.Index);

					if (forDetail.Type != forProjection.Type)
						forDetail = Expression.Convert(forDetail, forProjection.Type);

					memberOfDetail.Add(forDetail);
					memberOfProjection.Add(forProjection);
				}
			}

			if (memberOfDetail.Count == 0)
			{
				// add fake one
				var zero = Expression.Constant(0);
				memberOfDetail.Add(zero);
				memberOfProjection.Add(zero);
			}

			var exprProjection = GenerateKeyExpression(memberOfProjection.ToArray(), 0);
			var expDetail      = GenerateKeyExpression(memberOfDetail.ToArray(), 0);

			return Tuple.Create(exprProjection, expDetail);
		}

		static Tuple<Expression, Expression> GenerateKeyExpressionsNew(IBuildContext context, Expression expr, Expression path)
		{
			var sql = context.ConvertToIndex(expr, 0, ConvertFlags.Key);

			var memberOfProjection = new List<Expression>();
			var memberOfDetail = new List<Expression>();
			foreach (var s in sql)
			{
				Expression forDetail = null;

				forDetail = ConstructMemberPath(s.MemberChain, path);

				if (forDetail == null)
					continue;

				var forProjection = context.Builder.BuildSql(s.Sql.SystemType, s.Index);
				memberOfProjection.Add(forProjection);

				//TODO: more correct memberchain processing

				if (forDetail.Type != forProjection.Type)
					forDetail = Expression.Convert(forDetail, forProjection.Type);
				memberOfDetail.Add(forDetail);
			}

			if (memberOfDetail.Count == 0)
			{
				// add fake one
				var zero = Expression.Constant(0);
				memberOfDetail.Add(zero);
				memberOfProjection.Add(zero);
			}

			var exprProjection = GenerateKeyExpression(memberOfProjection.ToArray(), 0);
			var expDetail      = GenerateKeyExpression(memberOfDetail.ToArray(), 0);

			return Tuple.Create(exprProjection, expDetail);
		}

		static IEnumerable<Tuple<Expression, MemberExpression>> GenerateSelectKeyExpression(Expression tupleExpression, ParameterExpression param)
		{
			return ExtractTupleValues(tupleExpression, param);
		}

		static Tuple<Expression, Expression> GenerateKeyExpressions(IBuildContext context, Expression expr, Dictionary<ParameterExpression, ParameterExpression> mainParamTranformation, List<Expression> foundMembers)
		{
			var sql = context.ConvertToIndex(expr, 0, ConvertFlags.Key);

			var memberOfProjection = new List<Expression>();
			var memberOfDetail = new List<Expression>();
			foreach (var s in sql)
			{
				Expression forDetail = null;

				foreach (var transform in mainParamTranformation.Values)
				{
					forDetail = ConstructMemberPath(s.MemberChain, transform);
					if (forDetail != null)
						break;
				}

				if (forDetail == null)
					continue;

				var forProjection = context.Builder.BuildSql(s.Sql.SystemType, s.Index);
				memberOfProjection.Add(forProjection);

				//TODO: more correct memberchain processing

				if (forDetail.Type != forProjection.Type)
					forDetail = Expression.Convert(forDetail, forProjection.Type);
				memberOfDetail.Add(forDetail);
			}

			if (memberOfDetail.Count == 0)
			{
				// try to find fields
//				foreach (var member in foundMembers)
//				{
//					var ctx = context.Builder.GetContext(context, member);
//					if (ctx == null)
//						continue;
//
//					var fieldsSql = ctx.ConvertToIndex(member, 0, ConvertFlags.Field);
//					if (fieldsSql.Length == 1)
//					{
//						var s = fieldsSql[0];
//
//						foreach (var transform in mainParamTranformation.Values)
//						{
//							if (transform.Type == member.Member.DeclaringType)
//							{
//								var forDetail =
//									(Expression)Expression.MakeMemberAccess(transform, member.Member);
//								var forProjection = ctx.Builder.BuildSql(s.Sql.SystemType, s.Index);
//
//								if (forDetail.Type != forProjection.Type)
//									forDetail = Expression.Convert(forDetail, forProjection.Type);
//								memberOfDetail.Add(forDetail);
//
//								memberOfProjection.Add(forProjection);
//
//								break;
//							}
//						}
//					}
//				}
			}

			if (memberOfDetail.Count == 0)
			{
				// add fake one
				var zero = Expression.Constant(0);
				memberOfDetail.Add(zero);
				memberOfProjection.Add(zero);
			}

			var exprProjection = GenerateKeyExpression(memberOfProjection.ToArray(), 0);
			var expDetail      = GenerateKeyExpression(memberOfDetail.ToArray(), 0);

			return Tuple.Create(exprProjection, expDetail);
		}

		private static Dictionary<Expression, MemberInfo> CollectAliases(Expression expr)
		{
			var result = new Dictionary<Expression, MemberInfo>(new ExpressionEqualityComparer());
			expr.Visit(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.New:
						{
							var ne = (NewExpression)e;
							if (ne.Members != null)
							{
								for (int i = 0; i < ne.Members.Count; i++)
								{
									result[ne.Arguments[i]] = ne.Members[i];
								}
							}
							
							break;
						}
				}
			});

			return result;
		}

		static IEnumerable<Expression> ExtractArguments(Expression expression)
		{
			if (expression is MethodCallExpression mc)
			{
				foreach (var argument in mc.Arguments)
				{
					foreach (var subArgument in ExtractArguments(argument))
					{
						yield return subArgument;
					}
				}
			}
			else
			{
				yield return expression;
			}
		}

		static IEnumerable<Tuple<Expression, MemberExpression>> ExtractTupleValues(Expression expression, Expression obj)
		{
			if (expression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)expression;
				var properties = mc.Type.GetProperties();
				for (var i = 0; i < mc.Arguments.Count; i++)
				{
					var argument     = mc.Arguments[i];
					var memberAccess = Expression.MakeMemberAccess(obj, properties[i]);

					foreach (var subExpr in ExtractTupleValues(argument, memberAccess))
					{
						yield return subExpr;
					}
				}
			}
			else
			{
				if (obj.NodeType == ExpressionType.MemberAccess)
				{
					yield return Tuple.Create(expression, (MemberExpression)obj);
				}
			}
		}

		static List<Expression> SearchDependencies(Expression expression, ParameterExpression param, MappingSchema mapping)
		{
			var result = new List<Expression>();
			var ignore = new HashSet<Expression>();

			expression.Visit(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var ma = (MemberExpression)e;
							if (!ignore.Contains(ma))
							{
								var root = ma.GetRootObject(mapping);
								if (root == param)
								{
									result.Add(e);
									var current = e;
									while(true)
									{
										ignore.Add(current);
										if (current.NodeType == ExpressionType.MemberAccess)
											current = ((MemberExpression)current).Expression;
										else
											break;
									}  
								}
							}

							break;
						}
				}
			});

			if (result.Count == 0)
			{
				expression.Visit(e =>
				{
					switch (e.NodeType)
					{
						case ExpressionType.Parameter:
							{
								if (param == e && !ignore.Contains(e))
								{
									result.Add(e);
									ignore.Add(e);
								}

								break;
							}
					}

				});
			}

			return result;
		}

		static bool IsQueryableMethod(Expression expression, string methodName, out MethodCallExpression queryableMethod)
		{
			expression = expression.Unwrap();
			if (expression.NodeType == ExpressionType.Call)
			{
				queryableMethod = (MethodCallExpression)expression;
				return queryableMethod.IsQueryable(methodName);
			}

			queryableMethod = null;
			return false;
		}

		static Expression RemoveProjection(IBuildContext context, Expression query, out IBuildContext newContext)
		{
			newContext = context;
			if (IsQueryableMethod(query, "Select", out var selectMethod))
			{
				if (context is SelectContext selectContext)
					newContext = selectContext.Sequence[0];
				if (newContext != null)
					return selectMethod.Arguments[0];
			}

			newContext = context;
			return query;
		}

		static List<Expression> GetKeys(IBuildContext context, Expression expression, Expression keyObj)
		{
			var result = new List<Expression>();
			var keys = context.ConvertToSql(expression, 0, ConvertFlags.Key);
			foreach (var key in keys)
			{
				var path = ConstructMemberPath(key.MemberChain, keyObj);
				if (path != null)
					result.Add(path);
			}
			return result;
		}

		public static Expression EnsureEnumerable(Expression expression)
		{
			var enumerable = typeof(IEnumerable<>).MakeGenericType(GetEnumerableElementType(expression.Type));
			if (expression.Type != enumerable)
				expression = Expression.Convert(expression, enumerable);
			return expression;
		}

		static void ExtractIndepended(Expression mainExpression, Expression detailExpression, HashSet<ParameterExpression> parameters, out Expression queryableExpression, out Expression finalExpression, out ParameterExpression replaceParam)
		{
			queryableExpression = detailExpression;
			finalExpression     = null;
			replaceParam        = null;

			var allowed = new HashSet<ParameterExpression>(parameters);

			void CollectLambdaParamaters(Expression expr)
			{
				expr = expr.Unwrap();
				if (expr.NodeType == ExpressionType.Lambda)
				{
					var lambda = (LambdaExpression)expr;
					foreach (var parameter in lambda.Parameters) 
						allowed.Add(parameter);
				}
			}

			if (mainExpression.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)mainExpression;
				for (int i = 1; i < mc.Arguments.Count; i++)
				{
					CollectLambdaParamaters(mc.Arguments[i]);
				}
			}

			detailExpression.Visit(e =>
			{
				CollectLambdaParamaters(e);
			});

			Expression          newQueryable = null;
			ParameterExpression newParam     = null;
			// Where(Where(q, a => a > 10), b => b.Id > someParam)

			bool IsDepended(Expression expr)
			{
				var depended = null != expr.Find(ee =>
				{
					if (ee.NodeType == ExpressionType.Parameter && !allowed.Contains(ee))
						return true;
					return false;
				});
				return depended;
			}

			finalExpression = detailExpression.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.Lambda:
						{
							var lambda = (LambdaExpression)e;
							foreach (var parameter in lambda.Parameters)
								allowed.Add(parameter);
							break;
						}

					case ExpressionType.Call:
						{
							var mc = (MethodCallExpression)e;
							if (mc.IsQueryable(false))
							{
								var depended = mc.Method.Name.In("First", "FirstOrDefault", "Single", "SingleOrDefault",
									"Skip", "Take");
								if (depended && !IsDepended(mc.Arguments[0]))
								{ 
									depended = IsDepended(mc);

									if (depended)
									{
										newQueryable    = mc.Arguments[0];
										var elementType = GetEnumerableElementType(newQueryable.Type);
										newParam        = Expression.Parameter(typeof(List<>).MakeGenericType(elementType), "replacement");
										var replaceExpr = (Expression)newParam;
										if (!newQueryable.Type.IsSameOrParentOf(typeof(List<>)))
										{
											replaceExpr = Expression.Call(
												_asQueryableMethodInfo.MakeGenericMethod(elementType), replaceExpr);
										}
										var newMethod       = mc.Update(mc.Object, new[]{replaceExpr}.Concat(mc.Arguments.Skip(1)));
										return new TransformInfo(newMethod, true);
									}
								}
							}

							break;
						}
				}

				return new TransformInfo(e);
			});

			if (newQueryable != null)
			{
				queryableExpression = newQueryable;
				replaceParam        = newParam;
			}

		}

		class KeyInfo
		{
			public Expression Original;
			public Expression ForSelect;
			public Expression ForCompilation;
		}

		static IEnumerable<KeyInfo> ExtractKeys(IBuildContext context, Expression expr)
		{
			foreach (var arg in ExtractArguments(expr))
			{
				var ctx = context.Builder.GetContext(context, arg);
				if (ctx == null)
					continue;

				foreach (var keyInfo in ConvertToKeyInfos(ctx, arg, arg))
					yield return keyInfo;
			}
		}

		static IEnumerable<KeyInfo> ExtractKeys(IBuildContext context, ParameterExpression param)
		{
			return ConvertToKeyInfos(context, null, param);
		}

		static IEnumerable<KeyInfo> ConvertToKeyInfos(IBuildContext ctx, Expression forExpr,
			Expression obj)
		{
			var sql = ctx.ConvertToIndex(forExpr, 0, forExpr == null ? ConvertFlags.Key : ConvertFlags.Field);
			foreach (var sqlInfo in sql)
			{
				var forSelect = ConstructMemberPath(sqlInfo.MemberChain, obj);
				if (forSelect == null && forExpr?.NodeType == ExpressionType.MemberAccess)
					forSelect = forExpr;
				if (forSelect != null)
				{
					var parentIdx = ctx.ConvertToParentIndex(sqlInfo.Index, ctx);
					var forCompilation = ctx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx);

					yield return new KeyInfo
					{
						Original = forExpr ?? obj,
						ForSelect = forSelect,
						ForCompilation = forCompilation
					};
				}
			}
		}

//		public static Expression GenerateAssociationExpression(IBuildContext context, AssociationDescriptor association)
//		{
//            		var table = (TableBuilder.AssociatedTableContext)res.Context;
//
//		if (table.IsList)
//		{
//			var me = (MemberExpression)e;
//			Expression expr;
//
//			var parentType = me.Expression.Type;
//			var childType  = table.ObjectType;
//
//			var queryMethod = table.Association.GetQueryMethod(parentType, childType);
//			if (queryMethod != null)
//			{
//				//TODO: MARS
//				var dcConst = Expression.Constant(context.Builder.DataContext.Clone(true));
//
//				expr = queryMethod.GetBody(me.Expression, dcConst);
//			}
//			else
//			{
//				var ttype  = typeof(Table<>).MakeGenericType(childType);
//				var tbl    = Activator.CreateInstance(ttype, context.Builder.DataContext);
//				var method = e == expression ?
//					MemberHelper.MethodOf<IEnumerable<bool>>(n => n.Where(a => a)).GetGenericMethodDefinition().MakeGenericMethod(childType) :
//					_whereMethodInfo.MakeGenericMethod(e.Type, childType, ttype);
//
//				var op = Expression.Parameter(childType, "t");
//
//				parameters.Add(op);
//
//				Expression ex = null;
//
//				for (var i = 0; i < table.Association.ThisKey.Length; i++)
//				{
//					var field1 = table.ParentAssociation.SqlTable.Fields[table.Association.ThisKey [i]];
//					var field2 = table.                  SqlTable.Fields[table.Association.OtherKey[i]];
//
//					var ma1 = Expression.MakeMemberAccess(op,            field2.ColumnDescriptor.MemberInfo);
//					var ma2 = Expression.MakeMemberAccess(me.Expression, field1.ColumnDescriptor.MemberInfo);
//
//					var ee = Equal(mappingSchema, ma1, ma2);
//
//					ex = ex == null ? ee : Expression.AndAlso(ex, ee);
//				}
//
//				var predicate = table.Association.GetPredicate(parentType, childType);
//				if (predicate != null)
//				{
//					var body = predicate.GetBody(me.Expression, op);
//					ex = ex == null ? body : Expression.AndAlso(ex, body);
//				}
//
//				if (ex == null)
//					throw new LinqToDBException($"Invalid association configuration for {table.Association.MemberInfo.DeclaringType}.{table.Association.MemberInfo.Name}");
//
//				expr = Expression.Call(null, method, Expression.Constant(tbl), Expression.Lambda(ex, op));
//			}
//
//			if (e == expression)
//			{
//				expr = Expression.Call(
//					MemberHelper.MethodOf<IEnumerable<int>>(n => n.ToList()).GetGenericMethodDefinition().MakeGenericMethod(childType),
//					expr);
//			}
//
//			return expr;
//		}

		public static Expression GenerateDetailsExpression(IBuildContext context, MappingSchema mappingSchema,
			Expression expression, HashSet<ParameterExpression> parameters)
		{
			var builder = context.Builder;

			var initialMainQuery = builder.Expression.Unwrap();
			var masterQueryFinal = initialMainQuery;
			expression = expression.Unwrap();
			var unchangedDetailQuery = expression;

			ExtractIndepended(initialMainQuery, unchangedDetailQuery, parameters, out var queryableDetail, out var finalExpression, out var replaceParam);

			var foundMembers = new List<Expression>();
			Expression resultExpression;

			var mainParamTransformation = new Dictionary<ParameterExpression, ParameterExpression>();
			var foundKeyExpressions = new List<Expression>();

			var hasConnectionWithMaster = false;
			var detailExpressionTransformation = new Dictionary<Expression, Expression>(new ExpressionEqualityComparer());

			if (queryableDetail.NodeType == ExpressionType.Lambda)
			{
				// that means we processing association from TableContext. First parameter is master
				var lambdaExpression = ((LambdaExpression)queryableDetail);
				var masterParm       = lambdaExpression.Parameters[0];
				var newDetailQuery   = lambdaExpression.Body;
				var masterKeys       = ExtractKeys(context, masterParm).ToArray();

				resultExpression = GeneratePreambleExpression(masterKeys, detailExpressionTransformation, masterParm, newDetailQuery, masterQueryFinal, builder);

				return resultExpression;

			}
            else
			if (initialMainQuery.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)initialMainQuery;
				if (context is SelectContext selectContext)
				{
					if (mc.IsQueryable("Select"))
					{
						// remove projection
						initialMainQuery   = mc.Arguments[0];
						var paramType = EagerLoading.GetEnumerableElementType(initialMainQuery.Type);

						mainParamTransformation.Add(((LambdaExpression)mc.Arguments[1].Unwrap()).Parameters[0], Expression.Parameter(paramType, GetMasterParamName("master_")));

						context = selectContext.Sequence[0];
					}
                    else if (mc.IsQueryable("Join"))
					{
						resultExpression = ProcessMethodCallWithLastProjection(builder, mc, selectContext, queryableDetail, mappingSchema);
					}
					else if (mc.IsQueryable("SelectMany"))
					{
						var masterQuery           = mc.Arguments[0].Unwrap();
						var master_1_Lambda       = (LambdaExpression)mc.Arguments[1].Unwrap();
						var master_1              = master_1_Lambda.Parameters[0];
						var masterDetail_1_Lambda = (LambdaExpression)mc.Arguments[2].Unwrap();
						var master_12             = masterDetail_1_Lambda.Parameters[0];
						var detail_1              = masterDetail_1_Lambda.Parameters[1];
						var subDetail             = expression.Unwrap();
						var master_2              = Expression.Parameter(GetEnumerableElementType(masterQuery.Type), "master_2");

						if (!IsQueryableMethod(master_1_Lambda.Body, "Select", out var masterP_1_Select))
						{
							var initialMainQueryProjectionLambda = (LambdaExpression)mc.Arguments[2].Unwrap();
							var projectionMethod = _tupleConstructors[0].MakeGenericMethod(
								initialMainQueryProjectionLambda.Parameters[0].Type,
								initialMainQueryProjectionLambda.Parameters[1].Type);

							var tupleType = projectionMethod.ReturnType;

							var newProjection = Expression.Call(projectionMethod,
								initialMainQueryProjectionLambda.Parameters);

							var newProjectionLambda = Expression.Lambda(newProjection, initialMainQueryProjectionLambda.Parameters);

							var newSelectManyMethod = _selectManyMethodInfo.MakeGenericMethod(
								mc.Method.GetGenericArguments()[0], mc.Method.GetGenericArguments()[1],
								newProjection.Type);

							var ms = newSelectManyMethod.DisplayName(fullName:false);
							var zz = mc.Method.GetGenericArguments()[1];

							var newSelectMany = Expression.Call(newSelectManyMethod, mc.Arguments[0], mc.Arguments[1],
								newProjectionLambda);

							var masterParm = Expression.Parameter(tupleType, GetMasterParamName("master_x_"));

							detailExpressionTransformation.Add(initialMainQueryProjectionLambda.Parameters[0], Expression.PropertyOrField(masterParm, "Item1"));
							detailExpressionTransformation.Add(initialMainQueryProjectionLambda.Parameters[1], Expression.PropertyOrField(masterParm, "Item2"));

							var newDetailQuery = queryableDetail.Transform(e =>
							{
								if (e.NodeType == ExpressionType.Parameter)
								{
									if (detailExpressionTransformation.TryGetValue((ParameterExpression)e,
										out var replacement2))
										return replacement2;
								}

								return e;
							});

							masterQueryFinal = newSelectMany;
							var zzz = initialMainQuery;

							hasConnectionWithMaster = !ReferenceEquals(initialMainQuery, newDetailQuery);

							if (!hasConnectionWithMaster)
							{
								// detail query has no dependency with master, so load all

								var detailElementType = GetEnumerableElementType(unchangedDetailQuery.Type);
								var enlistMethod1 = EnlistEagerLoadingFunctionalityDetachedMethodInfo.MakeGenericMethod(detailElementType);

								resultExpression = (Expression)enlistMethod1.Invoke(null,
									new object[]
										{ builder, unchangedDetailQuery });

                                return resultExpression;
							}

							context = selectContext.Sequence[1];

							var masterKeys = ExtractKeys(context, initialMainQueryProjectionLambda.Parameters[1]).ToArray();

							var additionalDependencies1 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[0],
								mappingSchema);

							var additionalDependencies2 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[1],
								mappingSchema);

							var dependencies1 = additionalDependencies1.SelectMany(d => ExtractKeys(context, d)).ToArray();
							var dependencies2 = additionalDependencies2.SelectMany(d => ExtractKeys(context, d)).ToArray();

							var preparedKeys = masterKeys
								.Concat(dependencies1)
								.Concat(dependencies2)
								.ToArray();

							var keyCompiledExpression = GenerateKeyExpression(preparedKeys.Select(k => k.ForCompilation).ToArray(), 0);
							var keySelectExpression   = GenerateKeyExpression(preparedKeys.Select(k => k.ForSelect).ToArray(), 0);

							keySelectExpression = keySelectExpression.Transform(e =>
								detailExpressionTransformation.TryGetValue(e, out var replacement) ? replacement : e);

							var keySelectLambda    = Expression.Lambda(keySelectExpression, masterParm);
							var detailsQueryLambda = Expression.Lambda(EnsureEnumerable(newDetailQuery), masterParm);

							var enlistMethod =
								EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
									GetEnumerableElementType(masterQueryFinal.Type),
									GetEnumerableElementType(newDetailQuery.Type),
									keyCompiledExpression.Type);

							var msStr = enlistMethod.DisplayName(fullName:false);

							var master   = masterQueryFinal.Type.ShortDisplayName();
							var qd       = detailsQueryLambda.Type.ShortDisplayName();
							var compiled = keyCompiledExpression.Type.ShortDisplayName();
							var ke       = keySelectExpression.Type.ShortDisplayName();

							resultExpression = (Expression)enlistMethod.Invoke(null,
								new object[]
									{ builder, masterQueryFinal, detailsQueryLambda, keyCompiledExpression, keySelectLambda });

							return resultExpression;
						}
						else
						{
							var masterP_1            = ((LambdaExpression)masterP_1_Select.Arguments[1].Unwrap()).Parameters[0];
							var masterP_1Dpendencies = SearchDependencies(subDetail, masterP_1, mappingSchema);

							var subDetailQueryWithoutProjection = masterP_1_Select.Arguments[0].Unwrap();

							var master_1_Dependencies = SearchDependencies(subDetailQueryWithoutProjection, master_1,
								mappingSchema);

							if (typeof(KeyDetailEnvelope<,>).IsSameOrParentOf(masterDetail_1_Lambda.Body.Type))
							{
								var envelopeCreate = (MethodCallExpression)masterDetail_1_Lambda.Body.Unwrap();

								var reducedBody = RemoveProjection(selectContext, master_1_Lambda.Body, out var newContext);

								var subMasterParam =
									Expression.Parameter(GetEnumerableElementType(reducedBody.Type), "submaster");

								var dependencies1 = master_1_Dependencies.SelectMany(d => ExtractKeys(newContext, d)).ToArray();
								var dependencies2 = masterP_1Dpendencies. SelectMany(d => ExtractKeys(newContext, d)).ToArray();

								var preparedKeys = ExtractKeys(context, envelopeCreate.Arguments[0])
									.Concat(ExtractKeys(newContext, subMasterParam))
									.Concat(dependencies1)
									.Concat(dependencies2)
									.ToArray();

								var newSubMasterLambda = Expression.Lambda(EnsureEnumerable(reducedBody), master_1);

								var keyCompiledExpression = GenerateKeyExpression(preparedKeys.Select(k => k.ForCompilation).ToArray(), 0);
								var keySelectExpression   = GenerateKeyExpression(preparedKeys.Select(k => k.ForSelect).ToArray(), 0);

								var originalKeySelectExpression = keySelectExpression;
								keySelectExpression = keySelectExpression.Transform(e => e == master_1 ? master_12 : e);
								keySelectExpression = keySelectExpression.Transform(e => e == masterP_1 ? subMasterParam : e);
								var keySelectLambda = Expression.Lambda(keySelectExpression, master_12, subMasterParam); 

								var detailParam =
									Expression.Parameter(GetEnumerableElementType(keySelectExpression.Type), "ddd");

								var extractedTupleValues1 = ExtractTupleValues(keySelectExpression, detailParam).ToArray();
								var tupleAccessExpressions = extractedTupleValues1.Select(t => t.Item2).ToArray();

								var detailKeyExpression = GenerateKeyExpression(tupleAccessExpressions, 0);


								var masterQueryFinalMethod =
									_selectManyMethodInfo.MakeGenericMethod(master_2.Type, GetEnumerableElementType(newSubMasterLambda.Body.Type), GetEnumerableElementType(keySelectLambda.Body.Type));

								masterQueryFinal = Expression.Call(masterQueryFinalMethod, masterQuery, newSubMasterLambda, keySelectLambda);

								var masterObjType1  = GetEnumerableElementType(masterQueryFinal.Type);

								var secondMasterParam  = Expression.Parameter(masterObjType1, "subMaster");

							
								var extractedTupleValues2 = ExtractTupleValues(originalKeySelectExpression, secondMasterParam).ToArray();
								var correctLookup = extractedTupleValues2.ToLookup(tv => tv.Item1, tv => tv.Item2, new ExpressionEqualityComparer());

								var correctedQueryDetail = queryableDetail.Transform(e =>
								{
									if (correctLookup.Contains(e))
									{
										return correctLookup[e].First();
									}

									return e;
								});

								var detailsQueryLambda  = Expression.Lambda(EnsureEnumerable(correctedQueryDetail), secondMasterParam);
//							var detailsQueryLambda  = Expression.Lambda(EnsureEnumerable(queryableDetail), masterP_1);

								var detailObjType1  = GetEnumerableElementType(correctedQueryDetail.Type);

								var detailsKeyExpression = Expression.Lambda(detailKeyExpression, detailParam);

								var enlistMethod =
									EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(masterObjType1, detailObjType1,
										detailKeyExpression.Type);

								var ms = enlistMethod.DisplayName(fullName:false);

								var master   = masterQueryFinal.Type.ShortDisplayName();
								var qd       = detailsQueryLambda.Type.ShortDisplayName();
								var compiled = keyCompiledExpression.Type.ShortDisplayName();
								var ke       = detailKeyExpression.Type.ShortDisplayName();

								resultExpression = (Expression)enlistMethod.Invoke(null,
									new object[]
										{ builder, masterQueryFinal, detailsQueryLambda, keyCompiledExpression, detailsKeyExpression });

								return resultExpression;
							}
							else
							{
								throw new NotImplementedException();
							}
						}
					}
				}
			}
            else 
			{

			}

			var detailsQuery = queryableDetail.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
                    if (mainParamTransformation.TryGetValue((ParameterExpression)e, out var replacement1))
						return replacement1;
                    if (detailExpressionTransformation.TryGetValue((ParameterExpression)e, out var replacement2))
						return replacement2;
				}

				return e;
			});

			var hasConnection  = !ReferenceEquals(detailsQuery, queryableDetail);

			queryableDetail.Visit(e =>
			{
				if (e.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression)e;
					var root = ma.GetRootObject(context.Builder.MappingSchema);
					if (root is ParameterExpression mainParam && mainParamTransformation.ContainsKey(mainParam))
						foundMembers.Add(ma);
				}
			});

			var masterObjType  = GetEnumerableElementType(initialMainQuery.Type);
			var detailObjType  = GetEnumerableElementType(detailsQuery.Type);

			if (hasConnection && !EagerLoading.ValidateEagerLoading(context, mainParamTransformation, ref detailsQuery))
			{
				// TODO: temporary fallback to lazy implementation
				return null;
			}

			var ienumerableType = typeof(IEnumerable<>).MakeGenericType(detailObjType);
			if (detailsQuery.Type != ienumerableType)
				detailsQuery = Expression.Convert(detailsQuery, ienumerableType);

			if (!hasConnection)
			{
				// detail query has no dependency with master, so load all

				var enlistMethod = EnlistEagerLoadingFunctionalityDetachedMethodInfo.MakeGenericMethod(detailObjType);

				resultExpression = (Expression)enlistMethod.Invoke(null,
					new object[]
						{ builder, detailsQuery });
			}
			else
			{

				Tuple<Expression, Expression> keyExpressions =
					GenerateKeyExpressions(context, null, mainParamTransformation, foundMembers);

				var detailsKeyExpression = Expression.Lambda(keyExpressions.Item2, mainParamTransformation.Values.Last());
				var detailsQueryLambda   = Expression.Lambda(detailsQuery, mainParamTransformation.Values.First());

				var enlistMethod =
					EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(masterObjType, detailObjType,
						keyExpressions.Item1.Type);

				resultExpression = (Expression)enlistMethod.Invoke(null,
					new object[]
						{ builder, initialMainQuery, detailsQueryLambda, keyExpressions.Item1, detailsKeyExpression });
			}

			return resultExpression;
		}

		private static Expression GeneratePreambleExpression(KeyInfo[] preparedKeys, Dictionary<Expression, Expression> detailExpressionTransformation,
			ParameterExpression masterParam, Expression detailQuery, Expression masterQuery, ExpressionBuilder builder)
		{
			var keyCompiledExpression = GenerateKeyExpression(preparedKeys.Select(k => k.ForCompilation).ToArray(), 0);
			var keySelectExpression   = GenerateKeyExpression(preparedKeys.Select(k => k.ForSelect).ToArray(), 0);

			keySelectExpression = keySelectExpression.Transform(e =>
				detailExpressionTransformation.TryGetValue(e, out var replacement) ? replacement : e);

			var keySelectLambda    = Expression.Lambda(keySelectExpression, masterParam);
			var detailsQueryLambda = Expression.Lambda(EnsureEnumerable(detailQuery), masterParam);

			var enlistMethod =
				EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
					GetEnumerableElementType(masterQuery.Type),
					GetEnumerableElementType(detailQuery.Type),
					keyCompiledExpression.Type);

			var msStr    = enlistMethod.DisplayName(fullName: false);

			var master   = masterQuery.Type.ShortDisplayName();
			var qd       = detailsQueryLambda.Type.ShortDisplayName();
			var compiled = keyCompiledExpression.Type.ShortDisplayName();
			var ke       = keySelectExpression.Type.ShortDisplayName();

			var resultExpression = (Expression)enlistMethod.Invoke(null,
				new object[]
					{ builder, masterQuery, detailsQueryLambda, keyCompiledExpression, keySelectLambda });
			return resultExpression;
		}

		private static Expression ProcessMethodCallWithLastProjection(ExpressionBuilder builder, MethodCallExpression mc, SelectContext selectContext, Expression queryableDetail, MappingSchema mappingSchema)
		{
            //TODO: remove
			var initialMainQuery = mc;
			IBuildContext context = selectContext; 

			var detailExpressionTransformation = new Dictionary<Expression, Expression>(new ExpressionEqualityComparer());

			var initialMainQueryProjectionLambda = (LambdaExpression)mc.Arguments.Last().Unwrap();
			var projectionMethod = _tupleConstructors[0].MakeGenericMethod(
				initialMainQueryProjectionLambda.Parameters[0].Type,
				initialMainQueryProjectionLambda.Parameters[1].Type);

			var tupleType = projectionMethod.ReturnType;

			var newProjection = Expression.Call(projectionMethod,
				initialMainQueryProjectionLambda.Parameters);

			var newProjectionLambda = Expression.Lambda(newProjection, initialMainQueryProjectionLambda.Parameters);

			var originalMethodInfo = mc.Method.GetGenericMethodDefinition();
			var originalGenericArguments = mc.Method.GetGenericArguments();
			var genericArguments = originalGenericArguments.Take(originalGenericArguments.Length - 1)
				.Concat(new[] { newProjection.Type })
				.ToArray();

			var newMethodInfo = originalMethodInfo.MakeGenericMethod(genericArguments);

			var ms = newMethodInfo.DisplayName(fullName:false);
			var zz = mc.Method.GetGenericArguments()[1];

			var newArguments = mc.Arguments.Take(mc.Arguments.Count - 1)
				.Concat(new Expression[] { newProjectionLambda })
				.ToArray();

			var newMethodExpression = Expression.Call(newMethodInfo, newArguments);

			var masterParm = Expression.Parameter(tupleType, GetMasterParamName("master_x_"));

			detailExpressionTransformation.Add(initialMainQueryProjectionLambda.Parameters[0], Expression.PropertyOrField(masterParm, "Item1"));
			detailExpressionTransformation.Add(initialMainQueryProjectionLambda.Parameters[1], Expression.PropertyOrField(masterParm, "Item2"));

			var newDetailQuery = queryableDetail.Transform(e =>
			{
				if (e.NodeType == ExpressionType.Parameter)
				{
					if (detailExpressionTransformation.TryGetValue((ParameterExpression)e,
						out var replacement2))
						return replacement2;
				}

				return e;
			});

			var masterQueryFinal = newMethodExpression;

			var hasConnectionWithMaster = !ReferenceEquals(initialMainQuery, newDetailQuery);

			if (!hasConnectionWithMaster)
			{
				return null;
			}

			context = selectContext.Sequence[1];

			var masterKeys = ExtractKeys(context, initialMainQueryProjectionLambda.Parameters[1]).ToArray();

			var additionalDependencies1 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[0],
				mappingSchema);

			var additionalDependencies2 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[1],
				mappingSchema);

			var dependencies1 = additionalDependencies1.SelectMany(d => ExtractKeys(context, d)).ToArray();
			var dependencies2 = additionalDependencies2.SelectMany(d => ExtractKeys(context, d)).ToArray();

			var preparedKeys = masterKeys
				.Concat(dependencies1)
				.Concat(dependencies2)
				.ToArray();

			var keyCompiledExpression = GenerateKeyExpression(preparedKeys.Select(k => k.ForCompilation).ToArray(), 0);
			var keySelectExpression   = GenerateKeyExpression(preparedKeys.Select(k => k.ForSelect).ToArray(), 0);

			keySelectExpression = keySelectExpression.Transform(e =>
				detailExpressionTransformation.TryGetValue(e, out var replacement) ? replacement : e);

			var keySelectLambda    = Expression.Lambda(keySelectExpression, masterParm);
			var detailsQueryLambda = Expression.Lambda(EnsureEnumerable(newDetailQuery), masterParm);

			var enlistMethod =
				EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
					GetEnumerableElementType(masterQueryFinal.Type),
					GetEnumerableElementType(newDetailQuery.Type),
					keyCompiledExpression.Type);

			var msStr = enlistMethod.DisplayName(fullName:false);

			var master   = masterQueryFinal.Type.ShortDisplayName();
			var qd       = detailsQueryLambda.Type.ShortDisplayName();
			var compiled = keyCompiledExpression.Type.ShortDisplayName();
			var ke       = keySelectExpression.Type.ShortDisplayName();

			var resultExpression = (Expression)enlistMethod.Invoke(null,
				new object[]
					{ builder, masterQueryFinal, detailsQueryLambda, keyCompiledExpression, keySelectLambda });

			return resultExpression;
		}

		static Expression EnlistEagerLoadingFunctionalityDetached<TD>(
			ExpressionBuilder builder,
			Expression detailQueryExpression)
		{
			var detailQuery = Internals.CreateExpressionQueryInstance<TD>(builder.DataContext, detailQueryExpression);

			//TODO: currently we run in separate query

			var idx = RegisterPreamblesDetached(builder, detailQuery);

			var resultExpression = Expression.Convert(
				Expression.ArrayIndex(ExpressionBuilder.PreambleParam, Expression.Constant(idx)),
				typeof(List<TD>));

			return resultExpression;
		}


		static class KeyDetailEnvelope
		{
			public static KeyDetailEnvelope<TKey, TDetail> Create<TKey, TDetail>(TKey key, TDetail detail)
			{
				return new KeyDetailEnvelope<TKey, TDetail>(key, detail);
			}
		}

		class KeyDetailEnvelope<TKey, TDetail>
		{
			public KeyDetailEnvelope(TKey key, TDetail detail)
			{
				Key    = key;
				Detail = detail;
			}

			public TKey    Key    { get; }
			public TDetail Detail { get; }
		}

		static Expression EnlistEagerLoadingFunctionality<T, TD, TKey>(
			ExpressionBuilder builder,
			Expression mainQueryExpr, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryLambda,
			Expression compiledKeyExpression,
			Expression<Func<T, TKey>> selectKeyExpression)
		{
			var mainQuery   = Internals.CreateExpressionQueryInstance<T>(builder.DataContext, mainQueryExpr);
			var detailQuery = mainQuery.SelectMany(detailQueryLambda, (m, d) => KeyDetailEnvelope.Create(selectKeyExpression.Compile()(m), d));

			//TODO: currently we run in separate query

			var idx = RegisterPreambles(builder, detailQuery);

			var getListMethod =
				typeof(EagerLoadingContext<TD, TKey>).GetMethod("GetList", BindingFlags.Instance | BindingFlags.Public);

			var resultExpression =
				Expression.Call(
					Expression.Convert(Expression.ArrayIndex(ExpressionBuilder.PreambleParam, Expression.Constant(idx)),
						typeof(EagerLoadingContext<TD, TKey>)), getListMethod, compiledKeyExpression);

			return resultExpression;
		}

		private static int RegisterPreamblesDetached<TD>(ExpressionBuilder builder, IQueryable<TD> detailQuery)
		{
			var expr = detailQuery.Expression;
			var idx = builder.RegisterPreamble(dc =>
				{
					var queryable = new ExpressionQueryImpl<TD>(dc, expr);
					var details = queryable.ToList();
					return details;
				},
				async dc =>
				{
					var queryable = new ExpressionQueryImpl<TD>(dc, expr);
					var details = await queryable.ToListAsync();
					return details;
				}
			);
			return idx;
		}

		private static int RegisterPreambles<TD, TKey>(ExpressionBuilder builder, IQueryable<KeyDetailEnvelope<TKey, TD>> detailQuery)
		{
			var expr = detailQuery.Expression;
			// Filler code is duplicated for the future usage with IAsyncEnumerable
			var idx = builder.RegisterPreamble(dc =>
				{
					var queryable = new ExpressionQueryImpl<KeyDetailEnvelope<TKey, TD>>(dc, expr);
					var detailsWithKey = queryable.ToList();
					var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

					foreach (var d in detailsWithKey)
					{
						eagerLoadingContext.Add(d.Key, d.Detail);
					}

					return eagerLoadingContext;
				},
				async dc =>
				{
					var queryable = new ExpressionQueryImpl<KeyDetailEnvelope<TKey, TD>>(dc, expr);
					var detailsWithKey = await queryable.ToListAsync();
					var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

					foreach (var d in detailsWithKey)
					{
						eagerLoadingContext.Add(d.Key, d.Detail);
					}

					return eagerLoadingContext;
				}
			);
			return idx;
		}

		public static bool ValidateEagerLoading(IBuildContext context, Dictionary<ParameterExpression, ParameterExpression> mainParamTransformation, ref Expression expression)
		{
			if (context is JoinBuilder.GroupJoinContext joinContext)
				return false;

			var elementType = GetEnumerableElementType(expression.Type);

			var helperType = typeof(EagerLoadingHelper<,>).MakeGenericType(mainParamTransformation.Values.Last().Type, elementType);
			var helper = (EagerLoadingHelper)Activator.CreateInstance(helperType);

			if (!helper.Validate(context, mainParamTransformation, ref expression))
				return false;

			return true;
		}

		
		abstract class EagerLoadingHelper
		{
			public abstract bool Validate(IBuildContext context,
				Dictionary<ParameterExpression, ParameterExpression> masterParam,
				ref Expression expression);
		}

		class EagerLoadingHelper<TMaster, TDetail> : EagerLoadingHelper
		{
			private bool IsSelectValid(SelectQuery select)
			{
				var isInvalid = select.Select.SkipValue != null ||
				                select.Select.TakeValue != null;

				if (!isInvalid)
				{
					foreach (var t in select.Select.From.Tables)
					{
						if (t.Source is SelectQuery sq)
							if (!IsSelectValid(sq))
							{
								isInvalid = true;
								break;
							}
					}
				}

				return !isInvalid;
			}

			public override bool Validate(IBuildContext context,
				Dictionary<ParameterExpression, ParameterExpression> masterParam,
				ref Expression expression)
			{
				var isValid = null == expression.Find(e =>
				{
					if (e.NodeType == ExpressionType.Call)
					{
						var mc = (MethodCallExpression)e;

						if (mc.IsQueryable(false))
						{
							return mc.Method.Name.In("Fist", "FistOrDefault", "Single", "SingleOrDefault", "Skip",
								"Take");
						}
					}

					return false;
				});

				return isValid;


//				var detailsQuery = expression;
//				var detailObjType = GetEnumerableElementType(detailsQuery.Type);
//
//				var masterTable = new Table<TMaster>(context.Builder.DataContext);
//
//				var ienumerableType = typeof(IEnumerable<>).MakeGenericType(detailObjType);
//
//				if (detailsQuery.Type != ienumerableType)
//					detailsQuery = Expression.Convert(detailsQuery, ienumerableType);
//
//				var localQueryable = masterTable.SelectMany(Expression.Lambda<Func<TMaster, IEnumerable<TDetail>>>(detailsQuery, masterParam), 
//					(m, d) => d);
//
//				var queryableExpression = localQueryable.Expression;
//				var localQuery = new Query<TDetail>(context.Builder.DataContext, queryableExpression);
//				var localBuilder = new ExpressionBuilder(localQuery, context.Builder.DataContext, queryableExpression, null);
//
//				var buildInfo = new BuildInfo(context, queryableExpression, new SelectQuery());
//				var localSequence = localBuilder.BuildSequence(buildInfo);
//
//				var sqlOptimizer = context.Builder.DataContext.GetSqlOptimizer();
//				var statement = sqlOptimizer.Finalize(localSequence.GetResultStatement());
//
//				if (!(statement is SqlSelectStatement selectStatement))
//					return false;
//
//				var isValid = IsSelectValid(selectStatement.SelectQuery);
//				return isValid;
			}
		}
	}
}
