﻿namespace LinqToDB.SqlQuery
{
	public class SqlDropTableStatement : SqlStatement
	{
		public SqlDropTableStatement(SqlTable table)
		{
			Table = table;
		}

		public SqlTable Table { get; private set; }

		public override QueryType        QueryType    => QueryType.DropTable;
		public override QueryElementType ElementType  => QueryElementType.DropTableStatement;
		public override bool             IsParameterDependent { get => false; set {} }
		public override SelectQuery?     SelectQuery          { get => null;  set {} }

		public void Modify(SqlTable table)
		{
			Table = table;
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("DROP TABLE ")
				.AppendElement(Table)
				.AppendLine();

			return writer;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			noAlias = false;
			return null;
		}
	}
}
