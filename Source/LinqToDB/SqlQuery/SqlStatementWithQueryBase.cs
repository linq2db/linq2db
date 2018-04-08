using System;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlStatementWithQueryBase : SqlStatement
	{
		public override bool               IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}

		private SelectQuery        _selectQuery;
		public override SelectQuery SelectQuery
		{
			get => _selectQuery ?? (_selectQuery = new SelectQuery());
			set => _selectQuery = value;
		}

		public SqlWithClause With { get; set; }

		public SqlStatementWithQueryBase(SelectQuery selectQuery)
		{
			_selectQuery = selectQuery;
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			var ts = SelectQuery.GetTableSource(table);
			if (ts == null)
				ts = With?.GetTableSource(table);
			return ts;
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			if (SelectQuery != null)
			{
				var newQuery = func(SelectQuery);
				if (!ReferenceEquals(newQuery, SelectQuery))
					SelectQuery = newQuery;
			}

			With?.WalkQueries(func);
		}
	}
}
