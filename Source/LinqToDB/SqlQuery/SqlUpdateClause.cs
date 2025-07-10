using System;
using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlUpdateClause : QueryElement
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

		#region IQueryElement Members

		public override QueryElementType ElementType => QueryElementType.UpdateClause;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
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

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			foreach (var item in Items)
				hash.Add(item?.GetElementHashCode());
			foreach (var key in Keys)
				hash.Add(key?.GetElementHashCode());
			hash.Add(Table?.GetElementHashCode());
			hash.Add(TableSource?.GetElementHashCode());
			hash.Add(HasComparison);
			return hash.ToHashCode();
		}

		#endregion
	}
}
