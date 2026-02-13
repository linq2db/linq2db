using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.SqlQuery
{
	public static class SelectQueryExtensions
	{
		extension(SelectQuery selectQuery)
		{
			/// <summary>
			/// Determines whether the specified query includes a WHERE clause with any conditions.
			/// </summary>
			/// <returns><see langword="true" /> if the query contains a WHERE clause with at least one condition; otherwise, <see langword="false" />.</returns>
			public bool HasWhere =>
				!selectQuery.Where.SearchCondition.IsTrue;

			/// <summary>
			/// Determines whether the specified query includes any GROUP BY clauses.
			/// </summary>
			/// <returns><see langword="true" /> if the query contains one or more GROUP BY clauses; otherwise, <see langword="false" />.</returns>
			public bool HasGroupBy =>
				!selectQuery.GroupBy.IsEmpty;

			/// <summary>
			/// Determines whether the specified select query includes a HAVING clause with a non-trivial search condition.
			/// </summary>
			/// <returns><see langword="true" /> if the select query contains a HAVING clause with a condition other than always true; otherwise, <see langword="false" />.</returns>
			public bool HasHaving =>
				!selectQuery.Having.SearchCondition.IsTrue;

			/// <summary>
			/// Determines whether the specified query includes any ORDER BY clauses.
			/// </summary>
			/// <returns><see langword="true" /> if the query contains at least one ORDER BY clause; otherwise, <see langword="false" />.</returns>
			public bool HasOrderBy =>
				!selectQuery.OrderBy.IsEmpty;

			/// <summary>
			/// Determines whether the SELECT statement in the specified query is marked as DISTINCT.
			/// </summary>
			/// <returns><see langword="true" /> if the SELECT statement is DISTINCT; otherwise, <see langword="false" />.</returns>
			public bool IsDistinct =>
				selectQuery.Select.IsDistinct;

			/// <summary>
			/// Determines whether the specified query includes a limit or offset clause.
			/// </summary>
			/// <returns><see langword="true" /> if the query specifies a limit or offset; otherwise, <see langword="false" />.</returns>
			public bool IsLimited =>
				selectQuery.Select is { TakeValue: not null } or { SkipValue: not null };

			/// <summary>
			/// Determines whether the specified select query is a simple query without any set operators.
			/// </summary>
			/// <remarks>A simple select query is one that does not include set operations such as UNION, INTERSECT, or
			/// EXCEPT. Use this method to check if a query can be treated as a basic select statement without additional set
			/// logic.</remarks>
			/// <returns><see langword="true" /> if the select query is simple and does not contain set operators; otherwise, <see langword="false" />.</returns>
			public bool IsSimple =>
				selectQuery is { IsSimpleOrSet: true, HasSetOperators: false };

			/// <summary>
			/// Determines whether the specified query is a simple SELECT or a set operation without WHERE, GROUP BY, HAVING, or
			/// ORDER BY clauses, and with no table joins.
			/// </summary>
			/// <remarks>A simple query in this context is defined as one that does not use query modifiers, filtering,
			/// grouping, aggregation, ordering, or table joins. This method can be used to optimize processing or to apply
			/// special handling for basic queries.</remarks>
			/// <returns><see langword="true" /> if the query is a simple SELECT or set operation with no WHERE, GROUP BY, HAVING, or ORDER BY clauses and no
			/// joins; otherwise, <see langword="false" />.</returns>
			public bool IsSimpleOrSet =>
				selectQuery is
				{
					Select.HasModifier: false,
					HasWhere: false,
					HasGroupBy: false,
					HasHaving: false,
					HasOrderBy: false,
					From.Tables: [{ Joins.Count: 0 }],
				};
		}
	}
}
