using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Linq;
using LinqToDB.SqlQuery;

namespace Tests
{
	public static class QueryUtils
	{
		public static SqlStatement GetStatement<T>(this IQueryable<T> query)
		{
			var eq = (IExpressionQuery)query;
			var expression = eq.Expression;
			var info = Query<T>.GetQuery(eq.DataContext, ref expression);

			return info.Queries.Single().Statement;
		}

		public static SelectQuery GetSelectQuery<T>(this IQueryable<T> query)
		{
			return query.GetStatement().SelectQuery;
		}

		public static IEnumerable<SelectQuery> EnumQueries<T>([NoEnumeration] this IQueryable<T> query)
		{
			var selectQuery = query.GetSelectQuery();
			var information = new QueryInformation(selectQuery);
			return information.GetQueriesParentFirst();
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
	}
}
