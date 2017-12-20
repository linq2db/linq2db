using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlSelectStatement : SqlStatementWithQueryBase
	{
		public SqlSelectStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public SqlSelectStatement() : base(null)
		{
		}

		public override QueryType          QueryType  => QueryType.Select;

		public override QueryElementType   ElementType => QueryElementType.SelectStatement;

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return SelectQuery.ToString(sb, dic);
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(skipColumns, func);
			var newQuery = SelectQuery.Walk(skipColumns, func);
			if (!ReferenceEquals(newQuery, SelectQuery))
				SelectQuery = (SelectQuery)newQuery;
			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var clone = new SqlSelectStatement();
			
			if (SelectQuery != null)
				clone.SelectQuery = (SelectQuery)SelectQuery.Clone(objectTree, doClone);

			clone.Parameters.AddRange(Parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));

			objectTree.Add(this, clone);
			
			return clone;
		}

		public override ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			var ts = SelectQuery.GetTableSource(table);
			if (ts == null)
				ts = With?.GetTableSource(table);
			return ts;
		}

		public SqlWithClause With { get; set; }


	}
}
