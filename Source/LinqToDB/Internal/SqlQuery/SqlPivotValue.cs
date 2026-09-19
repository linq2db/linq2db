using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// One generated PIVOT output column: the constant value(s) matched against the <c>FOR</c> column(s)
	/// and the output field that carries the aggregated cell.
	/// </summary>
	public sealed class SqlPivotValue
	{
		public SqlPivotValue(IEnumerable<ISqlExpression> forValues, SqlField outputField)
		{
			ForValues   = [..forValues];
			OutputField = outputField;
		}

		/// <summary>Constant value(s) compared against <see cref="SqlPivotAggregate.ForColumns"/> (count matches).</summary>
		public List<ISqlExpression> ForValues { get; }

		/// <summary>The output field produced for this value (a member of <see cref="SqlPivotTableBase.OutputFields"/>).</summary>
		public SqlField OutputField { get; internal set; }
	}
}
