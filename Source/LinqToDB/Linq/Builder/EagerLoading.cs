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

		static readonly MethodInfo _toArrayMethodInfo =
			MemberHelper.MethodOf(() => Enumerable.ToArray<object>(null)).GetGenericMethodDefinition();

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

		static bool IsDetailType(Type type)
		{
			var isEnumerable = false;

			isEnumerable = type != typeof(string) && typeof(IEnumerable<>).IsSameOrParentOf(type);

			if (!isEnumerable && type.IsClassEx() && type.IsGenericTypeEx() && type.Name.StartsWith("<>"))
			{
				isEnumerable = type.GenericTypeArguments.Any(t => IsDetailType(t));
			}

			return isEnumerable;
		}

		public static bool IsDetailsMember(MemberInfo memberInfo)
		{
			return IsDetailType(memberInfo.GetMemberType());
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
			var                 needsTruncation = false;
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
							if (mc.IsQueryable(true))
							{
								var depended = mc.Method.Name.In("Any", "Sum", "Min", "Max", "Count", "Average", "Distinct", "First", "FirstOrDefault", "Single", "SingleOrDefault",
									"Skip", "Take");
								needsTruncation = needsTruncation || depended;
								if (needsTruncation && (mc.Arguments[0].NodeType != ExpressionType.Call || !IsDepended(mc.Arguments[0])))
								{ 
									depended = IsDepended(mc);

									if (true)
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
			else
			{
				if (!typeof(List<>).IsSameOrParentOf(queryableExpression.Type))
				{
					var elementType = GetEnumerableElementType(queryableExpression.Type);
					replaceParam    = Expression.Parameter(typeof(List<>).MakeGenericType(elementType), "replacement");
					finalExpression = Expression.Call(
						_asQueryableMethodInfo.MakeGenericMethod(elementType), replaceParam);
				}
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

		static IEnumerable<KeyInfo> ExtractKeysFromContext(IBuildContext context, Expression expr)
		{
			foreach (var keyInfo in ConvertToKeyInfos(context, null, expr))
				yield return keyInfo;
		}

		static IEnumerable<KeyInfo> ExtractKeys(IBuildContext context, ParameterExpression param)
		{
			return ConvertToKeyInfos(context, null, param);
		}

		static IEnumerable<KeyInfo> ConvertToKeyInfos(IBuildContext ctx, Expression forExpr,
			Expression obj)
		{
			var flags = forExpr == null || forExpr.NodeType == ExpressionType.Parameter ? ConvertFlags.Key : ConvertFlags.Field;
			var sql   = ctx.ConvertToIndex(forExpr, 0, flags);

			if (sql.Length == 1)
			{
				var sqlInfo = sql[0];
				if (sqlInfo.MemberChain.Count == 0 && sqlInfo.Sql.SystemType == obj.Type)
				{
					var parentIdx = ctx.ConvertToParentIndex(sqlInfo.Index, ctx);
					var forCompilation = ctx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx);
					yield return new KeyInfo
					{
						Original = forExpr ?? obj,
						ForSelect = obj,
						ForCompilation = forCompilation
					};
					yield break;
				}
			}

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

		public static Expression EnsureDestinationType(Expression expression, Type destinationType)
		{
			if (destinationType.IsArray)
			{
				if (destinationType.IsArray)
				{
					var destinationElementType = GetEnumerableElementType(destinationType);
					expression = Expression.Call(null, _toArrayMethodInfo.MakeGenericMethod(destinationElementType), expression);
				}
			}

			return expression;
		}

		public static Expression GenerateAssociationExpression(ExpressionBuilder builder, IBuildContext context, AssociationDescriptor association)
		{
			var initialMainQuery = builder.Expression;

			// that means we processing association from TableContext. First parameter is master
			Expression detailQuery;

			var mainQueryElementType = GetEnumerableElementType(initialMainQuery.Type);
			var masterParm           = Expression.Parameter(mainQueryElementType, GetMasterParamName("loadwith_"));

			Expression resultExpression;

			// recursive processing
			if (typeof(KeyDetailEnvelope<,>).IsSameOrParentOf(mainQueryElementType))
			{
				if (!IsQueryableMethod(initialMainQuery, "SelectMany", out var mainSelectManyMethod))
					throw new InvalidOperationException("Unexpected Main Query");

				var envelopeCreateLambda = (LambdaExpression)mainSelectManyMethod.Arguments[2].Unwrap();
				var envelopeCreateMethod = (MethodCallExpression)envelopeCreateLambda.Body;

				var newMasterParm = Expression.Parameter(mainQueryElementType, GetMasterParamName("loadwith_"));

				var prevKeys       = ExtractKeys(context, envelopeCreateMethod.Arguments[0]).ToArray();
				var subMasterKeys  = ExtractKeys(context, envelopeCreateMethod.Arguments[1]).ToArray();

				var prevKeysByParameter = ExtractTupleValues(envelopeCreateMethod.Arguments[0], Expression.PropertyOrField(newMasterParm, "Key")).ToArray();

				var correctLookup = prevKeysByParameter.ToLookup(tv => tv.Item1, tv => tv.Item2, new ExpressionEqualityComparer());
				foreach (var key in prevKeys)
				{
					if (correctLookup.Contains(key.ForSelect)) 
						key.ForSelect = correctLookup[key.ForSelect].First();
				}

				var detailProp = Expression.PropertyOrField(newMasterParm, "Detail");
				var subMasterObj = envelopeCreateMethod.Arguments[1];
				foreach (var key in subMasterKeys)
				{
					key.ForSelect = key.ForSelect.Transform(e => e == subMasterObj ? detailProp : e);
				}

				detailQuery = Expression.MakeMemberAccess(detailProp, association.MemberInfo);
				
				var masterKeys = prevKeys.Concat(subMasterKeys).ToArray();
				resultExpression = GeneratePreambleExpression(masterKeys, null, newMasterParm, newMasterParm, detailQuery, initialMainQuery, builder);
			}
			else
			{
				masterParm = Expression.Parameter(mainQueryElementType, GetMasterParamName("master_"));
				var masterKeys = ExtractKeys(context, masterParm).ToArray();

				detailQuery = Expression.MakeMemberAccess(masterParm, association.MemberInfo);

				resultExpression = GeneratePreambleExpression(masterKeys, null, masterParm, masterParm, detailQuery, initialMainQuery, builder);
			}

			resultExpression = EnsureDestinationType(resultExpression, association.MemberInfo.GetMemberType());

			return resultExpression;
		}

		static Expression FindMemberAccess(Expression obj, Type expectedType)
		{
			if (obj.Type == expectedType)
				return obj;

			var props = obj.Type.GetProperties();

			foreach (var prop in props)
			{
				var ma = Expression.MakeMemberAccess(obj, prop);
				if (prop.PropertyType == expectedType)
					return Expression.MakeMemberAccess(obj, prop);

				if (prop.PropertyType.IsGenericTypeEx())
				{
					var found = FindMemberAccess(ma, expectedType);
					if (found != null)
						return found;
				}
			}

			return null;
		}

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
			Expression resultExpression = null;

			var mainParamTransformation = new Dictionary<ParameterExpression, ParameterExpression>();

			var hasConnectionWithMaster = false;
			var detailExpressionTransformation = new Dictionary<Expression, Expression>(new ExpressionEqualityComparer());

			var masterObjType  = GetEnumerableElementType(initialMainQuery.Type);
			var masterParam    = Expression.Parameter(masterObjType, GetMasterParamName("master_"));

			if (initialMainQuery.NodeType == ExpressionType.Call)
			{
				var mc = (MethodCallExpression)initialMainQuery;
				if (context is SelectContext selectContext)
				{
					if (mc.IsQueryable("Select"))
					{
//						if (null == mc.Arguments[1].Find(e => e == queryableDetail))
						{
							// remove projection
							initialMainQuery = mc.Arguments[0];
							masterObjType  = EagerLoading.GetEnumerableElementType(initialMainQuery.Type);
							masterParam    = Expression.Parameter(masterObjType, GetMasterParamName("newmaster_"));

							var pt = masterParam.Type.ShortDisplayName();

							mainParamTransformation.Add(((LambdaExpression)mc.Arguments[1].Unwrap()).Parameters[0], masterParam);
							detailExpressionTransformation.Add(((LambdaExpression)mc.Arguments[1].Unwrap()).Parameters[0], masterParam);

							if (queryableDetail.NodeType == ExpressionType.Parameter)
							{
								var ma = FindMemberAccess(masterParam, queryableDetail.Type);
								if (ma != null)
								{
									hasConnectionWithMaster = true;
									queryableDetail = ma;
								}
							}
							else
								context = selectContext.Sequence[0];
						}
					}
                    else if (mc.IsQueryable("Join"))
					{
						resultExpression = ProcessMethodCallWithLastProjection(builder, mc, selectContext.Sequence[1], 1, queryableDetail, mappingSchema);
					}
					else if (mc.IsQueryable("GroupJoin"))
					{
						resultExpression = ProcessMethodCallWithLastProjection(builder, mc, selectContext.Sequence[0], 0, queryableDetail, mappingSchema);
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

							var masterKeys1= ExtractKeys(selectContext.Sequence[0], initialMainQueryProjectionLambda.Parameters[0]).ToArray();
							var masterKeys2 = ExtractKeys(selectContext.Sequence[1], initialMainQueryProjectionLambda.Parameters[1]).ToArray();


//							var additionalDependencies1 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[0],
//								mappingSchema);
//
//							var additionalDependencies2 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[1],
//								mappingSchema);
//
//							var dependencies1 = additionalDependencies1.SelectMany(d => ExtractKeys(context, d)).ToArray();
//							var dependencies2 = additionalDependencies2.SelectMany(d => ExtractKeys(context, d)).ToArray();

							var preparedKeys = masterKeys1
//								.Concat(dependencies1)
//								.Concat(dependencies2)
								.Concat(masterKeys2)
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

								//TODO: We can find duplicates
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

								var detailsQueryLambda   = Expression.Lambda(EnsureEnumerable(correctedQueryDetail), secondMasterParam);
								var detailObjType1       = GetEnumerableElementType(correctedQueryDetail.Type);
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

			if (resultExpression == null)
			{
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

				hasConnectionWithMaster = hasConnectionWithMaster || !ReferenceEquals(detailsQuery, queryableDetail);

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

				var detailObjType = GetEnumerableElementType(detailsQuery.Type);

//			if (hasConnection && !EagerLoading.ValidateEagerLoading(context, mainParamTransformation, ref detailsQuery))
//			{
//				// TODO: temporary fallback to lazy implementation
//				return null;
//			}

				var ienumerableType = typeof(IEnumerable<>).MakeGenericType(detailObjType);
				if (detailsQuery.Type != ienumerableType)
					detailsQuery = Expression.Convert(detailsQuery, ienumerableType);

				if (!hasConnectionWithMaster)
				{
					// detail query has no dependency with master, so load all

					var enlistMethod =
						EnlistEagerLoadingFunctionalityDetachedMethodInfo.MakeGenericMethod(detailObjType);

					resultExpression = (Expression)enlistMethod.Invoke(null,
						new object[]
							{ builder, detailsQuery });
				}
				else
				{
					Expression keyPath = masterParam;
					if (IsGroupByContext(context))
					{
						keyPath = Expression.PropertyOrField(keyPath, "Key");
					}
					else if (IsGroupJoinContext(context))
					{
						keyPath = Expression.MakeMemberAccess(masterParam, masterParam.Type.GetProperties().First());
					}

					var preparedKeys = ExtractKeysFromContext(context, keyPath).ToArray();
					if (preparedKeys.Length == 0)
					{
						preparedKeys = ExtractKeysFromContext(context, masterParam).ToArray();
					}

					resultExpression = GeneratePreambleExpression(preparedKeys, detailExpressionTransformation,
						masterParam, masterParam, detailsQuery, initialMainQuery, builder);

				}
			}
			if (replaceParam != null)
			{
				resultExpression = finalExpression.Transform(e => e == replaceParam ? resultExpression : e);
			}

			return resultExpression;
		}

		private static bool IsGroupByContext(IBuildContext context)
		{
			while (context is SubQueryContext)
				context = context.Parent;
			return context is GroupByBuilder.GroupByContext;
		}

		private static bool IsGroupJoinContext(IBuildContext context)
		{
			while (context is SubQueryContext)
				context = context.Parent;
			return context is JoinBuilder.GroupJoinContext;
		}

		private static Expression GeneratePreambleExpression(KeyInfo[] preparedKeys, Dictionary<Expression, Expression> detailExpressionTransformation,
			ParameterExpression masterParam, ParameterExpression detailParam, Expression detailQuery, Expression masterQuery, ExpressionBuilder builder)
		{
			var keyCompiledExpression = GenerateKeyExpression(preparedKeys.Select(k => k.ForCompilation).ToArray(), 0);
			var keySelectExpression   = GenerateKeyExpression(preparedKeys.Select(k => k.ForSelect).ToArray(), 0);

			if (detailExpressionTransformation != null)
			{
				keySelectExpression = keySelectExpression.Transform(e =>
					detailExpressionTransformation.TryGetValue(e, out var replacement) ? replacement : e);
			}

			var keySelectLambda    = Expression.Lambda(keySelectExpression, masterParam);
			var detailsQueryLambda = Expression.Lambda(EnsureEnumerable(detailQuery), detailParam);

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

		private static Expression ProcessMethodCallWithLastProjection(ExpressionBuilder builder, MethodCallExpression mc, IBuildContext context, int keyParamIndex, Expression queryableDetail, MappingSchema mappingSchema)
		{
            //TODO: remove
			var initialMainQuery = mc;

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
						out var replacement))
						return replacement;
				}

				return e;
			});

			var masterQueryFinal = newMethodExpression;

			var hasConnectionWithMaster = !ReferenceEquals(initialMainQuery, newDetailQuery);

			if (!hasConnectionWithMaster)
			{
				return null;
			}

			var masterKeys = ExtractKeys(context, initialMainQueryProjectionLambda.Parameters[keyParamIndex])
//				.Concat(ExtractKeys(context, initialMainQueryProjectionLambda.Parameters[1]))
				.ToArray();

//			var additionalDependencies1 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[0],
//				mappingSchema);
//
//			var additionalDependencies2 = SearchDependencies(queryableDetail, initialMainQueryProjectionLambda.Parameters[1],
//				mappingSchema);
//
//			var dependencies1 = additionalDependencies1.SelectMany(d => ExtractKeys(context, d)).ToArray();
//			var dependencies2 = additionalDependencies2.SelectMany(d => ExtractKeys(context, d)).ToArray();
//
//			var preparedKeys = masterKeys
//				.Concat(dependencies1)
//				.Concat(dependencies2)
//				.ToArray();

			var preparedKeys = masterKeys;

			var resultExpression = GeneratePreambleExpression(preparedKeys, detailExpressionTransformation, masterParm, masterParm, newDetailQuery, masterQueryFinal, builder);

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
			var detailQuery = mainQuery.SelectMany(detailQueryLambda, (main, detail) => KeyDetailEnvelope.Create(selectKeyExpression.Compile()(main), detail));

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
