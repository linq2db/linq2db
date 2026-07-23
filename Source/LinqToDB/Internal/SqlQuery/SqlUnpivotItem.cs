using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	/// <summary>
	/// One unpivoted column group: the name-column label and the source columns that feed the value field(s).
	/// </summary>
	public sealed class SqlUnpivotItem
	{
		public SqlUnpivotItem(string label, IEnumerable<ISqlExpression> columns)
		{
			Label   = label;
			Columns = [..columns];
		}

		/// <summary>The literal emitted in the name column for this group (the source column's mapped name, or an alias).</summary>
		public string Label { get; }

		/// <summary>Source columns for this group; count matches <see cref="SqlUnpivotTable.ValueFields"/>.</summary>
		public List<ISqlExpression> Columns { get; }
	}
}
