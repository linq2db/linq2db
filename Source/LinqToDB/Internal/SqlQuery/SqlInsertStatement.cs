using System;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlInsertStatement : SqlStatementWithQueryBase
	{

		public SqlInsertStatement() : base(null)
		{
		}

		public SqlInsertStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public override QueryType          QueryType   => QueryType.Insert;
		public override QueryElementType   ElementType => QueryElementType.InsertStatement;

		#region InsertClause

		private SqlInsertClause? _insert;
		public  SqlInsertClause   Insert
		{
			get => _insert ??= new SqlInsertClause();
			set => _insert = value;
		}

		internal bool HasInsert => _insert != null;

		#endregion

		#region Output

		public  SqlOutputClause?  Output { get; set; }

		#endregion

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer
				.AppendElement(_insert)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendElement(Output);
		}

		public override int GetElementHashCode()
		{
			var hash = new HashCode();
			hash.Add(base.GetElementHashCode());

			hash.Add(_insert?.GetElementHashCode());
			hash.Add(Output?.GetElementHashCode());
			return hash.ToHashCode();
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			noAlias = false;

			if (ReferenceEquals(_insert?.Into, table))
				return table;

			return SelectQuery!.GetTableSource(table);
		}
	}
}
