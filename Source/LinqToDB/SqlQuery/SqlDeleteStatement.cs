using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	using Mapping;

	public class SqlDeleteStatement : SqlStatement
	{
		public SqlDeleteStatement(SelectQuery selectQuery)
		{
			_selectQuery = selectQuery;
		}

		public SqlDeleteStatement()
		{
		}

		public override QueryType        QueryType   => QueryType.Delete;
		public override QueryElementType ElementType => QueryElementType.DeleteStatement;

		public override bool               IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}
		
		public override List<SqlParameter> Parameters  => SelectQuery.Parameters;

		public SqlTable       Table { get; set; }
		public ISqlExpression Top   { get; set; }

		private SelectQuery        _selectQuery;
		public override SelectQuery SelectQuery
		{
			get => _selectQuery ?? (_selectQuery = new SelectQuery());
			set => _selectQuery = value;
		}

		public override SqlStatement ProcessParameters(MappingSchema mappingSchema)
		{
			var newQuery = SelectQuery.ProcessParameters(mappingSchema);
			if (!ReferenceEquals(newQuery, _selectQuery))
				return new SqlDeleteStatement(newQuery);
			return this;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlDeleteStatement();

			if (Table != null)
				clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

			objectTree.Add(this, clone);

			return clone;
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			return null;
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			((ISqlExpressionWalkable)Table)?.Walk(skipColumns, func);

			return null;
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("DELETE FROM ");

			((IQueryElement)Table)?.ToString(sb, dic);

			sb.AppendLine();

			return sb;
		}
	}
}
