using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlWithClause : IQueryElement, ISqlExpressionWalkable, ICloneableElement
	{
		public QueryElementType ElementType => QueryElementType.WithClause;

		public StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement, IQueryElement> dic)
		{
			return sb.Append(";WITH ");
		}

		public List<CteClause> Clauses { get; set; } = new List<CteClause>();

		public ISqlTableSource GetTableSource(ISqlTableSource table)
		{
			foreach (var cte in Clauses)
			{
				var ts = cte.Body.GetTableSource(table);
				if (ts != null)
					return ts;
			}

			return null;
		}

		public void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			foreach (var c in Clauses)
			{
				if (c.Body != null)
					c.Body = func(c.Body);
			}
		}

		public ISqlExpression Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			for (var index = 0; index < Clauses.Count; index++)
			{
				Clauses[index].Walk(options, func);
			}

			return null;
		}

		public ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlWithClause();

			clone.Clauses.AddRange(Clauses.Select(c => (CteClause)c.Clone(objectTree, doClone)));

			objectTree.Add(this, clone);

			return clone;
		}

	}
}
