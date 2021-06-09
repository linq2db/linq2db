using System;
using System.Collections.Generic;
using System.Text;

namespace LinqToDB.SqlQuery
{
	public class SqlDeleteStatement : SqlStatementWithQueryBase
	{
		public SqlDeleteStatement(SelectQuery? selectQuery) : base(selectQuery)
		{
		}

		public SqlDeleteStatement() : this(null)
		{
		}

		public override QueryType        QueryType   => QueryType.Delete;
		public override QueryElementType ElementType => QueryElementType.DeleteStatement;

		public override bool             IsParameterDependent
		{
			get => SelectQuery.IsParameterDependent;
			set => SelectQuery.IsParameterDependent = value;
		}

		public SqlTable?       Table   { get; set; }
		public ISqlExpression? Top     { get; set; }

		public SqlOutputClause? Output { get; set; }

		public override ISqlExpression? Walk(WalkOptions options, Func<ISqlExpression,ISqlExpression> func)
		{
			With?.Walk(options, func);

			Table       = ((ISqlExpressionWalkable?)Table)?.Walk(options, func) as SqlTable;
			SelectQuery = (SelectQuery)SelectQuery.Walk(options, func);

			return null;
		}

		public override StringBuilder ToString(StringBuilder sb, Dictionary<IQueryElement,IQueryElement> dic)
		{
			sb.Append("DELETE FROM ");

			((IQueryElement?)Table)?.ToString(sb, dic);

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
