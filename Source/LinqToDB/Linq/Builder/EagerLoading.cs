using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;

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

		private static readonly MethodInfo _queryWithDetailsInternalMethodInfo = MemberHelper.MethodOf(() =>
			QueryWithDetailsInternalProbe<int, int, int>((IQueryable<int>)null, null, null, null)).GetGenericMethodDefinition();

		private static readonly MethodInfo _enlistEagerLoadingFunctionality = MemberHelper.MethodOf(() =>
			EnlistEagerLoadingFunctionality<int, int, int>(null, null, null, null)).GetGenericMethodDefinition();

		class EagerLoadingContext<T, TKey>
		{
			private Dictionary<TKey, List<T>> _items;

			public void Add(TKey key, T item)
			{
				List<T> list;
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

		static Tuple<Expression, Expression> GenerateKeyExpressions(IBuildContext context, ParameterExpression mainObj)
		{
			var sql = context.ConvertToIndex(null, 0, ConvertFlags.Key);

			var memberOfProjection = new List<Expression>();
			var memberOfDetail = new List<Expression>();
			foreach (var s in sql)
			{
				var forProjection = context.Builder.BuildSql(s.Sql.SystemType, s.Index);
				memberOfProjection.Add(forProjection);
				Expression forDetail = Expression.MakeMemberAccess(mainObj, s.MemberChain[0]);
				if (forDetail.Type != forProjection.Type)
					forDetail = Expression.Convert(forDetail, forProjection.Type);
				memberOfDetail.Add(forDetail);
			}

			var exprProjection = GenerateKeyExpression(memberOfProjection.ToArray(), 0);
			var expDetail = GenerateKeyExpression(memberOfDetail.ToArray(), 0);

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

		public static Expression GenerateDetailsExpression(IBuildContext context, Expression masterQuery, Expression detailsQuery)
		{
			var masterObjType = GetEnumerableElementType(masterQuery.Type);
			var detailObjType = GetEnumerableElementType(detailsQuery.Type);
			var masterObjParam = Expression.Parameter(masterObjType, "master");
			var keys = GenerateKeyExpressions(context, masterObjParam);

			var method = _enlistEagerLoadingFunctionality.MakeGenericMethod(masterObjType, detailObjType, keys.Item2.Type);

			var detailsKeyExpression = Expression.Lambda(keys.Item2, masterObjParam);

			var parameters = new HashSet<ParameterExpression>();
			detailsQuery.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foreach (var p in ((LambdaExpression)e).Parameters)
						parameters.Add(p);
			});

			var aliases = CollectAliases(masterQuery);

			detailsQuery = detailsQuery.Transform(e =>
			{
				switch (e.NodeType)
				{
					case ExpressionType.MemberAccess:
						{
							var root = e.GetRootObject(context.Builder.MappingSchema);

							if (root != null &&
							    root.NodeType == ExpressionType.Parameter &&
							    !parameters.Contains((ParameterExpression)root))
							{
								if (aliases.TryGetValue(e, out var member) /*&& member.DeclaringType == masterObjType*/)
								{
									return Expression.MakeMemberAccess(masterObjParam, member);
								}
							}

							break;
						}
				}

				return e;
			});


			var ienumerableType = typeof(IEnumerable<>).MakeGenericType(detailObjType);
			if (detailsQuery.Type != ienumerableType)
				detailsQuery = Expression.Convert(detailsQuery, ienumerableType);

			var detailsQueryLambda = Expression.Lambda(detailsQuery, masterObjParam);

			var eagerContext = method.Invoke(null,
				new object[] { context.Builder.DataContext, masterQuery, detailsQueryLambda, detailsKeyExpression });

			var getListMethod =
				eagerContext.GetType().GetMethod("GetList", BindingFlags.Instance | BindingFlags.Public);

			var resultExpression = Expression.Call(Expression.Constant(eagerContext), getListMethod, keys.Item1);

			return resultExpression;
		}


		private static EagerLoadingContext<TD, TKey> EnlistEagerLoadingFunctionality<T, TD, TKey>(
			IDataContext dataContext,
			Expression mainQueryExpr, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Expression<Func<T, TKey>> getKeyExpression)
		{
			var mainQuery = Internals.CreateExpressionQueryInstance<T>(dataContext, mainQueryExpr);
			var detailQuery = mainQuery.SelectMany(detailQueryExpression, (m, d) => new { MasterKey = getKeyExpression.Compile()(m), Detail = d});

			//TODO: currently we run in separate query
			var detailsWithKey = detailQuery.ToList();
			var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

			foreach (var d in detailsWithKey)
			{
				eagerLoadingContext.Add(d.MasterKey, d.Detail);
			}

			return eagerLoadingContext;
		}

		public static List<T> QueryWithDetailsProbe<T, TD>(
			IDataContext dc,
			IQueryable<T> mainQuery,
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Action<T, List<TD>> detailSetter)
		{
			var ed = dc.MappingSchema.GetEntityDescriptor(typeof(T));
			var keys = ed.Columns.Where(c => c.IsPrimaryKey).ToArray();
			if (keys.Length == 0) 
				keys = ed.Columns.Where(c => dc.MappingSchema.IsScalarType(c.MemberType)).ToArray();

			if (keys.Length == 0)
				throw new LinqToDBException($"Can not retrieve key fro type '{typeof(T).Name}'");

			var objParam = Expression.Parameter(typeof(T), "obj");
			var properties = keys.Select(k => (Expression)Expression.MakeMemberAccess(objParam, k.MemberInfo))
				.ToArray();
			var getKeyExpr = Expression.Lambda(GenerateKeyExpression(properties, 0), objParam);

			var method = _queryWithDetailsInternalMethodInfo.MakeGenericMethod(new[] { typeof(T), typeof(TD), getKeyExpr.Body.Type });

			var result = method.Invoke(null, new object[] { mainQuery, detailQueryExpression, getKeyExpr, detailSetter });
			return (List<T>)result;
		}

		private static List<T> QueryWithDetailsInternalProbe<T, TD, TKey>(
			IQueryable<T> mainQuery, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Expression<Func<T, TKey>> getKeyExpression,
			Action<T, List<TD>> detailSetter)
		{
			var detailQuery = mainQuery.SelectMany(detailQueryExpression, (m, d) => new { MasterKey = getKeyExpression.Compile().Invoke(m), Detail = d});
			var detailsWithKey = detailQuery.ToList();
			var detailDictionary = new Dictionary<TKey, List<TD>>();

			foreach (var d in detailsWithKey)
			{
				if (!detailDictionary.TryGetValue(d.MasterKey, out var list))
				{
					list = new List<TD>();
					detailDictionary.Add(d.MasterKey, list);
				}
				list.Add(d.Detail);
			}

			var mainEntities = mainQuery.ToList();
			var getKeyFunc = getKeyExpression.Compile();

			foreach (var entity in mainEntities)
			{
				var key = getKeyFunc(entity);
				if (!detailDictionary.TryGetValue(key, out var ds))
					ds = new List<TD>();
				detailSetter(entity, ds);
			}

			return mainEntities;
		}
	}
}
