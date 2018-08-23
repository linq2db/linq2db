using System;

namespace LinqToDB.SqlProvider
{
	using SqlQuery;
	using System.Collections.Generic;

	public class SqlProviderFlags
	{
		public bool        IsParameterOrderDependent      { get; set; }
		public bool        AcceptsTakeAsParameter         { get; set; }
		public bool        AcceptsTakeAsParameterIfSkip   { get; set; }
		public bool        IsTakeSupported                { get; set; }
		public bool        IsSkipSupported                { get; set; }
		public bool        IsSkipSupportedIfTake          { get; set; }
		public bool        IsSubQueryTakeSupported        { get; set; }
		public bool        IsSubQueryColumnSupported      { get; set; }
		public bool        IsSubQueryOrderBySupported     { get; set; }
		public bool        IsCountSubQuerySupported       { get; set; }
		public bool        IsIdentityParameterRequired    { get; set; }
		public bool        IsApplyJoinSupported           { get; set; }
		public bool        IsInsertOrUpdateSupported      { get; set; }
		public bool        CanCombineParameters           { get; set; }
		public bool        IsGroupByExpressionSupported   { get; set; }
		public int         MaxInListValuesCount           { get; set; }
		public bool        IsUpdateSetTableAliasSupported { get; set; }
		public bool        IsSybaseBuggyGroupBy           { get; set; }
		//public IsTakeHints GetIsTakeHintsSupported        { get; set; }
		public TakeHints?  TakeHintsSupported             { get; set; }

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

		public bool GetIsSkipSupportedFlag(SelectQuery selectQuery)
		{
			return IsSkipSupported || IsSkipSupportedIfTake && selectQuery.Select.TakeValue != null;
		}

		public bool GetIsTakeHintsSupported(TakeHints hints)
		{
			if (TakeHintsSupported == null)
				return false;

			return (TakeHintsSupported.Value & hints) == hints;
		}

		/// <summary>
		/// Flags for use by external providers.
		/// </summary>
		public List<string> CustomFlags { get; } = new List<string>();
	}
}
