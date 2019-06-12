using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using LinqToDB.Expressions;
using LinqToDB.Extensions;
using LinqToDB.SqlQuery;

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

		//TODO: move to common static class
		static readonly MethodInfo _whereMethodInfo =
			MemberHelper.MethodOf(() => LinqExtensions.Where<int,int,object>(null,null)).GetGenericMethodDefinition();

		//TODO: move to common static class
		static readonly MethodInfo _getTableMethodInfo =
			MemberHelper.MethodOf(() => DataExtensions.GetTable<object>(null)).GetGenericMethodDefinition();

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

		static Tuple<Expression, Expression> GenerateKeyExpressions(IBuildContext context, ParameterExpression mainObj, List<MemberExpression> foundMembers)
		{
			var sql = context.ConvertToIndex(null, 0, ConvertFlags.Key);

			var memberOfProjection = new List<Expression>();
			var memberOfDetail = new List<Expression>();
			foreach (var s in sql)
			{
				Expression forDetail = null;
				if (s.MemberChain.Count == 1 && s.MemberChain[0].DeclaringType == mainObj.Type)
				{
					forDetail = Expression.MakeMemberAccess(mainObj, s.MemberChain[0]);
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
				foreach (var member in foundMembers)
				{
					var ctx = context.Builder.GetContext(context, member);
					if (ctx == null)
						continue;

					var fieldsSql = ctx.ConvertToIndex(member, 0, ConvertFlags.Field);
					if (fieldsSql.Length == 1)
					{
						var s = fieldsSql[0];
						if (s.Sql is SqlField field)
						{
							var forDetail = (Expression)Expression.MakeMemberAccess(mainObj, member.Member);
							var forProjection = ctx.Builder.BuildSql(s.Sql.SystemType, s.Index);

							if (forDetail.Type != forProjection.Type)
								forDetail = Expression.Convert(forDetail, forProjection.Type);
							memberOfDetail.Add(forDetail);

							memberOfProjection.Add(forProjection);
						}
					}
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
			var expDetail = GenerateKeyExpression(memberOfDetail.ToArray(), 0);

			return Tuple.Create(exprProjection, expDetail);
		}

		static Tuple<Expression, Expression> GenerateKeyExpressionsOld(IBuildContext context, ParameterExpression mainObj)
		{
			var sql = context.ConvertToIndex(null, 0, ConvertFlags.Key);

			var memberOfProjection = new List<Expression>();
			var memberOfDetail = new List<Expression>();
			foreach (var s in sql)
			{
				var forProjection = context.Builder.BuildSql(s.Sql.SystemType, s.Index);
				memberOfProjection.Add(forProjection);

				//TODO: more correct memberchain processing

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

		public static Expression GenerateDetailsExpression(IBuildContext context, Expression masterQuery,
			Expression detailsQuery, ParameterExpression masterObjParam, List<MemberExpression> foundMembers)
		{
			var builder = context.Builder;

			var masterObjType  = GetEnumerableElementType(masterQuery.Type);
			var detailObjType  = GetEnumerableElementType(detailsQuery.Type);
			var keyExpressions = GenerateKeyExpressions(context, masterObjParam, foundMembers);

			var detailsKeyExpression = Expression.Lambda(keyExpressions.Item2, masterObjParam);

			var parameters = new HashSet<ParameterExpression>();
			detailsQuery.Visit(e =>
			{
				if (e.NodeType == ExpressionType.Lambda)
					foreach (var p in ((LambdaExpression)e).Parameters)
						parameters.Add(p);
			});

			var ienumerableType = typeof(IEnumerable<>).MakeGenericType(detailObjType);
			if (detailsQuery.Type != ienumerableType)
				detailsQuery = Expression.Convert(detailsQuery, ienumerableType);

			var detailsQueryLambda = Expression.Lambda(detailsQuery, masterObjParam);

			var enlistMethod = EnlistEagerLoadingFunctionalityMethodInfo.MakeGenericMethod(masterObjType, detailObjType, keyExpressions.Item2.Type);

			var resultExpression = (Expression)enlistMethod.Invoke(null,
				new object[] { builder, masterQuery, detailsQueryLambda, keyExpressions.Item1, detailsKeyExpression });

			return resultExpression;
		}

		private static Expression EnlistEagerLoadingFunctionality<T, TD, TKey>(
			ExpressionBuilder builder,
			Expression mainQueryExpr, 
			Expression<Func<T, IEnumerable<TD>>> detailQueryExpression,
			Expression currentRecordKeyExpression,
			Expression<Func<T, TKey>> getKeyExpression)
		{
			var mainQuery = Internals.CreateExpressionQueryInstance<T>(builder.DataContext, mainQueryExpr);
			var detailQuery = mainQuery.SelectMany(detailQueryExpression, (m, d) => Tuple.Create(getKeyExpression.Compile()(m), d));

			//TODO: currently we run in separate query

			var idx = RegisterPreambles(builder, detailQuery);

			var getListMethod =
				typeof(EagerLoadingContext<TD, TKey>).GetMethod("GetList", BindingFlags.Instance | BindingFlags.Public);

			var resultExpression =
				Expression.Call(
					Expression.Convert(Expression.ArrayIndex(ExpressionBuilder.PreambleParam, Expression.Constant(idx)),
						typeof(EagerLoadingContext<TD, TKey>)), getListMethod, currentRecordKeyExpression);

			return resultExpression;
		}

		private static int RegisterPreambles<TD, TKey>(ExpressionBuilder builder, IQueryable<Tuple<TKey, TD>> detailQuery)
		{
			var expr = detailQuery.Expression;
			// Filler code is duplicated for the future usage with IAsyncEnumerable
			var idx = builder.RegisterPreamble(dc =>
				{
					var queryable = new ExpressionQueryImpl<Tuple<TKey, TD>>(dc, expr);
					var detailsWithKey = queryable.ToList();
					var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

					foreach (var d in detailsWithKey)
					{
						eagerLoadingContext.Add(d.Item1, d.Item2);
					}

					return eagerLoadingContext;
				},
				async dc =>
				{
					var queryable = new ExpressionQueryImpl<Tuple<TKey, TD>>(dc, expr);
					var detailsWithKey = await queryable.ToListAsync();
					var eagerLoadingContext = new EagerLoadingContext<TD, TKey>();

					foreach (var d in detailsWithKey)
					{
						eagerLoadingContext.Add(d.Item1, d.Item2);
					}

					return eagerLoadingContext;
				}
			);
			return idx;
		}

		public static bool ValidateEagerLoading(IBuildContext context, ParameterExpression masterParam, ref Expression expression)
		{
			if (context is JoinBuilder.GroupJoinContext joinContext)
				return false;

			var elementType = GetEnumerableElementType(expression.Type);

			var helperType = typeof(EagerLoadingHelper<,>).MakeGenericType(masterParam.Type, elementType);
			var helper = (EagerLoadingHelper)Activator.CreateInstance(helperType);

			if (!helper.Validate(context, masterParam, ref expression))
				return false;

			return true;
		}

		
		abstract class EagerLoadingHelper
		{
			public abstract bool Validate(IBuildContext context, ParameterExpression masterParam,
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

			public override bool Validate(IBuildContext context, ParameterExpression masterParam,
				ref Expression expression)
			{
				var detailsQuery = expression;
				var detailObjType = GetEnumerableElementType(detailsQuery.Type);

				var masterTable = new Table<TMaster>(context.Builder.DataContext);

				var ienumerableType = typeof(IEnumerable<>).MakeGenericType(detailObjType);

				if (detailsQuery.Type != ienumerableType)
					detailsQuery = Expression.Convert(detailsQuery, ienumerableType);

				var localQueryable = masterTable.SelectMany(Expression.Lambda<Func<TMaster, IEnumerable<TDetail>>>(detailsQuery, masterParam), 
					(m, d) => d);

				var queryableExpression = localQueryable.Expression;
				var localQuery = new Query<TDetail>(context.Builder.DataContext, queryableExpression);
				var localBuilder = new ExpressionBuilder(localQuery, context.Builder.DataContext, queryableExpression, null);

				var buildInfo = new BuildInfo(context, queryableExpression, new SelectQuery());
				var localSequence = localBuilder.BuildSequence(buildInfo);

				var sqlOptimizer = context.Builder.DataContext.GetSqlOptimizer();
				var statement = sqlOptimizer.Finalize(localSequence.GetResultStatement());

				if (!(statement is SqlSelectStatement selectStatement))
					return false;

				var isValid = IsSelectValid(selectStatement.SelectQuery);
				return isValid;
			}
		}
	}
}
