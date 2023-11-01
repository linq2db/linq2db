using System;

namespace LinqToDB.SqlQuery
{
	public class SqlCreateTableStatement : SqlStatement
	{
		public SqlCreateTableStatement(SqlTable sqlTable)
		{
			Table = sqlTable;
		}

		public SqlTable        Table           { get; private set; }
		public string?         StatementHeader { get; set; }
		public string?         StatementFooter { get; set; }
		public DefaultNullable DefaultNullable { get; set; }

		public override QueryType        QueryType   => QueryType.CreateTable;
		public override QueryElementType ElementType => QueryElementType.CreateTableStatement;

		public override bool             IsParameterDependent
		{
			get => false;
			set {}
		}

		public void Modify(SqlTable table)
		{
			Table = table;
		}

		public override SelectQuery? SelectQuery { get => null; set {}}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.Append("CREATE TABLE ")
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
