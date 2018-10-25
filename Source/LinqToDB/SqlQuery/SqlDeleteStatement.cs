using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlDeleteStatement : SqlStatementWithQueryBase
	{
		public SqlDeleteStatement(SelectQuery selectQuery) : base(selectQuery)
		{
		}

		public SqlDeleteStatement() : this(null)
		{
		}

		public override QueryType        QueryType   => QueryType.Delete;
		public override QueryElementType ElementType => QueryElementType.DeleteStatement;

		public override bool               IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}
		
		public SqlTable       Table { get; set; }
		public ISqlExpression Top   { get; set; }

		public override ICloneableElement Clone(Dictionary<ICloneableElement, ICloneableElement> objectTree, Predicate<ICloneableElement> doClone)
		{
			if (!doClone(this))
				return this;

			var clone = new SqlDeleteStatement();

			if (SelectQuery != null)
				clone.SelectQuery = (SelectQuery)SelectQuery.Clone(objectTree, doClone);

			if (Table != null)
				clone.Table = (SqlTable)Table.Clone(objectTree, doClone);

			clone.Parameters.AddRange(Parameters.Select(p => (SqlParameter)p.Clone(objectTree, doClone)));

			objectTree.Add(this, clone);

			return clone;
		}

		public override ISqlExpression Walk(bool skipColumns, Func<ISqlExpression,ISqlExpression> func)
		{
			With?.Walk(skipColumns, func);
			Table = ((ISqlExpressionWalkable)Table)?.Walk(skipColumns, func) as SqlTable;
			SelectQuery = (SelectQuery)SelectQuery.Walk(skipColumns, func);

			return null;
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("DELETE FROM ");

			((IQueryElement)Table)?.ToString(sb, dic);

			sb.AppendLine();

			return sb;
		}

		public override void WalkQueries(Func<SelectQuery, SelectQuery> func)
		{
			if (SelectQuery != null)
			{
				var newQuery = func(SelectQuery);
				if (!ReferenceEquals(newQuery, SelectQuery))
					SelectQuery = newQuery;
			}
		}

	}
}
