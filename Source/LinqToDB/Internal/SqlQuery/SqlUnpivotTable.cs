using System;
using System.Collections.Generic;
using System.Diagnostics;

using LinqToDB.Internal.SqlQuery.Visitors;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// UNPIVOT table-source operator: rotates a fixed set of source columns into rows,
	/// producing one output row per (source-row, column-group). Emitted natively on providers
	/// that support it; otherwise lowered to <c>UNION ALL</c> / <c>CROSS APPLY (VALUES …)</c>
	/// by the SQL optimizer before it reaches the SQL builder.
	/// </summary>
	public sealed class SqlUnpivotTable : SqlPivotTableBase
	{
		public SqlUnpivotTable(ISqlTableSource source, bool includeNulls) : base(source)
		{
			IncludeNulls = includeNulls;
		}

		internal SqlUnpivotTable(int id, ISqlTableSource source, bool includeNulls) : base(id, source)
		{
			IncludeNulls = includeNulls;
		}

		/// <summary>When <see langword="false"/> (default) rows whose value cell is NULL are excluded (ANSI/native default).</summary>
		public bool IncludeNulls { get; }

		/// <summary>Output column carrying the source column's name (the <c>FOR</c> column).</summary>
		public SqlField NameField { get; internal set; } = null!;

		/// <summary>Output value column(s) — one for a classic unpivot, several for a multi-value unpivot.</summary>
		public List<SqlField> ValueFields { get; } = new();

		/// <summary>The unpivoted column groups; each carries a name label and one source column per value field.</summary>
		public List<SqlUnpivotItem> Items { get; } = new();

		public override SqlTableType     SqlTableType => SqlTableType.Unpivot;
		public override QueryElementType ElementType  => QueryElementType.SqlUnpivotTable;

		[DebuggerStepThrough]
		public override IQueryElement Accept(QueryElementVisitor visitor) => visitor.VisitSqlUnpivotTable(this);

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendElement(PivotSource)
				.Append(" UNPIVOT ");

			if (IncludeNulls)
				writer.Append("INCLUDE NULLS ");

			writer.Append('(');

			for (var i = 0; i < ValueFields.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");
				writer.AppendElement(ValueFields[i]);
			}

			writer
				.Append(" FOR ")
				.AppendElement(NameField)
				.Append(" IN (");

			for (var i = 0; i < Items.Count; i++)
			{
				if (i > 0)
					writer.Append(", ");
				writer.Append(Items[i].Label);
			}

			writer.Append("))");

			return writer;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();

			hash.Add(ElementType);
			hash.Add(IncludeNulls);
			hash.Add(PivotSource.GetElementHashCode());

			foreach (var field in ValueFields)
				hash.Add(field.GetElementHashCode());

			foreach (var item in Items)
			{
				hash.Add(item.Label);
				foreach (var column in item.Columns)
					hash.Add(column.GetElementHashCode());
			}

			return hash.ToHashCode();
		}
	}
}
