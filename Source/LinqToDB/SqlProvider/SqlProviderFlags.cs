using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Runtime.Serialization;

namespace LinqToDB.SqlProvider;

using SqlQuery;

[DataContract]
public sealed class SqlProviderFlags
{
	[DataMember(Order =  1)]
	public bool        IsSybaseBuggyGroupBy           { get; set; }

	[DataMember(Order =  2)]
	public bool        IsParameterOrderDependent      { get; set; }

	[DataMember(Order =  3)]
	public bool        AcceptsTakeAsParameter         { get; set; }
	[DataMember(Order =  4)]
	public bool        AcceptsTakeAsParameterIfSkip   { get; set; }
	[DataMember(Order =  5)]
	public bool        IsTakeSupported                { get; set; }
	[DataMember(Order =  6)]
	public bool        IsSkipSupported                { get; set; }
	[DataMember(Order =  7)]
	public bool        IsSkipSupportedIfTake          { get; set; }
	[DataMember(Order =  8)]
	public TakeHints?  TakeHintsSupported              { get; set; }
	[DataMember(Order =  9)]
	public bool        IsSubQueryTakeSupported        { get; set; }

	[DataMember(Order = 10)]
	public bool        IsSubQueryColumnSupported      { get; set; }
	[DataMember(Order = 11)]
	public bool        IsSubQueryOrderBySupported     { get; set; }
	[DataMember(Order = 12)]
	public bool        IsCountSubQuerySupported       { get; set; }

	[DataMember(Order = 13)]
	public bool        IsIdentityParameterRequired    { get; set; }
	[DataMember(Order = 14)]
	public bool        IsApplyJoinSupported           { get; set; }
	[DataMember(Order = 15)]
	public bool        IsInsertOrUpdateSupported      { get; set; }
	[DataMember(Order = 16)]
	public bool        CanCombineParameters           { get; set; }
	[DataMember(Order = 17)]
	public int         MaxInListValuesCount           { get; set; }
	[DataMember(Order = 18)]
	public bool        IsUpdateSetTableAliasSupported { get; set; }

	/// <summary>
	/// If <c>true</c>, removed record fields in OUTPUT clause of DELETE statement should be referenced using
	/// table with special name (e.g. DELETED or OLD). Otherwise fields should be referenced using target table.
	/// </summary>
	[DataMember(Order = 19)]
	public bool        OutputDeleteUseSpecialTable       { get; set; }
	/// <summary>
	/// If <c>true</c>, added record fields in OUTPUT clause of INSERT statement should be referenced using
	/// table with special name (e.g. INSERTED or NEW). Otherwise fields should be referenced using target table.
	/// </summary>
	[DataMember(Order = 20)]
	public bool        OutputInsertUseSpecialTable       { get; set; }
	/// <summary>
	/// If <c>true</c>, OUTPUT clause supports both OLD and NEW data in UPDATE statement using tables with special names.
	/// Otherwise only current record fields (after update) available using target table.
	/// </summary>
	[DataMember(Order = 21)]
	public bool        OutputUpdateUseSpecialTables      { get; set; }

	/// <summary>
	/// Provider requires that selected subquery column must be used in group by even for constant column.
	/// </summary>
	[DataMember(Order = 22)]
	public bool        IsGroupByColumnRequred         { get; set; }

	/// <summary>
	/// Provider supports:
	/// CROSS JOIN a Supported
	/// </summary>
	[DataMember(Order = 23)]
	public bool IsCrossJoinSupported                  { get; set; }

	/// <summary>
	/// Provider supports:
	/// INNER JOIN a ON 1 = 1
	/// </summary>
	[DataMember(Order = 24)]
	public bool IsInnerJoinAsCrossSupported           { get; set; }

	/// <summary>
	/// Provider supports CTE expressions.
	/// If provider does not support CTE, unsuported exception will be thrown when using CTE.
	/// </summary>
	[DataMember(Order = 25)]
	public bool IsCommonTableExpressionsSupported     { get; set; }

	/// <summary>
	/// Provider supports DISTINCT and ORDER BY with fields that are not in projection.
	/// </summary>
	[DataMember(Order = 26)]
	public bool IsDistinctOrderBySupported            { get; set; }

	/// <summary>
	/// Provider supports aggregate functions in ORDER BY statement.
	/// </summary>
	[DataMember(Order = 27)]
	public bool IsOrderByAggregateFunctionsSupported  { get; set; }

	/// <summary>
	/// Provider supports EXCEPT ALL, INTERSECT ALL set operators. Otherwise it will be emulated.
	/// </summary>
	[DataMember(Order = 28)]
	public bool IsAllSetOperationsSupported           { get; set; }

	/// <summary>
	/// Provider supports EXCEPT, INTERSECT set operators. Otherwise it will be emulated.
	/// </summary>
	[DataMember(Order = 29)]
	public bool IsDistinctSetOperationsSupported      { get; set; }

	/// <summary>
	/// Provider supports COUNT(DISTINCT column) function. Otherwise it will be emulated.
	/// </summary>
	[DataMember(Order = 30)]
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
	[DataMember(Order = 31)]
	public bool AcceptsOuterExpressionInAggregate { get; set; }

	/// <summary>
	/// Provider supports
	/// <code>
	/// UPDATE A
	/// SET ...
	/// FROM B
	/// </code> syntax
	/// </summary>
	[DataMember(Order = 32)]
	public bool IsUpdateFromSupported             { get; set; }

	/// <summary>
	/// Provider supports Naming Query Blocks
	/// <code>
	/// QB_NAME(qb)
	/// </code>
	/// </summary>
	[DataMember(Order = 33)]
	public bool IsNamingQueryBlockSupported       { get; set; }

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
	[DataMember(Order = 34)]
	public IsolationLevel DefaultMultiQueryIsolationLevel { get; set; } = IsolationLevel.RepeatableRead;

	/// <summary>
	/// Provider support Row Constructor `(1, 2, 3)` in various positions (flags)
	/// </summary>
	[DataMember(Order = 35)]
	public RowFeature RowConstructorSupport { get; set; }

	/// <summary>
	/// Flags for use by external providers.
	/// </summary>
	[DataMember(Order = 36)]
	public List<string> CustomFlags { get; set; } = new List<string>();

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
			^ IsNamingQueryBlockSupported                  .GetHashCode()
			^ RowConstructorSupport                        .GetHashCode()
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
			&& IsNamingQueryBlockSupported          == other.IsNamingQueryBlockSupported
			&& RowConstructorSupport                == other.RowConstructorSupport
			&& OutputDeleteUseSpecialTable          == other.OutputDeleteUseSpecialTable
			&& OutputInsertUseSpecialTable          == other.OutputInsertUseSpecialTable
			&& OutputUpdateUseSpecialTables         == other.OutputUpdateUseSpecialTables
			// CustomFlags as List wasn't best idea
			&& CustomFlags.Count                    == other.CustomFlags.Count
			&& (CustomFlags.Count                   == 0
				|| CustomFlags.OrderBy(_ => _).SequenceEqual(other.CustomFlags.OrderBy(_ => _)));
	}
	#endregion
}
