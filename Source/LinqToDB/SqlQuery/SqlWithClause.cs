using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlWithClause : IQueryElement, ISqlExpressionWalkable
	{
		public QueryElementType ElementType => QueryElementType.WithClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(";WITH ");
		}

		public List<SqlCteClause> Clauses { get; set; } = new List<SqlCteClause>();

		public ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			foreach (var cte in Clauses)
			{
				var ts = cte.Body.GetTableSource(table);
				if (ts == null)
					ts = cte == table ? cte : null;
				if (ts != null)
					return ts;
			}

			return null;
		}

		public ISqlExpression Walk(bool skipColumns, Func<ISqlExpression, ISqlExpression> func)
		{
			for (var index = 0; index < Clauses.Count; index++)
			{
				Clauses[index] = (SqlCteClause)Clauses[index].Walk(skipColumns, func);
			}

			return null;
		}
	}
}
