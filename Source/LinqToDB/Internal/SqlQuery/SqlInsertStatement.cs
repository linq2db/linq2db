using System;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class SqlInsertStatement : SqlStatementWithQueryBase
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
			get => _insert ??= new();
			set => _insert = value;
		}

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
			return HashCode.Combine(
				base.GetElementHashCode(),
				_insert?.GetElementHashCode(),
				Output?.GetElementHashCode()
			);
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
