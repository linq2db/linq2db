using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// One aggregate of a PIVOT: an aggregation function over <see cref="Value"/>, spread across the
	/// output columns produced for each entry in <see cref="Values"/>.
	/// </summary>
	public sealed class SqlPivotAggregate
	{
		public SqlPivotAggregate(string aggregationName, ISqlExpression value, IEnumerable<ISqlExpression> forColumns)
		{
			AggregationName = aggregationName;
			Value           = value;
			ForColumns      = [..forColumns];
		}

		/// <summary>Aggregation function name, e.g. <c>SUM</c>, <c>MIN</c>, <c>MAX</c>, <c>COUNT</c>, <c>AVG</c>.</summary>
		public string AggregationName { get; }

		/// <summary>The value expression being aggregated (a source column).</summary>
		public ISqlExpression Value { get; internal set; }

		/// <summary>The <c>FOR</c> column(s) — one for a classic pivot, several for a composite (multi-column) pivot.</summary>
		public List<ISqlExpression> ForColumns { get; }

		/// <summary>One entry per generated output column: the constant <c>FOR</c> value(s) and the resulting field.</summary>
		public List<SqlPivotValue> Values { get; } = new();
	}
}
