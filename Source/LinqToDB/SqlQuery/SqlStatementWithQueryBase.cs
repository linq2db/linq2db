using System;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlStatementWithQueryBase : SqlStatement
	{
		public override bool          IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}

		private         SelectQuery? _selectQuery;
		[NotNull]
		public override SelectQuery?  SelectQuery
		{
			get => _selectQuery ??= new SelectQuery();
			set => _selectQuery = value;
		}

		public SqlWithClause? With { get; set; }

		protected SqlStatementWithQueryBase(SelectQuery? selectQuery)
		{
			_selectQuery = selectQuery;
		}

		public override ISqlTableSource? GetTableSource(ISqlTableSource table, out bool noAlias)
		{
			var ts = SelectQuery!.GetTableSource(table) ?? With?.GetTableSource(table);
			noAlias = false;
			return ts;
		}
	}
}
