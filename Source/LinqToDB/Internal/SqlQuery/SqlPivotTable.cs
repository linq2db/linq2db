using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// PIVOT table-source operator: rotates distinct values of the <c>FOR</c> column(s) into columns,
	/// aggregating a value per (grouping, pivoted-value) cell. Emitted natively on providers that support
	/// it; otherwise lowered to <c>GROUP BY</c> + conditional aggregation (<c>SUM(CASE WHEN …)</c>) by the
	/// SQL optimizer before it reaches the SQL builder.
	/// </summary>
	public sealed class SqlPivotTable : SqlPivotTableBase
	{
		public SqlPivotTable(ISqlTableSource source) : base(source)
		{
		}

		internal SqlPivotTable(int id, ISqlTableSource source) : base(id, source)
		{
		}

		/// <summary>Passthrough / GROUP BY key columns (a subset of <see cref="SqlPivotTableBase.OutputFields"/>).</summary>
		public List<SqlField> KeyFields { get; } = new();

		/// <summary>The aggregate specifications; more than one models a multi-aggregate pivot.</summary>
		public List<SqlPivotAggregate> Aggregates { get; } = new();

		public override SqlTableType     SqlTableType => SqlTableType.Pivot;
		public override QueryElementType ElementType  => QueryElementType.SqlPivotTable;

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlPivotTable(this);

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendElement(PivotSource)
				.Append(" PIVOT (");

			for (var i = 0; i < Aggregates.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");

				var agg = Aggregates[i];
				writer
					.Append(agg.AggregationName)
					.Append('(')
					.AppendElement(agg.Value)
					.Append(')');
			}

			writer.Append(" FOR ");

			var forColumns = Aggregates.Count > 0 ? Aggregates[0].ForColumns : new List<ISqlExpression>();
			for (var i = 0; i < forColumns.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");
				writer.AppendElement(forColumns[i]);
			}

			writer.Append(" IN (…))");

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(PivotSource.GetElementHashCode());

			foreach (var field in KeyFields)
				hash.Add(field.GetElementHashCode());

			foreach (var agg in Aggregates)
			{
				hash.Add(agg.AggregationName);
				hash.Add(agg.Value.GetElementHashCode());

				foreach (var forColumn in agg.ForColumns)
					hash.Add(forColumn.GetElementHashCode());

				foreach (var value in agg.Values)
				{
					foreach (var forValue in value.ForValues)
						hash.Add(forValue.GetElementHashCode());
					hash.Add(value.OutputField.GetElementHashCode());
				}
			}

			return hash.ToHashCode();
		}
	}
}
