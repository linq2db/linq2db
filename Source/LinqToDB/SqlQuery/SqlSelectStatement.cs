using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlSelectStatement : SqlStatement
	{
		public SqlSelectStatement(SelectQuery selectQuery)
		{
			_selectQuery = selectQuery;
		}

		public SqlSelectStatement()
		{
		}

		public override QueryType          QueryType  => SelectQuery.QueryType;
		public override List<SqlParameter> Parameters => SelectQuery.Parameters;
		
		public override bool               IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}
		
		public override QueryElementType   ElementType => QueryElementType.SelectStatement;

		private SelectQuery        _selectQuery;
		public override SelectQuery SelectQuery
		{
			get => _selectQuery ?? (_selectQuery = new SelectQuery());
			set => _selectQuery = value;
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return SelectQuery.ToString(sb, dic);
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			var newQuery = SelectQuery.Walk(skipColumns, func);
			if (!ReferenceEquals(newQuery, SelectQuery))
				SelectQuery = (SelectQuery)newQuery;
			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var newStatement = new SqlSelectStatement();
			if (_selectQuery != null)
				newStatement.SelectQuery = (SelectQuery)SelectQuery.Clone(objectTree, doClone);
			return this;
		}

		public override SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			var newQuery = SelectQuery.ProcessParameters(mappingSchema);
			if (!ReferenceEquals(newQuery, _selectQuery))
				return new SqlSelectStatement(newQuery);
			return this;
		}

	}
}
