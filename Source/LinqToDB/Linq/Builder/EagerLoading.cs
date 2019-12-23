using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
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
	internal class EagerLoading
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

			if (count > MutableTuple.MaxMemberCount)
			{
				count = MutableTuple.MaxMemberCount;
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count - 1);
				arguments[count - 1] = GenerateKeyExpression(members, startIndex + count);
			}
			else
			{
				arguments = new Expression[count];
				Array.Copy(members, startIndex, arguments, 0, count);
			}

			var type = MutableTuple.Types[count - 1];
			var concreteType = type.MakeGenericType(arguments.Select(a => a.Type).ToArray());

			var constructor = concreteType.GetConstructor(Array<Type>.Empty);

			var newExpression = Expression.New(constructor);
			var initExpression = Expression.MemberInit(newExpression,
				arguments.Select((a, i) => Expression.Bind(concreteType.GetProperty("Item" + (i + 1)), a)));
			return initExpression;
		}


		private static Expression GenerateKeyExpressionOld(Expression[] members, int startIndex)
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

		public static Type GetEnumerableElementType(Type type, MappingSchema mappingSchema)
		{
			if (!IsEnumerableType(type, mappingSchema))
				return type;
			if (type.IsArray)
				return type.GetElementType();
			return type.GetGenericArguments()[0];
		}

		public static bool IsEnumerableType(Type type, MappingSchema mappingSchema)
		{
			if (mappingSchema.IsScalarType(type))
				return false;
			if (!typeof(IEnumerable<>).IsSameOrParentOf(type))
				return false;
			return true;
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
			if (expression is MemberInitExpression mi)
			{
				foreach (var binding in mi.Bindings)
				{
					var assignment = (MemberAssignment)binding;
					foreach (var subArgument in ExtractArguments(assignment.Expression))
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
			if (expression is MemberInitExpression mi)
			{
				foreach (var binding in mi.Bindings)
				{
					var assignment = (MemberAssignment)binding;
					var memberAccess = Expression.MakeMemberAccess(obj, assignment.Member);
					foreach (var subExpr in ExtractTupleValues(assignment.Expression, memberAccess))
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

		public static Expression EnsureEnumerable(Expression expression, MappingSchema mappingSchema)
		{
			var enumerable = typeof(IEnumerable<>).MakeGenericType(GetEnumerableElementType(expression.Type, mappingSchema));
			if (expression.Type != enumerable)
				expression = Expression.Convert(expression, enumerable);
			return expression;
		}

		static void ExtractIndepended(MappingSchema mappingSchema, Expression mainExpression, Expression detailExpression, HashSet<ParameterExpression> parameters, out Expression queryableExpression, out Expression finalExpression, out ParameterExpression replaceParam)
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
										var elementType = GetEnumerableElementType(newQueryable.Type, mappingSchema);
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
					var elementType = GetEnumerableElementType(queryableExpression.Type, mappingSchema);
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

		static IEnumerable<KeyInfo> ExtractKeysFromContext2(IBuildContext context, IEnumerable<Expression> keys)
		{
			foreach (var key in keys)
			{
				foreach (var extracted in ConvertToKeyInfos2(context, key))
				{
					yield return extracted;
				}
			}
		}

		static IEnumerable<KeyInfo> ConvertToKeyInfos2(IBuildContext ctx, Expression forExpr)
		{
			var flags = forExpr.NodeType == ExpressionType.Parameter ? ConvertFlags.Key : ConvertFlags.Field;
			var sql   = ctx.ConvertToIndex(forExpr, 0, flags);

			if (sql.Length == 1)
			{
				var sqlInfo = sql[0];
				if (sqlInfo.MemberChain.Count == 0 && sqlInfo.Sql.SystemType == forExpr.Type)
				{
					var parentIdx = ctx.ConvertToParentIndex(sqlInfo.Index, ctx);
					var forCompilation = ctx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx);
					yield return new KeyInfo
					{
						Original = forExpr,
						ForSelect = forExpr,
						ForCompilation = forCompilation
					};
					yield break;
				}
			}

			foreach (var sqlInfo in sql)
			{
				var forSelect = ConstructMemberPath(sqlInfo.MemberChain, forExpr);
				if (forSelect == null && forExpr.NodeType == ExpressionType.MemberAccess)
					forSelect = forExpr;
				if (forSelect != null)
				{
					var parentIdx = ctx.ConvertToParentIndex(sqlInfo.Index, ctx);
					var forCompilation = ctx.Builder.BuildSql(sqlInfo.Sql.SystemType, parentIdx);

					yield return new KeyInfo
					{
						Original = forExpr,
						ForSelect = forSelect,
						ForCompilation = forCompilation
					};
				}
			}
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

		public static Expression EnsureDestinationType(Expression expression, Type destinationType, MappingSchema mappingSchema)
		{
			if (destinationType.IsArray)
			{
				if (destinationType.IsArray)
				{
					var destinationElementType = GetEnumerableElementType(destinationType, mappingSchema);
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

			var mainQueryElementType = GetEnumerableElementType(initialMainQuery.Type, builder.MappingSchema);
			var masterParm           = Expression.Parameter(mainQueryElementType, GetMasterParamName("loadwith_"));

			Expression resultExpression;

			// recursive processing
			if (typeof(KDH).IsSameOrParentOf(mainQueryElementType))
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

				var detailProp = Expression.PropertyOrField(newMasterParm, "Data");
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

			resultExpression = EnsureDestinationType(resultExpression, association.MemberInfo.GetMemberType(), builder.MappingSchema);

			return resultExpression;
		}

		public static LambdaExpression FindContainingLambda(Expression expr, Expression toFind)
		{
			LambdaExpression result = null;
			expr.Visit(e =>
			{
				if (result == null && e.NodeType == ExpressionType.Lambda)
				{
					var lambda = (LambdaExpression)e;
					var found = lambda.Body.Find(ee => ee == toFind);
					if (found != null)
					{
						result = lambda;
					}
				}
			});

			return result;
		}

		private static void CollectDependencies(Expression forExpr, List<Expression> dependencies, List<ParameterExpression> dependencyParameters)
		{
			var ignore  = new HashSet<Expression>();

			// parent first
			forExpr.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
				{
					var lambda = (LambdaExpression)e;
					foreach (var parameter in lambda.Parameters)
					{
						ignore.Add(parameter);
					}
				}
			});

			// child first
			forExpr.Visit(e =>
			{
				if (ignore.Contains(e))
					return true;

				if (e.NodeType == ExpressionType.MemberAccess)
				{
					var ma = (MemberExpression)e;
					while (ma.Expression.NodeType == ExpressionType.MemberAccess)
					{
						ignore.Add(ma.Expression);
						ma = (MemberExpression)ma.Expression;
					}

					if (ma.Expression.NodeType == ExpressionType.Parameter && !ignore.Contains(ma.Expression))
					{
						dependencies.Add(e);
						ignore.Add(ma.Expression);
						dependencyParameters.Add((ParameterExpression)ma.Expression);
					}
				}
				else if (e.NodeType == ExpressionType.Parameter)
				{
					dependencies.Add(e);
					dependencyParameters.Add((ParameterExpression)e);
					ignore.Add(e);
				}
				return true;
			});
		}

		public static Expression GenerateDetailsExpression(IBuildContext context, MappingSchema mappingSchema,
			Expression expression, HashSet<ParameterExpression> parameters)
		{
			var builder = context.Builder;

			var initialMainQuery = builder.Expression.Unwrap();
			expression = expression.Unwrap();
			var unchangedDetailQuery = expression;

			ExtractIndepended(builder.MappingSchema, initialMainQuery, unchangedDetailQuery, parameters, out var queryableDetail, out var finalExpression, out var replaceParam);

			Expression resultExpression = null;

			var detailLambda = FindContainingLambda(initialMainQuery, queryableDetail);

			if (detailLambda == null)
				throw new NotImplementedException();

			var replaceInfo = new ReplaceInfo(mappingSchema);
			replaceInfo.TargetLambda = detailLambda;

			//TODO: Associations
			var dependencies = new List<Expression>();
			var dependencyParameters = new List<ParameterExpression>();
			CollectDependencies(queryableDetail, dependencies, dependencyParameters);

			if (dependencies.Count == 0 && dependencyParameters.Count == 0)
			{
				// detail query has no dependency with master, so load all

				var enlistMethod =
					EnlistEagerLoadingFunctionalityDetachedMethodInfo.MakeGenericMethod(GetEnumerableElementType(unchangedDetailQuery.Type, builder.MappingSchema));

				resultExpression = (Expression)enlistMethod.Invoke(null,
					new object[]
						{ builder, unchangedDetailQuery });
			}
			else
			{
				// append lambda parameters
				dependencyParameters.AddRange(detailLambda.Parameters.Except(dependencyParameters));

				var keysInfo = ExtractKeysFromContext2(context, dependencies).ToList();
				var keysInfoByParams = ExtractKeysFromContext2(context, dependencyParameters).ToArray();

				var queryableAccessorDic = new Dictionary<Expression, QueryableAccessor>();
				foreach (var info in keysInfoByParams)
				{
					if (!keysInfo.Any(_ => _.ForSelect.EqualsTo(info.ForSelect, queryableAccessorDic, null)))
						keysInfo.Add(info);
				}

				// replaceInfo.Keys.AddRange(keysInfo.Select(k => k.ForSelect));
				replaceInfo.Keys.AddRange(dependencyParameters);

				var mainQueryWithCollectedKey = ApplyReMapping(initialMainQuery, replaceInfo);
				mainQueryWithCollectedKey     = ExpandKeyToMany(mainQueryWithCollectedKey, mappingSchema);

				var resultParameterType = GetEnumerableElementType(mainQueryWithCollectedKey.Type, builder.MappingSchema);
				var resultParameter     = Expression.Parameter(resultParameterType, "key_data");

				var generateKeyExpression   = GenerateKeyExpression(keysInfo.Select(k => k.ForCompilation).ToArray(), 0);
				var keySelectExpression     = GenerateKeyExpression(keysInfo.Select(k => k.ForSelect).ToArray(), 0);
				var keyParametersExpression = GenerateKeyExpression(dependencyParameters.Cast<Expression>().ToArray(), 0);

				var keyField = Expression.PropertyOrField(resultParameter, "Key");
				var pairs = ExtractTupleValues(keyParametersExpression, keyField)
					.ToArray();

				var keySelectExpressionCorrected = keySelectExpression.Transform(e =>
				{
					if (e.NodeType == ExpressionType.Parameter)
					{
						var idx = dependencyParameters.IndexOf((ParameterExpression)e);
						if (idx >= 0)
						{
							return pairs[idx].Item2;
						}
					}

					return e;
				});

				var queryableDetailCorrected = queryableDetail.Transform(e =>
				{
					if (e.NodeType == ExpressionType.Parameter)
					{
						var idx = dependencyParameters.IndexOf((ParameterExpression)e);
						if (idx >= 0)
						{
							return pairs[idx].Item2;
						}
					}

					return e;
				});

				queryableDetailCorrected = EnsureEnumerable(queryableDetailCorrected, builder.MappingSchema);

				var queryableDetailLambda = Expression.Lambda(queryableDetailCorrected, resultParameter);

				var keySelectLambda2 = Expression.Lambda(keySelectExpressionCorrected, resultParameter);

				var enlistMethodFinal =
					EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
						resultParameterType,
						GetEnumerableElementType(queryableDetail.Type, builder.MappingSchema),
						generateKeyExpression.Type);

				resultExpression = (Expression)enlistMethodFinal.Invoke(null,
					new object[]
						{ builder, mainQueryWithCollectedKey, queryableDetailLambda, generateKeyExpression, keySelectLambda2 });
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
			var detailsQueryLambda = Expression.Lambda(EnsureEnumerable(detailQuery, builder.MappingSchema), detailParam);

			var enlistMethod =
				EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(
					GetEnumerableElementType(masterQuery.Type, builder.MappingSchema),
					GetEnumerableElementType(detailQuery.Type, builder.MappingSchema),
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


		class KeyDetailEnvelope<TKey, TDetail>
		{
			public TKey    Key    { get; set; }
			public TDetail Detail { get; set; }
		}

		static Expression EnlistEagerLoadingFunctionality<T, TD, TKey>(
			ExpressionBuilder builder,
			Expression mainQueryExpr, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryLambda,
			Expression compiledKeyExpression,
			Expression<Func<T, TKey>> selectKeyExpression)
		{
			var mainQuery   = Internals.CreateExpressionQueryInstance<T>(builder.DataContext, mainQueryExpr);
			var detailQuery = mainQuery.SelectMany(detailQueryLambda,
				(main, detail) => new KeyDetailEnvelope<TKey, TD>
				{
					Key = selectKeyExpression.Compile()(main),
					Detail = detail
				});

			//TODO: currently we run in separate query

			var idx = RegisterPreambles(builder, detailQuery);

			var getListMethod = MemberHelper.MethodOf((EagerLoadingContext<TD, TKey> c) => c.GetList(default));

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

		public static LambdaExpression CorrectLambdaType(LambdaExpression before, LambdaExpression after)
		{
			if (typeof(IQueryable<>).IsSameOrParentOf(after.ReturnType))
			{
				if (!typeof(IQueryable<>).IsSameOrParentOf(before.ReturnType))
				{
					var convertType = typeof(IEnumerable<>).MakeGenericType(after.Body.Type.GetGenericArguments()[0]);

					after = Expression.Lambda(Expression.Convert(after.Body, convertType), after.Parameters);
				}
			}

			return after;
		}

		internal class ReplaceInfo
		{
			public ReplaceInfo(MappingSchema mappingSchema)
			{
				MappingSchema = mappingSchema;
			}

			public MappingSchema MappingSchema { get; }
			public LambdaExpression TargetLambda { get; set; }
			public List<Expression> Keys { get; } = new List<Expression>();
			public Dictionary<MemberInfo, MemberInfo[]> MemberMapping { get; } = new Dictionary<MemberInfo, MemberInfo[]>();
		}

		private static MethodInfo _selectEnumerableMethodInfo =
			MemberHelper.MethodOf((IEnumerable<int> q) => q.Select(p => p)).GetGenericMethodDefinition();

		private static MethodInfo _selectMethodInfo =
			MemberHelper.MethodOf((IQueryable<int> q) => q.Select(p => p)).GetGenericMethodDefinition();


		internal static Expression CreateKDH(Expression key, Expression data)
		{
			var genericType   = typeof(KDH<,>).MakeGenericType(key.Type, data.Type);
			var constructor   = genericType.GetConstructor(Array<Type>.Empty);
			var newExpression = Expression.New(constructor);
			var memberInit    = Expression.MemberInit(newExpression, 
				Expression.Bind(genericType.GetProperty("Key"), key),
				Expression.Bind(genericType.GetProperty("Data"), data));

			return memberInit;
		}

		class MIReplacement : Expression
		{
			public Expression Original { get; set; }
			public Expression WithKey  { get; set; }
		}

		internal static Expression ExpandKeyToMany(Expression queryExpression, MappingSchema mappingSchema)
		{
			var elementType = GetEnumerableElementType(queryExpression.Type, mappingSchema);
			if (typeof(KDH<,>).IsSameOrParentOf(elementType))
			{
				var keyType = elementType.GetGenericArguments()[0];
				if (IsEnumerableType(keyType, mappingSchema))
				{
					var arg1        = queryExpression;
					var secondParam = Expression.Parameter(elementType, "many_keys");
					var body2       = Expression.PropertyOrField(secondParam, "Key");
					var arg2        = Expression.Lambda(EnsureEnumerable(body2, mappingSchema), secondParam);

					var simplifiedKey = GetEnumerableElementType(body2.Type, mappingSchema);

					var thirdParam1 = Expression.Parameter(elementType, "many_key");
					var thirdParam2 = Expression.Parameter(simplifiedKey, "simplified_key");
					var body3       = CreateKDH(thirdParam2, Expression.PropertyOrField(thirdParam1, "Data"));
					var arg3        = Expression.Lambda(body3, thirdParam1, thirdParam2);

					var methodInfo = _selectManyMethodInfo.MakeGenericMethod(elementType, simplifiedKey, body3.Type);

					var newQuery = Expression.Call(methodInfo, arg1, Expression.Quote(arg2), Expression.Quote(arg3));

					queryExpression = ExpandKeyToMany(newQuery, mappingSchema);
				}
			}

			return queryExpression;
		}

		internal static Expression ApplyReMapping(Expression expr, ReplaceInfo replaceInfo)
		{
			var newExpr = expr;
			switch (expr.NodeType)
			{
				case ExpressionType.Constant:
					{
						if (typeof(IQueryable<>).IsSameOrParentOf(expr.Type))
						{
							var cnt = (ConstantExpression)expr;
							var value = ((IQueryable)cnt.Value).Expression;
							if (value.NodeType != ExpressionType.Constant)
							{
								var newValue = ApplyReMapping(value, replaceInfo);
								if (newValue.Type != value.Type)
								{
									newExpr = newValue;
								}
							}
						}
						break;
					}
				case ExpressionType.Quote:
					{
						var unary = (UnaryExpression)expr;
						var newOperand = ApplyReMapping(unary.Operand, replaceInfo);
						newExpr = unary.Update(newOperand);
						break;
					}
				case ExpressionType.Lambda:
					{
						var lambdaExpression = (LambdaExpression)expr;

						if (expr == replaceInfo.TargetLambda)
						{
							if (typeof(IQueryable<>).IsSameOrParentOf(lambdaExpression.Body.Type))
							{
								// replacing body
								var neededKey     = GenerateKeyExpression(replaceInfo.Keys.ToArray(), 0);
								var itemType      = EagerLoading.GetEnumerableElementType(lambdaExpression.Body.Type, replaceInfo.MappingSchema);
								var parameter     = Expression.Parameter(itemType, "t");
								var selectBody    = CreateKDH(neededKey, parameter);

								var selectMethod = _selectMethodInfo.MakeGenericMethod(itemType, selectBody.Type);
								var newBody      = Expression.Call(selectMethod, lambdaExpression.Body, Expression.Lambda(selectBody, parameter));
								newExpr          = Expression.Lambda(newBody, lambdaExpression.Parameters);
							}
							else
							{
								// replacing body
								var neededKey  = GenerateKeyExpression(replaceInfo.Keys.ToArray(), 0);

								var newBody   = CreateKDH(neededKey, lambdaExpression.Body);
								newExpr       = Expression.Lambda(newBody, lambdaExpression.Parameters);
							}

							newExpr = CorrectLambdaType(lambdaExpression, (LambdaExpression)newExpr);

							// if (expr.Type.IsGenericType)
							// {
							// 	var typesMapping = new Dictionary<Type, Type>();
							// 	var genericArgs = expr.Type.GetGenericArguments();
							// 	RegisterTypeRemapping(expr.Type, newExpr.Type, genericArgs, typesMapping);
							//
							//
							// 	var resultGeneric = genericArgs[genericArgs.Length - 1];
							//
							// 	var newGenerciArgs = newExpr.Type.GetGenericArguments();
							// 	var newResultGeneric = newGenerciArgs[newGenerciArgs.Length - 1];
							//
							// 	var expectedType = ConstructType(resultGeneric, newResultGeneric, typesMapping);
							// 	if (expectedType != newResultGeneric)
							// 	{
							// 		var newLambda = (LambdaExpression)newExpr;
							// 		newExpr = Expression.Lambda(Expression.Convert(newLambda.Body, expectedType),
							// 			newLambda.Parameters);
							// 	}
							// }

						}
						else
						{
							var newBody = ApplyReMapping(lambdaExpression.Body, replaceInfo);
							if (newBody != lambdaExpression.Body)
							{
								newExpr = Expression.Lambda(newBody, lambdaExpression.Parameters);
							}

						}
						break;
					}
				case ExpressionType.Call:
					{
						var mc = (MethodCallExpression)expr;
						if (false && mc.IsQueryable("SelectMany"))
						{
							// public static IQueryable<TResult> SelectMany<TSource, TCollection, TResult>(
							//        this IQueryable<TSource> source,
							//        Expression<Func<TSource, IEnumerable<TCollection>>> collectionSelector,
							//        Expression<Func<TSource, TCollection, TResult>> resultSelector
							// );

						}
						else 
						{
							if (mc.IsQueryable())
							{
								var methodGenericArgumets   = mc.Method.GetGenericArguments();
								var methodGenericDefinition = mc.Method.GetGenericMethodDefinition();
								var genericArguments        = methodGenericDefinition.GetGenericArguments();
								var genericParameters       = methodGenericDefinition.GetParameters();
								var typesMapping            = new Dictionary<Type, Type>();
								var newArguments            = mc.Arguments.ToArray();
								var methodNeedsUpdate       = false;
								for (int i = 0; i < mc.Arguments.Count; i++)
								{
									var arg = mc.Arguments[i];
									var newArg = ApplyReMapping(arg, replaceInfo);
									if (arg.Type != newArg.Type)
									{
										methodNeedsUpdate = true;
										RegisterTypeRemapping(genericParameters[i].ParameterType, newArg.Type,
											genericArguments, typesMapping);

										newArguments[i] = newArg;
									}
									else
									{
										arg = arg.Unwrap();
										if (arg.NodeType == ExpressionType.Lambda && typesMapping.Count > 0)
										{
											var currentLambdaTemplateParams = genericParameters[i];
											var templateLambdaType          = currentLambdaTemplateParams.ParameterType;
											var ga                          = templateLambdaType.GetGenericArguments()[0].GetGenericArguments();
											var argLambda                   = (LambdaExpression)arg;
											var newParameters               = argLambda.Parameters.ToArray();
											var newBody                     = argLambda.Body;
											var needsUpdate                 = false;
											ParameterExpression transientParam = null;
											for (int j = 0; j < argLambda.Parameters.Count; j++)
											{
												var prm = argLambda.Parameters[j];
												var genericType = ga[j];
												if (typesMapping.TryGetValue(genericType, out var replacedType))
												{
													if (replacedType != prm.Type)
													{
														needsUpdate = true;

														if (typeof(KDH<,>).IsSameOrParentOf(replacedType))
														{
															var newParam = Expression.Parameter(replacedType, prm.Name);
															transientParam = newParam;
															newParameters[j] = newParam;

															var accessExpr =
																Expression.PropertyOrField(newParam, "Data");
															newBody = newBody.Transform(e => e == prm ? accessExpr : e);
														}
														else if (typeof(IGrouping<,>).IsSameOrParentOf(replacedType))
														{
															//TODO: Support Grouping???
															return expr;
															var newParam = Expression.Parameter(replacedType, prm.Name);
															transientParam = newParam;
															newParameters[j] = newParam;

															var accessExpr =
																Expression.PropertyOrField(Expression.PropertyOrField(newParam, "Key"), "Data");
															newBody = newBody.Transform(e =>
															{
																if (e == prm)
																{
																	return newParam;
																}

																return e;
															});															
														}
													}
												}
											}

											if (needsUpdate)
											{
												methodNeedsUpdate = true;
												var resultTemplateParam = ga[ga.Length - 1];

												if (typesMapping.TryGetValue(resultTemplateParam, out var replacedType))
												{
													throw new NotImplementedException();
												}
												else
												{
													if (transientParam != null)
													{
														// replacing body
														if (typeof(IEnumerable<>).IsSameOrParentOf(resultTemplateParam))
														{
															var neededKey     = Expression.PropertyOrField(transientParam, "Key");
															var itemType      = EagerLoading.GetEnumerableElementType(newBody.Type, replaceInfo.MappingSchema);
															var parameter     = Expression.Parameter(itemType, "t");
															var selectBody    = CreateKDH(neededKey, parameter);

															var selectMethod =
																(typeof(IQueryable<>).IsSameOrParentOf(
																	resultTemplateParam)
																	? _selectMethodInfo
																	: _selectEnumerableMethodInfo)
																.MakeGenericMethod(itemType, selectBody.Type);
															newBody          = Expression.Call(selectMethod, newBody, Expression.Lambda(selectBody, parameter));
														}
														else
														{
															var neededKey = Expression.PropertyOrField(transientParam, "Key");
															newBody       = CreateKDH(neededKey, newBody);
														}

													}
												}

												var newArgLambda = Expression.Lambda(newBody, newParameters);

												newArgLambda = CorrectLambdaType(argLambda, newArgLambda);
												
												RegisterTypeRemapping(templateLambdaType.GetGenericArguments()[0], newArgLambda.Type, genericArguments, typesMapping);

												newArguments[i] = newArgLambda;
											}
										}
									}
								}
									
								if (methodNeedsUpdate)
								{
									var newGenericArguments = genericArguments.Select((t, i) =>
									{
										if (typesMapping.TryGetValue(t, out var replaced))
											return replaced;
										return methodGenericArgumets[i];
									}).ToArray();

									if (methodGenericDefinition.Name == "ToArray")
										newExpr = newArguments[0];
									else
									{
										var newMethodInfo =
											methodGenericDefinition.MakeGenericMethod(newGenericArguments);
										newExpr = Expression.Call(newMethodInfo, newArguments);
									}
								}
							}
						}

						break;
					}
				case ExpressionType.MemberInit:
					{
						var mi = (MemberInitExpression)expr;
						Expression updated = null;
						for (int i = 0; i < mi.Bindings.Count; i++)
						{
							if (mi.Bindings[i] is MemberAssignment arg)
							{
								var newArg = ApplyReMapping(arg.Expression, replaceInfo);
								if (newArg != arg.Expression)
								{
									updated = newArg;
									break;
								}
							}
						}

						if (updated != null)
						{
							if (typeof(KDH<,>).IsSameOrParentOf(updated.Type))
							{
								updated = updated.NodeType == ExpressionType.MemberInit
									? ((MemberAssignment)((MemberInitExpression)updated).Bindings[0]).Expression
									: Expression.PropertyOrField(updated, "Key");
							}	
							newExpr = CreateKDH(updated, mi);
						}

						break;
					}
				case ExpressionType.New:
					{
						var ne = (NewExpression)expr;
						Expression updated = null;
						int updatedIdx = 0;
						for (int i = 0; i < ne.Arguments.Count; i++)
						{
							var arg = ne.Arguments[i];
							var newArg = ApplyReMapping(arg, replaceInfo);
							if (arg != newArg)
							{
								updated = newArg;
								updatedIdx = i;
								break;
							}
						}

						if (updated != null)
						{
							if (typeof(KDH<,>).IsSameOrParentOf(updated.Type))
							{
								updated = updated.NodeType == ExpressionType.MemberInit
									? ((MemberAssignment)((MemberInitExpression)updated).Bindings[0]).Expression
									: Expression.PropertyOrField(updated, "Key");
							}
							else if (IsEnumerableType(updated.Type, replaceInfo.MappingSchema))
							{
								var elementType = GetEnumerableElementType(updated.Type, replaceInfo.MappingSchema);
								if (typeof(KDH<,>).IsSameOrParentOf(elementType))
								{
									var p = Expression.Parameter(elementType, "t");
									var body = Expression.PropertyOrField(p, "Key");
									var methodInfo = (typeof(IQueryable<>).IsSameOrParentOf(
										updated.Type)
										? _selectMethodInfo
										: _selectEnumerableMethodInfo).MakeGenericMethod(p.Type, body.Type);
									updated = Expression.Call(methodInfo, updated, Expression.Lambda(body, p));
								}
								else
									throw new NotImplementedException();
							};
							newExpr = CreateKDH(updated, ne);
						}

						// if (updated != null)
						// {
						// 	var kdhType = GetEnumerableElementType(updated.Type);
						// 	var keyType = kdhType.GetGenericArguments()[0];
						// 	var param = Expression.Parameter(kdhType, "kdh");
						// 	var body = CreateKDH(Expression.PropertyOrField(param, "Key"), ne);
						// 	var methodInfo = _selectMethodInfo.MakeGenericMethod(kdhType, body.Type);
						// 	newExpr = Expression.Call(methodInfo, updated, Expression.Lambda(body, param));
						// }
						break;
					}
				default:
					{
						if (expr is UnaryExpression unary)
						{
							newExpr = unary.Update(ApplyReMapping(unary.Operand, replaceInfo));
						}
						
						break;
					}
			}

			return newExpr;
		}

		static void RegisterTypeRemapping(Type templateType, Type replaced, Type[] templateArguments, Dictionary<Type, Type> typeMappings)
		{
			if (templateType.IsGenericTypeEx())
			{
				var currentTemplateArguments = templateType.GetGenericArguments();

				var replacedArguments = replaced.GetGenericArguments();
				for (int i = 0; i < currentTemplateArguments.Length; i++)
				{
					RegisterTypeRemapping(currentTemplateArguments[i], replacedArguments[i], templateArguments, typeMappings);
				}
			}
			else
			{
				var idx = Array.IndexOf(templateArguments, templateType);
				if (idx >= 0)
				{
					if (!typeMappings.TryGetValue(templateType, out var value))
					{
						typeMappings.Add(templateType, replaced);
					}
					else
					{
						if (value != replaced)
							throw new InvalidOperationException();
					}
				}
			}
		}



		public static bool ValidateEagerLoading(IBuildContext context, Dictionary<ParameterExpression, ParameterExpression> mainParamTransformation, ref Expression expression)
		{
			if (context is JoinBuilder.GroupJoinContext joinContext)
				return false;

			var elementType = GetEnumerableElementType(expression.Type, context.Builder.MappingSchema);

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
