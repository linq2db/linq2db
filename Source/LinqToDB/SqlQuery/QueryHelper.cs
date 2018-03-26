using System;

namespace LinqToDB.SqlQuery
{
	using System.Collections.Generic;
	using System.Linq;

	public static class QueryHelper
	{
		public static void CollectDependencies(IQueryElement root, IEnumerable<ISqlTableSource> sources, HashSet<ISqlExpression> found, IEnumerable<IQueryElement> ignore = null)
		{
			var hash       = new HashSet<ISqlTableSource>(sources);
			var hashIgnore = new HashSet<IQueryElement  >(ignore ?? Enumerable.Empty<IQueryElement>());

			new QueryVisitor().VisitParentFirst(root, e =>
			{
				var source = e as ISqlTableSource;
				if (source != null && hash.Contains(source) || hashIgnore.Contains(e))
					return false;

				switch (e.ElementType)
				{
					case QueryElementType.Column :
						{
							var c = (SqlColumn) e;
							if (hash.Contains(c.Parent))
								found.Add(c);
							break;
						}
					case QueryElementType.SqlField :
						{
							var f = (SqlField) e;
							if (hash.Contains(f.Table))
								found.Add(f);
							break;
						}
				}
				return true;
			});
		}

		public static SelectQuery RootQuery(this SelectQuery query)
		{
			while (query.ParentSelect != null)
			{
				query = query.ParentSelect;
			}
			return query;
		}

		public static SqlJoinedTable FindJoin(this SelectQuery query,
			Func<SqlJoinedTable, bool> match)
		{
			return QueryVisitor.Find(query, e =>
			{
				if (e.ElementType == QueryElementType.JoinedTable)
				{
					if (match((SqlJoinedTable) e))
						return true;
				}
				return false;
			}) as SqlJoinedTable;
		}

		public static void ConcatSearchCondition(this SqlWhereClause where, SqlSearchCondition search)
		{
			if (where.IsEmpty)
			{
				where.SearchCondition.Conditions.AddRange(search.Conditions);
			}
			else
			{
				if (where.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc1 = new SqlSearchCondition();

					sc1.Conditions.AddRange(where.SearchCondition.Conditions);

					where.SearchCondition.Conditions.Clear();
					where.SearchCondition.Conditions.Add(new SqlCondition(false, sc1));
				}

				if (search.Precedence < Precedence.LogicalConjunction)
				{
					var sc2 = new SqlSearchCondition();

					sc2.Conditions.AddRange(search.Conditions);

					where.SearchCondition.Conditions.Add(new SqlCondition(false, sc2));
				}
				else
					where.SearchCondition.Conditions.AddRange(search.Conditions);
			}
		}
	}
}
