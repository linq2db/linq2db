using System;

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

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			writer.AppendTag(Tag);

			if (With?.Clauses.Count > 0)
			{
				writer
					.AppendElement(With)
					.AppendLine("--------------------------");
			}

			return writer.AppendElement(SelectQuery);
		}

		public override ISqlExpression? Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			With?.Walk(options, context, func);

			var newQuery = SelectQuery.Walk(options, context, func);

			if (!ReferenceEquals(newQuery, SelectQuery))
				SelectQuery = (SelectQuery)newQuery;

			return base.Walk(options, context, func);
		}
	}
}
