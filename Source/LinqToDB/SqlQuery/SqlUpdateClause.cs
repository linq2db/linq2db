using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public class SqlUpdateClause : IQueryElement
	{
		public List<SqlSetExpression> Items         { get; set; } = new();
		public List<SqlSetExpression> Keys          { get; set; } = new();
		public SqlTable?              Table         { get; set; }
		public SqlTableSource?        TableSource   { get; set; }
		public bool                   HasComparison { get; set; }

		public void Modify(SqlTable? table, SqlTableSource? tableSource)
		{
			Table       = table;
			TableSource = tableSource;
		}

		#region Overrides

#if OVERRIDETOSTRING

		public override string ToString()
		{
			return this.ToDebugString();
		}

#endif

		#endregion

		#region IQueryElement Members

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.UpdateClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			writer
				.Append('\t');

			if (Table != null)
				writer.AppendElement(Table);
			if (TableSource != null)
				writer.AppendElement(TableSource);

			writer.AppendLine()
				.Append("SET ")
				.AppendLine();

			using (writer.IndentScope())
				foreach (var e in Items)
				{
					writer
						.AppendElement(e)
						.AppendLine();
				}

			return writer;
		}

		#endregion
	}
}
