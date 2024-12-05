using System.Collections.Generic;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB.Linq;
using LinqToDB.Linq.Internal;
using LinqToDB.SqlQuery;

namespace Tests
{
	public static class QueryUtils
	{
		public static SqlStatement GetStatement<T>(this IQueryable<T> query)
		{
			var eq          = (IExpressionQuery)query;
			var expressions = (IQueryExpressions)new RuntimeExpressionsContainer(eq.Expression);
			var info        = Query<T>.GetQuery(eq.DataContext, ref expressions, out _);

			InitParameters(eq, info, expressions);

			return info.GetQueries().Single().Statement;
		}

		private static void InitParameters<T>(IExpressionQuery eq, Query<T> info, IQueryExpressions expressions)
		{
			eq.DataContext.GetQueryRunner(info, eq.DataContext, 0, expressions, null, null).GetSqlText(null);
		}

		public static SelectQuery GetSelectQuery<T>(this IQueryable<T> query)
		{
			return query.GetStatement().SelectQuery!;
		}

		public static IEnumerable<SelectQuery> EnumQueries<T>([NoEnumeration] this IQueryable<T> query)
		{
			var selectQuery = query.GetSelectQuery();
			var information = new QueryInformation(selectQuery);
			return information.GetQueriesParentFirst();
		}

		public static IEnumerable<SqlJoinedTable> EnumJoins(this SelectQuery query)
		{
			return query.From.Tables.SelectMany(t => t.Joins);
		}

		public static SqlSearchCondition GetWhere<T>(this IQueryable<T> query)
		{
			return GetSelectQuery(query).Where.SearchCondition;
		}

		public static SqlSearchCondition GetWhere(this SelectQuery selectQuery)
		{
			return selectQuery.Where.SearchCondition;
		}

		public static SqlTableSource GetTableSource(this SelectQuery selectQuery)
		{
			return selectQuery.From.Tables.Single();
		}

		public static SqlTableSource GetTableSource<T>(this IQueryable<T> query)
		{
			return GetSelectQuery(query).From.Tables.Single();
		}

		public static long GetCacheMissCount<T>(this IQueryable<T> _)
		{
			return Query<T>.CacheMissCount;
		}

		public static void ClearCache<T>(this IQueryable<T> _)
		{
			Query<T>.ClearCache();
		}

		public static SqlParameter[] CollectParameters(this SqlStatement statement)
		{
			var parametersHash = new HashSet<SqlParameter>();

			statement.VisitAll(parametersHash, static (parametersHash, expr) =>
			{
				switch (expr.ElementType)
				{
					case QueryElementType.SqlParameter:
					{
						var p = (SqlParameter)expr;
						if (p.IsQueryParameter)
							parametersHash.Add(p);

						break;
					}
				}
			});

			return parametersHash.ToArray();
		}

	}
}
