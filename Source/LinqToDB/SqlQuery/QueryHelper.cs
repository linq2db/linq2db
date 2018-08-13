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
			var hashIgnore = new HashSet<IQueryElement>(ignore ?? Enumerable.Empty<IQueryElement>());

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
			if (@where.IsEmpty)
			{
				@where.SearchCondition.Conditions.AddRange(search.Conditions);
			}
			else
			{
				if (@where.SearchCondition.Precedence < Precedence.LogicalConjunction)
				{
					var sc1 = new SqlSearchCondition();

					sc1.Conditions.AddRange(@where.SearchCondition.Conditions);

					@where.SearchCondition.Conditions.Clear();
					@where.SearchCondition.Conditions.Add(new SqlCondition(false, sc1));
				}

				if (search.Precedence < Precedence.LogicalConjunction)
				{
					var sc2 = new SqlSearchCondition();

					sc2.Conditions.AddRange(search.Conditions);

					@where.SearchCondition.Conditions.Add(new SqlCondition(false, sc2));
				}
				else
					@where.SearchCondition.Conditions.AddRange(search.Conditions);
			}
		}

		public static bool IsEqualTables(SqlTable table1, SqlTable table2)
		{
			var result =
				table1                 != null
				&& table2              != null
				&& table1.ObjectType   == table2.ObjectType
				&& table1.Database     == table2.Database
				&& table1.Schema       == table2.Schema
				&& table1.Name         == table2.Name
				&& table1.PhysicalName == table2.PhysicalName;

			return result;
		}

		/// <summary>
		/// Enumerates table sources recursively based on joins
		/// </summary>
		/// <param name="query"></param>
		/// <returns></returns>
		public static IEnumerable<ISqlTableSource> EnumerateJoinedSources(SelectQuery query)
		{
			foreach (var tableSource in query.Select.From.Tables)
			{
				yield return tableSource;
				foreach (var join in tableSource.Joins)
				{
					yield return @join.Table;

					if (@join.Table.Source is SelectQuery subquery)
					{
						foreach (var ts in EnumerateJoinedSources(subquery))
						{
							yield return ts;
						}
					}
				}
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
			if (!@select.Select.IsDistinct || @select.OrderBy.IsEmpty)
				return false;

			var nonProjecting = @select.Select.OrderBy.Items.Select(i => i.Expression)
				.Except(@select.Select.Columns.Select(c => c.Expression))
				.ToList();

			if (nonProjecting.Count > 0)
			{
				if (!flags.IsOrderByAggregateFunctionsSupported)
					throw new LinqToDBException("Can not convert sequence to SQL. DISTINCT with ORDER BY not supported.");

				// converting to Group By

				var newOrderItems = @select.Select.OrderBy.Items
					.Select(oi =>
						!nonProjecting.Contains(oi.Expression)
							? oi
							: new SqlOrderByItem(
								new SqlFunction(oi.Expression.SystemType, oi.IsDescending ? "Min" : "Max", true, oi.Expression),
								oi.IsDescending))
					.ToList();

				@select.Select.OrderBy.Items.Clear();
				@select.Select.OrderBy.Items.AddRange(newOrderItems);

				// add only missing group items
				var currentGroupItems = new HashSet<ISqlExpression>(@select.Select.GroupBy.Items);
				@select.Select.GroupBy.Items.AddRange(
					@select.Select.Columns.Select(c => c.Expression)
						.Where(e => !currentGroupItems.Contains(e))
				);

				@select.Select.IsDistinct = false;

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
		/// Transforms
		///   SELECT * FROM A
		///     INNER JOIN B ON A.ID = B.ID
		/// to
		///   SELECT * FROM A, B
		///   WHERE A.ID = B.ID
		/// </summary>
		/// <param name="selectQuery">Input SelectQuery.</param>
		/// <returns>The same query insance.</returns>
		public static SelectQuery TransformInnerJoinsToWhere(this SelectQuery selectQuery)
		{
			if (selectQuery.From.Tables.Count > 0 && selectQuery.From.Tables[0] is SqlTableSource tableSource)
			{
				if (tableSource.Joins.All(j => j.JoinType == JoinType.Inner))
				{
					while (tableSource.Joins.Count > 0)
					{
						// consider to remove join and simplify query
						var join = tableSource.Joins[0];
						selectQuery.Where.ConcatSearchCondition(join.Condition);
						selectQuery.From.Tables.Add(join.Table);
						tableSource.Joins.RemoveAt(0);
					}
				}
			}

			return selectQuery;
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
					return GetUnderlyingField(column.Expression);
			}
			return null;
		}

		public static SqlCondition GenerateEquality(ISqlExpression field1, ISqlExpression field2)
		{
			var compare = new SqlCondition(false, new SqlPredicate.ExprExpr(field1, SqlPredicate.Operator.Equal, field2));

			if (field1.CanBeNull && field2.CanBeNull)
			{
				var isNull1 = new SqlCondition(false, new SqlPredicate.IsNull(field1, false));
				var isNull2 = new SqlCondition(false, new SqlPredicate.IsNull(field2, false));
				var nulls = new SqlSearchCondition(isNull1, isNull2);
				compare.IsOr = true;
				compare = new SqlCondition(false, new SqlSearchCondition(compare, new SqlCondition(false, nulls)));
			}

			return compare;
		}

		public static void GetUsedSources(ISqlExpression root, [NotNull] HashSet<ISqlTableSource> foundSorces)
		{
			if (foundSorces == null) throw new ArgumentNullException(nameof(foundSorces));

			new QueryVisitor().Visit(root, e =>
			{
				if (e is ISqlTableSource source)
					foundSorces.Add(source);
				else
					switch (e.ElementType)
					{
						case QueryElementType.Column:
						{
							var c = (SqlColumn) e;
							foundSorces.Add(c.Parent);
							break;
						}
						case QueryElementType.SqlField:
						{
							var f = (SqlField) e;
							foundSorces.Add(f.Table);
							break;
						}
					}
			});
		}

	}
}
