using System;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlTruncateTableStatement : SqlStatement
	{
		public SqlTable? Table         { get; set; }
		public bool      ResetIdentity { get; set; }

		public override QueryType          QueryType    => QueryType.TruncateTable;
		public override QueryElementType   ElementType  => QueryElementType.TruncateTableStatement;

		public override bool               IsParameterDependent
		{
			get => false;
			set {}
		}

		public override SelectQuery? SelectQuery { get => null; set {}}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("TRUNCATE TABLE ")
				.AppendElement(Table)
				.AppendLine();

			return writer;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			noAlias = false;
			return null;
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(base.GetElementHashCode());

			hash.Add(Table?.GetElementHashCode());
			hash.Add(ResetIdentity);
			return hash.ToHashCode();
		}
	}
}
