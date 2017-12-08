using System.Collections.Generic;

namespace LinqToDB.SqlQuery
{
	public abstract class SqlStatementWithQueryBase : SqlStatement
	{
		public override List<SqlParameter> Parameters => SelectQuery.Parameters;

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
		
		public SqlStatementWithQueryBase(SelectQuery selectQuery)
		{
			_selectQuery = selectQuery;
		}
		
	}
}
