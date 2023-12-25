using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;

namespace LinqToDB.SqlProvider
{
	using Common;
	using DataProvider;
	using SqlQuery;

	[DataContract]
	public sealed class SqlProviderFlags
	{
		/// <summary>
		/// Enables fix for incorrect Sybase ASE behavior for following query:
		/// <code>
		/// -- will return single record with 0 value (incorrect)
		/// SELECT 0 as [c1] FROM [Child] [t1] GROUP BY [t1].[ParentID]
		/// </code>
		/// Fix enables following SQL generation:
		/// <code>
		/// -- works correctly
		/// SELECT [t1].[ParentID] as [c1] FROM [Child] [t1] GROUP BY [t1].[ParentID]
		/// </code>
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order =  1)]
		public bool        IsSybaseBuggyGroupBy           { get; set; }

		/// <summary>
		/// Indicates that provider (not database!) uses positional parameters instead of named parameters (parameter values assigned in order they appear in query, not by parameter name).
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order =  2)]
		public bool        IsParameterOrderDependent      { get; set; }

		/// <summary>
		/// Indicates that TAKE/TOP/LIMIT could accept parameter.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order =  3)]
		public bool        AcceptsTakeAsParameter         { get; set; }
		/// <summary>
		/// Indicates that TAKE/LIMIT could accept parameter only if also SKIP/OFFSET specified.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order =  4)]
		public bool        AcceptsTakeAsParameterIfSkip   { get; set; }
		/// <summary>
		/// Indicates support for TOP/TAKE/LIMIT paging clause.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order =  5)]
		public bool        IsTakeSupported                { get; set; }
		/// <summary>
		/// Indicates support for SKIP/OFFSET paging clause (parameter) without TAKE clause.
		/// Provider could set this flag even if database not support it if emulates missing functionality.
		/// E.g. : <c>TAKE [MAX_ALLOWED_VALUE] SKIP skip_value </c>
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order =  6)]
		public bool        IsSkipSupported                { get; set; }
		/// <summary>
		/// Indicates support for SKIP/OFFSET paging clause (parameter) only if also TAKE/LIMIT specified.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order =  7)]
		public bool        IsSkipSupportedIfTake          { get; set; }
		/// <summary>
		/// Indicates supported TAKE/LIMIT hints.
		/// Default (set by <see cref="DataProviderBase"/>): <c>null</c> (none).
		/// </summary>
		[DataMember(Order =  8)]
		public TakeHints?  TakeHintsSupported              { get; set; }
		/// <summary>
		/// Indicates support for paging clause in subquery.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order =  9)]
		public bool        IsSubQueryTakeSupported        { get; set; }

		/// <summary>
		/// Indicates support for skip clause in subquery.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order =  10)]
		public bool        IsSubQuerySkipSupported        { get; set; }

		/// <summary>
		/// Indicates support for scalar subquery in select list.
		/// E.g. <c>SELECT (SELECT TOP 1 value FROM some_table) AS MyColumn, ...</c>
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 11)]
		public bool        IsSubQueryColumnSupported      { get; set; }
		/// <summary>
		/// Indicates support of <c>ORDER BY</c> clause in sub-queries.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 12)]
		public bool        IsSubQueryOrderBySupported     { get; set; }
		/// <summary>
		/// Indicates that database supports count subquery as scalar in column.
		/// <code>SELECT (SELECT COUNT(*) FROM some_table) FROM ...</code>
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 13)]
		public bool        IsCountSubQuerySupported       { get; set; }

		/// <summary>
		/// Indicates that provider requires explicit output parameter for insert with identity queries to get identity from database.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 14)]
		public bool        IsIdentityParameterRequired    { get; set; }
		/// <summary>
		/// Indicates support for OUTER/CROSS APPLY.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 15)]
		public bool        IsApplyJoinSupported           { get; set; }
		/// <summary>
		/// Indicates support for OUTER/CROSS APPLY supports condition LATERAL JOIN for example.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 16)]
		public bool IsApplyJoinSupportsCondition { get; set; }
		/// <summary>
		/// Indicates support for single-query insert-or-update operation support.
		/// Otherwise two separate queries used to emulate operation (update, then insert if nothing found to update).
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 17)]
		public bool        IsInsertOrUpdateSupported      { get; set; }
		/// <summary>
		/// Indicates that provider could share parameter between statements in multi-statement batch.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 18)]
		public bool        CanCombineParameters           { get; set; }
		/// <summary>
		/// Specifies limit of number of values in single <c>IN</c> predicate without splitting it into several IN's.
		/// Default (set by <see cref="DataProviderBase"/>): <c>int.MaxValue</c> (basically means there is no limit).
		/// </summary>
		[DataMember(Order = 19)]
		public int         MaxInListValuesCount           { get; set; }
		/// <summary>
		/// Indicates that SET clause in update statement could use table alias prefix for set columns (lvalue): <c> SET t_alias.field = value</c>.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 20)]
		public bool        IsUpdateSetTableAliasSupported { get; set; }

		/// <summary>
		/// If <c>true</c>, removed record fields in OUTPUT clause of DELETE statement should be referenced using
		/// table with special name (e.g. DELETED or OLD). Otherwise fields should be referenced using target table.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 21)]
		public bool        OutputDeleteUseSpecialTable    { get; set; }
		/// <summary>
		/// If <c>true</c>, added record fields in OUTPUT clause of INSERT statement should be referenced using
		/// table with special name (e.g. INSERTED or NEW). Otherwise fields should be referenced using target table.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 22)]
		public bool        OutputInsertUseSpecialTable    { get; set; }
		/// <summary>
		/// If <c>true</c>, OUTPUT clause supports both OLD and NEW data in UPDATE statement using tables with special names.
		/// Otherwise only current record fields (after update) available using target table.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 23)]
		public bool        OutputUpdateUseSpecialTables   { get; set; }

		/// <summary>
		/// Provider requires that selected subquery column must be used in group by even for constant column.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 24)]
		public bool        IsGroupByColumnRequred            { get; set; }

		/// <summary>
		/// Indicates support for CROSS JOIN.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 25)]
		public bool        IsCrossJoinSupported              { get; set; }

		/// <summary>
		/// Indicates support for CROSS JOIN emulation using <c>INNER JOIN a ON 1 = 1</c>.
		/// Currently has no effect if <see cref="IsCrossJoinSupported"/> enabled but it is recommended to use proper value.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 26)]
		public bool IsInnerJoinAsCrossSupported           { get; set; }

		/// <summary>
		/// Indicates support of CTE expressions.
		/// If provider does not support CTE, unsuported exception will be thrown when using CTE.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 27)]
		public bool IsCommonTableExpressionsSupported     { get; set; }

		/// <summary>
		/// Indicates that database supports and correctly handles DISTINCT queries with ORDER BY over fields missing from projection.
		/// Otherwise:
		/// <list>
		/// <item>if <see cref="Configuration.Linq.KeepDistinctOrdered"/> is set: query will be converted to GROUP BY query</item>
		/// <item>if <see cref="Configuration.Linq.KeepDistinctOrdered"/> is not set: non-projected columns will be removed from ordering</item>
		/// </list>
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 28)]
		public bool IsDistinctOrderBySupported            { get; set; }

		/// <summary>
		/// Indicates support for aggregate functions in ORDER BY statement.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 29)]
		public bool IsOrderByAggregateFunctionsSupported  { get; set; }

		/// <summary>
		/// Provider supports EXCEPT ALL, INTERSECT ALL set operators. Otherwise they will be emulated.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 30)]
		public bool IsAllSetOperationsSupported           { get; set; }

		/// <summary>
		/// Provider supports EXCEPT, INTERSECT set operators. Otherwise it will be emulated.
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 31)]
		public bool IsDistinctSetOperationsSupported      { get; set; }

		/// <summary>
		/// Provider supports COUNT(DISTINCT column) function. Otherwise it will be emulated.
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 32)]
		public bool IsCountDistinctSupported              { get; set; }

		/// <summary>
		/// Provider supports aggregated expression with Outer reference
		/// <code>
		/// SELECT
		/// (
		///		SELECT SUM(inner.FieldX + outer.FieldOuter)
		///		FROM table2 inner
		/// ) AS Sum_Column
		/// FROM table1 outer
		///</code>
		/// Otherwise aggeragated expression will be wrapped in subquery and aggregate function will be applied to subquery column.
		/// <code>
		/// SELECT
		/// (
		///		SELECT
		///			SUM(sub.Column)
		///		FROM
		///			(
		///				SELECT inner.FieldX + outer.FieldOuter AS Column
		///				FROM table2 inner
		///			) sub
		/// ) AS Sum_Column
		/// FROM table1 outer
		///</code>
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 33)]
		public bool AcceptsOuterExpressionInAggregate { get; set; }

		/// <summary>
		/// Indicates support for following UPDATE syntax:
		/// <code>
		/// UPDATE A
		/// SET ...
		/// FROM B
		/// </code>
		/// Default (set by <see cref="DataProviderBase"/>): <c>true</c>.
		/// </summary>
		[DataMember(Order = 34)]
		public bool IsUpdateFromSupported             { get; set; }

		/// <summary>
		/// Provider supports Naming Query Blocks
		/// <code>
		/// QB_NAME(qb)
		/// </code>
		/// Default (set by <see cref="DataProviderBase"/>): <c>false</c>.
		/// </summary>
		[DataMember(Order = 35)]
		public bool IsNamingQueryBlockSupported       { get; set; }

		/// <summary>
		/// Indicates that provider supports window functions
		/// </summary>
		[DataMember(Order = 36)]
		public bool IsWindowFunctionsSupported { get; set; }

		public bool GetAcceptsTakeAsParameterFlag(SelectQuery selectQuery)
		{
			return AcceptsTakeAsParameter || AcceptsTakeAsParameterIfSkip && selectQuery.Select.SkipValue != null;
		}

		public bool GetIsSkipSupportedFlag(ISqlExpression? takeExpression, ISqlExpression? skipExpression)
		{
			return IsSkipSupported || IsSkipSupportedIfTake && takeExpression != null;
		}

		public bool GetIsTakeHintsSupported(TakeHints hints)
		{
			if (TakeHintsSupported == null)
				return false;

			return (TakeHintsSupported.Value & hints) == hints;
		}

		/// <summary>
		/// Used when there is query which needs several additional database requests for completing query (e.g. eager load or client-side GroupBy).
		/// Default (set by <see cref="DataProviderBase"/>): <see cref="IsolationLevel.RepeatableRead"/>.
		/// </summary>
		[DataMember(Order = 37)]
		public IsolationLevel DefaultMultiQueryIsolationLevel { get; set; }

		/// <summary>
		/// Provider support Row Constructor `(1, 2, 3)` in various positions (flags)
		/// Default (set by <see cref="DataProviderBase"/>): <see cref="RowFeature.None"/>.
		/// </summary>
		[DataMember(Order = 38), DefaultValue(RowFeature.None)]
		public RowFeature RowConstructorSupport { get; set; }

		/// <summary>
		/// Flags for use by external providers.
		/// </summary>
		[DataMember(Order = 39)]
		public List<string> CustomFlags { get; set; } = new List<string>();

		[DataMember(Order = 40)]
		public bool DoesNotSupportCorrelatedSubquery { get; set; }

		[DataMember(Order = 41)]
		public bool IsExistsPreferableForContains   { get; set; }

		[DataMember(Order = 42), DefaultValue(true)]
		public bool IsProjectionBoolSupported { get; set; } = true;

		/// <summary>
		/// Provider supports ROW_NUMBER OVER () without ORDER BY
		/// </summary>
		[DataMember(Order = 43), DefaultValue(true)]
		public bool IsRowNumberWithoutOrderBySupported { get; set; } = true;

		/// <summary>
		/// Provider supports condition in subquery which references parent table
		/// </summary>
		[DataMember(Order = 44), DefaultValue(true)]
		public bool IsSubqueryWithParentReferenceInJoinConditionSupported { get; set; } = true;

		/// <summary>
		/// Provider supports column subqueries which references outer scope when nesting is more than 1
		/// </summary>
		[DataMember(Order = 45), DefaultValue(true)]
		public bool IsColumnSubqueryWithParentReferenceSupported { get; set; } = true;

		/// <summary>
		/// Provider supports INNER JOIN with condition inside Recursive CTE, currently not supported only by DB2
		/// </summary>
		[DataMember(Order = 46), DefaultValue(true)]
		public bool IsRecursiveCTEJoinWithConditionSupported { get; set; } = true;

		/// <summary>
		/// Provider supports INNER JOIN inside OUTER JOIN. For example:
		/// <code>
		/// LEFT JOIN table1 ON ...
		///	   INNER JOIN query ON ...
		/// </code>
		///
		/// Otherwise the following query will be left:
		/// <code>
		/// LEFT JOIN (
		///	   SELECT ...
		///	   FROM table1
		///	   INNER JOIN query ON ...
		/// )
		/// </code>
		/// Default: <c>true</c>.
		/// <remarks>
		/// Currently not supported only by Access.
		/// </remarks>
		/// </summary>
		[DataMember(Order = 47), DefaultValue(true)]
		public bool IsOuterJoinSupportsInnerJoin { get; set; } = true;

		/// <summary>
		/// Indicates that provider supports JOINS in FROM clause which have several tables
		/// <code>
		/// SELECT ...
		/// FROM table1
		///	   INNER JOIN query ON ...
		///    , table2
		/// </code>
		/// Otherwise the following query will be generated:
		/// <code>
		/// SELECT ...
		/// FROM (
		///		SELECT ...
		///		FROM table1, table2
		/// ) S
		///	   INNER JOIN query ON ... ,
		/// FROM table2
		/// </code>
		/// Default: <c>true</c>.
		/// <remarks>
		/// Currently not supported only by Access.
		/// </remarks>
		/// </summary>
		[DataMember(Order = 48), DefaultValue(true)]
		public bool IsMultiTablesSupportsJoins { get; set; } = true;

		/// <summary>
		/// Indicates that top level CTE query supports ORDER BY.
		/// </summary>
		[DataMember(Order = 49), DefaultValue(true)]
		public bool IsCTESupportsOrdering { get; set; } = true;

		/// <summary>
		/// Indicates that provider has bug in LEFT join translator.
		/// In the following example <b>can_be_null</b> is always not null, which is wrong.
		/// <code>
		/// SELECT
		///		can_be_null
		/// FROM Some
		/// LEFT JOIN 1 as can_be_null, * FROM Other
		///		ON ...
		/// </code>
		/// As workaround translator is trying to check for nullability all projected fields.
		/// </summary>
		[DataMember(Order =  50)]
		public bool IsAccessBuggyLeftJoinConstantNullability { get; set; }

		#region Equality
		// equality support currently needed for remote context to avoid incorrect use of cached dependent types
		// with different flags
		// https://github.com/linq2db/linq2db/issues/1445
		public override int GetHashCode()
		{
			return IsSybaseBuggyGroupBy                                .GetHashCode()
				^ IsParameterOrderDependent                            .GetHashCode()
				^ AcceptsTakeAsParameter                               .GetHashCode()
				^ AcceptsTakeAsParameterIfSkip                         .GetHashCode()
				^ IsTakeSupported                                      .GetHashCode()
				^ IsSkipSupported                                      .GetHashCode()
				^ IsSkipSupportedIfTake                                .GetHashCode()
				^ IsSubQueryTakeSupported                              .GetHashCode()
				^ IsSubQuerySkipSupported                              .GetHashCode()
				^ IsSubQueryColumnSupported                            .GetHashCode()
				^ IsSubQueryOrderBySupported                           .GetHashCode()
				^ IsCountSubQuerySupported                             .GetHashCode()
				^ IsIdentityParameterRequired                          .GetHashCode()
				^ IsApplyJoinSupported                                 .GetHashCode()
				^ IsInsertOrUpdateSupported                            .GetHashCode()
				^ CanCombineParameters                                 .GetHashCode()
				^ MaxInListValuesCount                                 .GetHashCode()
				^ IsUpdateSetTableAliasSupported                       .GetHashCode()
				^ (TakeHintsSupported?                                 .GetHashCode() ?? 0)
				^ IsGroupByColumnRequred                               .GetHashCode()
				^ IsCrossJoinSupported                                 .GetHashCode()
				^ IsInnerJoinAsCrossSupported                          .GetHashCode()
				^ IsCommonTableExpressionsSupported                    .GetHashCode()
				^ IsDistinctOrderBySupported                           .GetHashCode()
				^ IsOrderByAggregateFunctionsSupported                 .GetHashCode()
				^ IsAllSetOperationsSupported                          .GetHashCode()
				^ IsDistinctSetOperationsSupported                     .GetHashCode()
				^ IsCountDistinctSupported                             .GetHashCode()
				^ IsUpdateFromSupported                                .GetHashCode()
				^ DefaultMultiQueryIsolationLevel                      .GetHashCode()
				^ AcceptsOuterExpressionInAggregate                    .GetHashCode()
				^ IsNamingQueryBlockSupported                          .GetHashCode()
				^ RowConstructorSupport                                .GetHashCode()
				^ OutputDeleteUseSpecialTable                          .GetHashCode()
				^ OutputInsertUseSpecialTable                          .GetHashCode()
				^ OutputUpdateUseSpecialTables                         .GetHashCode()
				^ DoesNotSupportCorrelatedSubquery                     .GetHashCode()
				^ IsExistsPreferableForContains                        .GetHashCode()
				^ IsProjectionBoolSupported                            .GetHashCode()
				^ IsRowNumberWithoutOrderBySupported                   .GetHashCode()
				^ IsSubqueryWithParentReferenceInJoinConditionSupported.GetHashCode()
				^ IsColumnSubqueryWithParentReferenceSupported         .GetHashCode()
				^ IsRecursiveCTEJoinWithConditionSupported             .GetHashCode()
				^ IsOuterJoinSupportsInnerJoin                         .GetHashCode()
				^ IsMultiTablesSupportsJoins                           .GetHashCode()
				^ IsCTESupportsOrdering                                .GetHashCode()
				^ IsAccessBuggyLeftJoinConstantNullability             .GetHashCode()
				^ CustomFlags.Aggregate(0, (hash, flag) => flag.GetHashCode() ^ hash);
	}

		public override bool Equals(object? obj)
		{
			return obj is SqlProviderFlags other
				&& IsSybaseBuggyGroupBy                                  == other.IsSybaseBuggyGroupBy
				&& IsParameterOrderDependent                             == other.IsParameterOrderDependent
				&& AcceptsTakeAsParameter                                == other.AcceptsTakeAsParameter
				&& AcceptsTakeAsParameterIfSkip                          == other.AcceptsTakeAsParameterIfSkip
				&& IsTakeSupported                                       == other.IsTakeSupported
				&& IsSkipSupported                                       == other.IsSkipSupported
				&& IsSkipSupportedIfTake                                 == other.IsSkipSupportedIfTake
				&& IsSubQueryTakeSupported                               == other.IsSubQueryTakeSupported
				&& IsSubQuerySkipSupported                               == other.IsSubQuerySkipSupported
				&& IsSubQueryColumnSupported                             == other.IsSubQueryColumnSupported
				&& IsSubQueryOrderBySupported                            == other.IsSubQueryOrderBySupported
				&& IsCountSubQuerySupported                              == other.IsCountSubQuerySupported
				&& IsIdentityParameterRequired                           == other.IsIdentityParameterRequired
				&& IsApplyJoinSupported                                  == other.IsApplyJoinSupported
				&& IsInsertOrUpdateSupported                             == other.IsInsertOrUpdateSupported
				&& CanCombineParameters                                  == other.CanCombineParameters
				&& MaxInListValuesCount                                  == other.MaxInListValuesCount
				&& IsUpdateSetTableAliasSupported                        == other.IsUpdateSetTableAliasSupported
				&& TakeHintsSupported                                    == other.TakeHintsSupported
				&& IsGroupByColumnRequred                                == other.IsGroupByColumnRequred
				&& IsCrossJoinSupported                                  == other.IsCrossJoinSupported
				&& IsInnerJoinAsCrossSupported                           == other.IsInnerJoinAsCrossSupported
				&& IsCommonTableExpressionsSupported                     == other.IsCommonTableExpressionsSupported
				&& IsDistinctOrderBySupported                            == other.IsDistinctOrderBySupported
				&& IsOrderByAggregateFunctionsSupported                  == other.IsOrderByAggregateFunctionsSupported
				&& IsAllSetOperationsSupported                           == other.IsAllSetOperationsSupported
				&& IsDistinctSetOperationsSupported                      == other.IsDistinctSetOperationsSupported
				&& IsCountDistinctSupported                              == other.IsCountDistinctSupported
				&& IsUpdateFromSupported                                 == other.IsUpdateFromSupported
				&& DefaultMultiQueryIsolationLevel                       == other.DefaultMultiQueryIsolationLevel
				&& AcceptsOuterExpressionInAggregate                     == other.AcceptsOuterExpressionInAggregate
				&& IsNamingQueryBlockSupported                           == other.IsNamingQueryBlockSupported
				&& RowConstructorSupport                                 == other.RowConstructorSupport
				&& OutputDeleteUseSpecialTable                           == other.OutputDeleteUseSpecialTable
				&& OutputInsertUseSpecialTable                           == other.OutputInsertUseSpecialTable
				&& OutputUpdateUseSpecialTables                          == other.OutputUpdateUseSpecialTables
				&& DoesNotSupportCorrelatedSubquery                      == other.DoesNotSupportCorrelatedSubquery
				&& IsExistsPreferableForContains                         == other.IsExistsPreferableForContains
				&& IsProjectionBoolSupported                             == other.IsProjectionBoolSupported
				&& IsRowNumberWithoutOrderBySupported                    == other.IsRowNumberWithoutOrderBySupported
				&& IsSubqueryWithParentReferenceInJoinConditionSupported == other.IsSubqueryWithParentReferenceInJoinConditionSupported
				&& IsColumnSubqueryWithParentReferenceSupported          == other.IsColumnSubqueryWithParentReferenceSupported
				&& IsRecursiveCTEJoinWithConditionSupported              == other.IsRecursiveCTEJoinWithConditionSupported
				&& IsOuterJoinSupportsInnerJoin                          == other.IsOuterJoinSupportsInnerJoin
				&& IsMultiTablesSupportsJoins                            == other.IsMultiTablesSupportsJoins
				&& IsCTESupportsOrdering                                 == other.IsCTESupportsOrdering
				&& IsAccessBuggyLeftJoinConstantNullability              == other.IsAccessBuggyLeftJoinConstantNullability
				// CustomFlags as List wasn't best idea
				&& CustomFlags.Count                                     == other.CustomFlags.Count
				&& (CustomFlags.Count                                    == 0
					|| CustomFlags.OrderBy(_ => _).SequenceEqual(other.CustomFlags.OrderBy(_ => _)));
		}
		#endregion
	}
}
