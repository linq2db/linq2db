using System;

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

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, context, func);

			Table       = ((ISqlExpressionWalkable?)Table)?.Walk(options, context, func) as SqlTable;
			SelectQuery = (SelectQuery)SelectQuery.Walk(options, context, func);

			return base.Walk(options, context, func);
		}

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer
				.AppendTag(Tag)
				.AppendElement(With)
				.Append("DELETE FROM ")
				.AppendElement(Table)
				.AppendLine()
				.AppendElement(SelectQuery)
				.AppendLine();

			return writer;
		}

	}
}
