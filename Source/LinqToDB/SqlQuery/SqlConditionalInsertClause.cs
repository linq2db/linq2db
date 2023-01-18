using System;

namespace LinqToDB.SqlQuery
{
	public class SqlConditionalInsertClause : IQueryElement, ISqlExpressionWalkable
	{
		public SqlInsertClause     Insert { get; }
		public SqlSearchCondition? When   { get; }

		public SqlConditionalInsertClause(SqlInsertClause insert, SqlSearchCondition? when)
		{
			Insert = insert;
			When   = when;
		}

		#region ISqlExpressionWalkable

		ISqlExpression? ISqlExpressionWalkable.Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			((ISqlExpressionWalkable?)When)?.Walk(options, context, func);

			((ISqlExpressionWalkable)Insert).Walk(options, context, func);

			return null;
		}

		#endregion

		#region IQueryElement

		QueryElementType IQueryElement.ElementType => QueryElementType.ConditionalInsertClause;

		QueryElementTextWriter IQueryElement.ToString(QueryElementTextWriter writer)
		{
			if (When != null)
			{
				writer
					.Append("WHEN ")
					.AppendElement(When)
					.AppendLine(" THEN");
			}

			writer.AppendElement(Insert);

			return writer;
		}

		#endregion
	}
}
