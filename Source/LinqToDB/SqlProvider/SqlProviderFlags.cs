using System;
using System.Data;
using System.Linq;

namespace LinqToDB.SqlProvider
{
	using System.Collections.Generic;
	using SqlQuery;

	public class SqlProviderFlags
	{
		public bool        IsSybaseBuggyGroupBy              { get; set; }

		public bool        IsParameterOrderDependent          { get; set; }
		public bool        AcceptsTakeAsParameter             { get; set; }
		public bool        AcceptsTakeAsParameterIfSkip       { get; set; }
		public bool        IsTakeSupported                    { get; set; }
		public bool        IsSkipSupported                    { get; set; }
		public bool        IsSkipSupportedIfTake              { get; set; }
		public bool        IsSubQueryTakeSupported            { get; set; }
		public bool        IsSubQueryColumnSupported          { get; set; }
		public bool        IsSubQueryOrderBySupported         { get; set; }
		public bool        IsCountSubQuerySupported           { get; set; }
		public bool        IsIdentityParameterRequired        { get; set; }
		public bool        IsApplyJoinSupported               { get; set; }
		public bool        IsInsertOrUpdateSupported          { get; set; }
		public bool        CanCombineParameters               { get; set; }
		public int         MaxInListValuesCount               { get; set; }
		public bool        IsUpdateSetTableAliasSupported     { get; set; }
		public TakeHints?  TakeHintsSupported                 { get; set; }

		/// <summary>
		/// If <c>true</c>, removed record fields in OUTPUT clause of DELETE statement should be referenced using
		/// table with special name (e.g. DELETED or OLD). Otherwise fields should be referenced using target table.
		/// </summary>
		public bool        OutputDeleteUseSpecialTable       { get; set; }
		/// <summary>
		/// If <c>true</c>, added record fields in OUTPUT clause of INSERT statement should be referenced using
		/// table with special name (e.g. INSERTED or NEW). Otherwise fields should be referenced using target table.
		/// </summary>
		public bool        OutputInsertUseSpecialTable       { get; set; }
		/// <summary>
		/// If <c>true</c>, OUTPUT clause supports both OLD and NEW data in UPDATE statement using tables with special names.
		/// Otherwise only current record fields (after update) available using target table.
		/// </summary>
		public bool        OutputUpdateUseSpecialTables      { get; set; }

		/// <summary>
		/// Provider requires that selected subquery column must be used in group by even for constant column.
		/// </summary>
		public bool        IsGroupByColumnRequred         { get; set; }

		/// <summary>
		/// Provider supports:
		/// CROSS JOIN a Supported
		/// </summary>
		public bool IsCrossJoinSupported                  { get; set; }

		/// <summary>
		/// Provider supports:
		/// INNER JOIN a ON 1 = 1 
		/// </summary>
		public bool IsInnerJoinAsCrossSupported           { get; set; }

		/// <summary>
		/// Provider supports CTE expressions.
		/// If provider does not support CTE, unsuported exception will be thrown when using CTE.
		/// </summary>
		public bool IsCommonTableExpressionsSupported     { get; set; }

		/// <summary>
		/// Provider supports DISTINCT and ORDER BY with fields that are not in projection.
		/// </summary>
		public bool IsDistinctOrderBySupported            { get; set; }

		/// <summary>
		/// Provider supports aggregate functions in ORDER BY statement.
		/// </summary>
		public bool IsOrderByAggregateFunctionsSupported  { get; set; }

		/// <summary>
		/// Provider supports EXCEPT ALL, INTERSECT ALL set operators. Otherwise it will be emulated.
		/// </summary>
		public bool IsAllSetOperationsSupported           { get; set; }

		/// <summary>
		/// Provider supports EXCEPT, INTERSECT set operators. Otherwise it will be emulated.
		/// </summary>
		public bool IsDistinctSetOperationsSupported      { get; set; }

		/// <summary>
		/// Provider supports COUNT(DISTINCT column) function. Otherwise it will be emulated.
		/// </summary>
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
		/// </summary>
		public bool AcceptsOuterExpressionInAggregate { get; set; }

		/// <summary>
		/// Provider supports
		/// <code>
		/// UPDATE A
		/// SET ...
		/// FROM B
		/// </code> syntax
		/// </summary>
		public bool IsUpdateFromSupported                 { get; set; }

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
		/// Used when there is query which needs several additional database request for completing query.
		/// Default is <see cref="IsolationLevel.RepeatableRead"/>
		/// </summary>
		public IsolationLevel DefaultMultiQueryIsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

		/// <summary>
		/// Flags for use by external providers.
		/// </summary>
		public List<string> CustomFlags { get; } = new List<string>();

		#region Equality
		// equality support currently needed for remote context to avoid incorrect use of cached dependent types
		// with different flags
		// https://github.com/linq2db/linq2db/issues/1445
		public override int GetHashCode()
		{
			return IsSybaseBuggyGroupBy                        .GetHashCode()
				^ IsParameterOrderDependent                    .GetHashCode()
				^ AcceptsTakeAsParameter                       .GetHashCode()
				^ AcceptsTakeAsParameterIfSkip                 .GetHashCode()
				^ IsTakeSupported                              .GetHashCode()
				^ IsSkipSupported                              .GetHashCode()
				^ IsSkipSupportedIfTake                        .GetHashCode()
				^ IsSubQueryTakeSupported                      .GetHashCode()
				^ IsSubQueryColumnSupported                    .GetHashCode()
				^ IsSubQueryOrderBySupported                   .GetHashCode()
				^ IsCountSubQuerySupported                     .GetHashCode()
				^ IsIdentityParameterRequired                  .GetHashCode()
				^ IsApplyJoinSupported                         .GetHashCode()
				^ IsInsertOrUpdateSupported                    .GetHashCode()
				^ CanCombineParameters                         .GetHashCode()
				^ MaxInListValuesCount                         .GetHashCode()
				^ IsUpdateSetTableAliasSupported               .GetHashCode()
				^ (TakeHintsSupported?                         .GetHashCode() ?? 0)
				^ IsGroupByColumnRequred                       .GetHashCode()
				^ IsCrossJoinSupported                         .GetHashCode()
				^ IsInnerJoinAsCrossSupported                  .GetHashCode()
				^ IsCommonTableExpressionsSupported            .GetHashCode()
				^ IsDistinctOrderBySupported                   .GetHashCode()
				^ IsOrderByAggregateFunctionsSupported         .GetHashCode()
				^ IsAllSetOperationsSupported                  .GetHashCode()
				^ IsDistinctSetOperationsSupported             .GetHashCode()
				^ IsCountDistinctSupported                     .GetHashCode()
				^ IsUpdateFromSupported                        .GetHashCode()
				^ DefaultMultiQueryIsolationLevel              .GetHashCode()
				^ AcceptsOuterExpressionInAggregate            .GetHashCode()
				^ OutputDeleteUseSpecialTable                  .GetHashCode()
				^ OutputInsertUseSpecialTable                  .GetHashCode()
				^ OutputUpdateUseSpecialTables                 .GetHashCode()
				^ CustomFlags.Aggregate(0, (hash, flag) => flag.GetHashCode() ^ hash);
	}

		public override bool Equals(object? obj)
		{
			return obj is SqlProviderFlags other
				&& IsSybaseBuggyGroupBy                 == other.IsSybaseBuggyGroupBy
				&& IsParameterOrderDependent            == other.IsParameterOrderDependent
				&& AcceptsTakeAsParameter               == other.AcceptsTakeAsParameter
				&& AcceptsTakeAsParameterIfSkip         == other.AcceptsTakeAsParameterIfSkip
				&& IsTakeSupported                      == other.IsTakeSupported
				&& IsSkipSupported                      == other.IsSkipSupported
				&& IsSkipSupportedIfTake                == other.IsSkipSupportedIfTake
				&& IsSubQueryTakeSupported              == other.IsSubQueryTakeSupported
				&& IsSubQueryColumnSupported            == other.IsSubQueryColumnSupported
				&& IsSubQueryOrderBySupported           == other.IsSubQueryOrderBySupported
				&& IsCountSubQuerySupported             == other.IsCountSubQuerySupported
				&& IsIdentityParameterRequired          == other.IsIdentityParameterRequired
				&& IsApplyJoinSupported                 == other.IsApplyJoinSupported
				&& IsInsertOrUpdateSupported            == other.IsInsertOrUpdateSupported
				&& CanCombineParameters                 == other.CanCombineParameters
				&& MaxInListValuesCount                 == other.MaxInListValuesCount
				&& IsUpdateSetTableAliasSupported       == other.IsUpdateSetTableAliasSupported
				&& TakeHintsSupported                   == other.TakeHintsSupported
				&& IsGroupByColumnRequred               == other.IsGroupByColumnRequred
				&& IsCrossJoinSupported                 == other.IsCrossJoinSupported
				&& IsInnerJoinAsCrossSupported          == other.IsInnerJoinAsCrossSupported
				&& IsCommonTableExpressionsSupported    == other.IsCommonTableExpressionsSupported
				&& IsDistinctOrderBySupported           == other.IsDistinctOrderBySupported
				&& IsOrderByAggregateFunctionsSupported == other.IsOrderByAggregateFunctionsSupported
				&& IsAllSetOperationsSupported          == other.IsAllSetOperationsSupported
				&& IsDistinctSetOperationsSupported     == other.IsDistinctSetOperationsSupported
				&& IsCountDistinctSupported             == other.IsCountDistinctSupported
				&& IsUpdateFromSupported                == other.IsUpdateFromSupported
				&& DefaultMultiQueryIsolationLevel      == other.DefaultMultiQueryIsolationLevel
				&& AcceptsOuterExpressionInAggregate    == other.AcceptsOuterExpressionInAggregate
				&& OutputDeleteUseSpecialTable          == other.OutputDeleteUseSpecialTable
				&& OutputInsertUseSpecialTable          == other.OutputInsertUseSpecialTable
				&& OutputUpdateUseSpecialTables         == other.OutputUpdateUseSpecialTables
				// CustomFlags as List wasn't best idea
				&& CustomFlags.Count                    == other.CustomFlags.Count
				&& (CustomFlags.Count                   == 0
					|| CustomFlags.OrderBy(_            => _).SequenceEqual(other.CustomFlags.OrderBy(_ => _)));
		}
		#endregion
	}
}
