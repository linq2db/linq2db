using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace LinqToDB.Internal.SqlQuery
{
	public static class SelectQueryExtensions
	{
		/// <summary>
		/// Determines whether the specified query includes a WHERE clause with any conditions.
		/// </summary>
		/// <param name="selectQuery">The query to check for the presence of a WHERE clause. Cannot be null.</param>
		/// <returns>true if the query contains a WHERE clause with at least one condition; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasWhere(this SelectQuery selectQuery)
		{
			return !selectQuery.Where.SearchCondition.IsTrue;
		}

		/// <summary>
		/// Determines whether the specified query includes any GROUP BY clauses.
		/// </summary>
		/// <param name="selectQuery">The query to inspect for GROUP BY clauses. Cannot be null.</param>
		/// <returns>true if the query contains one or more GROUP BY clauses; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasGroupBy(this SelectQuery selectQuery)
		{
			return !selectQuery.GroupBy.IsEmpty;
		}

		/// <summary>
		/// Determines whether the specified select query includes a HAVING clause with a non-trivial search condition.
		/// </summary>
		/// <param name="selectQuery">The select query to check for the presence of a HAVING clause. Cannot be null.</param>
		/// <returns>true if the select query contains a HAVING clause with a condition other than always true; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasHaving(this SelectQuery selectQuery)
		{
			return !selectQuery.Having.SearchCondition.IsTrue;
		}

		/// <summary>
		/// Determines whether the specified query includes any ORDER BY clauses.
		/// </summary>
		/// <param name="selectQuery">The query to inspect for ORDER BY clauses. Cannot be null.</param>
		/// <returns>true if the query contains at least one ORDER BY clause; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool HasOrderBy(this SelectQuery selectQuery)
		{
			return !selectQuery.OrderBy.IsEmpty;
		}

		/// <summary>
		/// Determines whether the SELECT statement in the specified query is marked as DISTINCT.
		/// </summary>
		/// <param name="selectQuery">The query to evaluate for the DISTINCT modifier. Cannot be null.</param>
		/// <returns>true if the SELECT statement is DISTINCT; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsDistinct(this SelectQuery selectQuery)
		{
			return selectQuery.Select.IsDistinct;
		}

		/// <summary>
		/// Determines whether the specified query includes a limit or offset clause.
		/// </summary>
		/// <param name="selectQuery">The query to evaluate for the presence of a limit (Take) or offset (Skip) clause. Cannot be null.</param>
		/// <returns>true if the query specifies a limit or offset; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLimited(this SelectQuery selectQuery)
		{
			return selectQuery.Select.TakeValue != null || selectQuery.Select.SkipValue != null;
		}

		/// <summary>
		/// Determines whether the specified select query is a simple query without any set operators.
		/// </summary>
		/// <remarks>A simple select query is one that does not include set operations such as UNION, INTERSECT, or
		/// EXCEPT. Use this method to check if a query can be treated as a basic select statement without additional set
		/// logic.</remarks>
		/// <param name="selectQuery">The select query to evaluate. Cannot be null.</param>
		/// <returns>true if the select query is simple and does not contain set operators; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSimple(this SelectQuery selectQuery)
		{
			return selectQuery.IsSimpleOrSet() && !selectQuery.HasSetOperators;
		}

		/// <summary>
		/// Determines whether the specified query is a simple SELECT or a set operation without WHERE, GROUP BY, HAVING, or
		/// ORDER BY clauses, and with no table joins.
		/// </summary>
		/// <remarks>A simple query in this context is defined as one that does not use query modifiers, filtering,
		/// grouping, aggregation, ordering, or table joins. This method can be used to optimize processing or to apply
		/// special handling for basic queries.</remarks>
		/// <param name="selectQuery">The SELECT query to evaluate. Cannot be null.</param>
		/// <returns>true if the query is a simple SELECT or set operation with no WHERE, GROUP BY, HAVING, or ORDER BY clauses and no
		/// joins; otherwise, false.</returns>
		[DebuggerStepThrough]
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsSimpleOrSet(this SelectQuery selectQuery)
		{
			return !selectQuery.Select.HasModifier && !selectQuery.HasWhere()   && !selectQuery.HasGroupBy() 
			       && !selectQuery.HasHaving()     && !selectQuery.HasOrderBy() && selectQuery.From.Tables is [{ Joins.Count: 0 }];
		}
	}
}
