namespace LinqToDB.SqlQuery
{
	public class SqlInsertOrUpdateStatement: SqlStatementWithQueryBase
	{
		public override QueryType QueryType          => QueryType.InsertOrUpdate;
		public override QueryElementType ElementType => QueryElementType.InsertOrUpdateStatement;

		private SqlInsertClause? _insert;
		public  SqlInsertClause   Insert
		{
			get => _insert ??= new SqlInsertClause();
			set => _insert = value;
		}

		private SqlUpdateClause? _update;
		public  SqlUpdateClause   Update
		{
			get => _update ??= new SqlUpdateClause();
			set => _update = value;
		}

		internal bool HasInsert => _insert != null;
		internal bool HasUpdate => _update != null;

		public SqlInsertOrUpdateStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendLine("/* insert or update */")
				.AppendElement(Insert)
				.AppendElement(Update);
			return writer;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			if (Equals(_update?.Table, table))
			{
				noAlias = true;
				return table;
			}

			noAlias = false;
			if (Equals(_insert?.Into, table))
				return table;

			return SelectQuery!.GetTableSource(table);
		}
	}
}
