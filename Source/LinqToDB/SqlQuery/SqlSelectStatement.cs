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

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, func);
			var newQuery = SelectQuery.Walk(options, func);
			if (!ReferenceEquals(newQuery, SelectQuery))
				SelectQuery = (SelectQuery)newQuery;
			return null;
		}

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			var clone = new SqlSelectStatement();

			if (SelectQuery != null)
				clone.SelectQuery = (SelectQuery)SelectQuery.Clone(objectTree, doClone);

			if (With != null)
				clone.With = (SqlWithClause)With.Clone(objectTree, doClone);

			clone.Parameters.AddRange(Parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));

			objectTree.Add(this, clone);

			return clone;
		}
	}
}
