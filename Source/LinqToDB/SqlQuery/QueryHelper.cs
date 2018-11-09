using System;
using System.Linq;
using System.Collections.Generic;
using JetBrains.Annotations;

namespace LinqToDB.SqlQuery
{
	using SqlProvider;

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

		/// <summary>
		/// Converts ORDER BY DISTINCT to GROUP BY equivalent
		/// </summary>
		/// <param name="select"></param>
		/// <param name="flags"></param>
		/// <returns></returns>
		public static bool TryConvertOrderedDistinctToGroupBy(SelectQuery select, SqlProviderFlags flags)
		{
			if (!select.Select.IsDistinct || select.OrderBy.IsEmpty)
				return false;

			var nonProjecting = select.Select.OrderBy.Items.Select(i => i.Expression)
				.Except(select.Select.Columns.Select(c => c.Expression))
				.ToList();

			if (nonProjecting.Count > 0)
			{
				if (!flags.IsOrderByAggregateFunctionsSupported)
					throw new LinqToDBException("Can not convert sequence to SQL. DISTINCT with ORDER BY not supported.");

				// converting to Group By

				var newOrderItems = select.Select.OrderBy.Items
					.Select(oi =>
						!nonProjecting.Contains(oi.Expression)
							? oi
							: new SqlOrderByItem(
								new SqlFunction(oi.Expression.SystemType, oi.IsDescending ? "Min" : "Max", true, oi.Expression),
								oi.IsDescending))
					.ToList();

				select.Select.OrderBy.Items.Clear();
				select.Select.OrderBy.Items.AddRange(newOrderItems);

				// add only missing group items
				var currentGroupItems = new HashSet<ISqlExpression>(select.Select.GroupBy.Items);
				select.Select.GroupBy.Items.AddRange(
					select.Select.Columns.Select(c => c.Expression)
						.Where(e => !currentGroupItems.Contains(e)));

				select.Select.IsDistinct = false;

				return true;
			}

			return false;
		}

		/// <summary>
		/// Detects when we can remove order
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <param name="flags"></param>
		/// <param name="information"></param>
		/// <returns></returns>
		public static bool CanRemoveOrderBy([NotNull] SelectQuery selectQuery, SqlProviderFlags flags, QueryInformation information)
		{
			if (selectQuery == null) throw new ArgumentNullException(nameof(selectQuery));

			if (selectQuery.OrderBy.IsEmpty || selectQuery.ParentSelect == null)
				return false;

			var current = selectQuery;
			do
			{
				if (current.Select.SkipValue != null || current.Select.TakeValue != null)
				{
					return false;
				}

				if (current != selectQuery)
				{
					if (!current.OrderBy.IsEmpty || current.Select.IsDistinct)
						return true;
				}

				var info = information.GetHierarchyInfo(current);
				if (info == null)
					break;

				switch (info.HierarchyType)
				{
					case QueryInformation.HierarchyType.From:
						if (!flags.IsSubQueryOrderBySupported)
							return true;
						current = info.MasterQuery;
						break;
					case QueryInformation.HierarchyType.Join:
						return true;
					case QueryInformation.HierarchyType.Union:
						// currently removing ordering for all UNION
						return true;
					case QueryInformation.HierarchyType.InnerQuery:
						return true;
					default:
						throw new ArgumentOutOfRangeException();
				}

			} while (current != null);

			return false;
		}


		/// <summary>
		/// Detects when we can remove order
		/// </summary>
		/// <param name="selectQuery"></param>
		/// <param name="information"></param>
		/// <returns></returns>
		public static bool TryRemoveDistinct([NotNull] SelectQuery selectQuery, QueryInformation information)
		{
			if (selectQuery == null) throw new ArgumentNullException(nameof(selectQuery));

			if (!selectQuery.Select.IsDistinct)
				return false;

			var info = information.GetHierarchyInfo(selectQuery);
			switch (info?.HierarchyType)
			{
				case QueryInformation.HierarchyType.InnerQuery:
					{
						if (info.ParentElement is SqlFunction func && func.Name == "EXISTS")
						{
							// ORDER BY not needed for EXISTS function, even when Take and Skip specified
							selectQuery.Select.OrderBy.Items.Clear();

							if (selectQuery.Select.SkipValue == null && selectQuery.Select.TakeValue == null)
							{
								// we can sefely remove DISTINCT
								selectQuery.Select.IsDistinct = false;
								selectQuery.Select.Columns.Clear();
								return true;
							}
						}
					}
					break;
			}

			return false;
		}

		/// <summary>
		/// Returns SqlField from specific expression. Usually from SqlColumn.
		/// Complex expressions ignored.
		/// </summary>
		/// <param name="expression"></param>
		/// <returns>Field instance associated with expression</returns>
		public static SqlField GetUnderlyingField(ISqlExpression expression)
		{
			switch (expression)
			{
				case SqlField field:
					return field;
				case SqlColumn column:
					return GetUnderlyingField(column.Expression, new HashSet<ISqlExpression>());
			}
			return null;
		}

 		static SqlField GetUnderlyingField(ISqlExpression expression, HashSet<ISqlExpression> visited)
		{
			switch (expression)
			{
				case SqlField field:
					return field;
				case SqlColumn column:
				{
					if (visited.Contains(column))
						return null;
					visited.Add(column);
					return GetUnderlyingField(column.Expression, visited);
				}
			}
			return null;
		}

	}
}
