using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	abstract class FindVisitorBase : ExpressionVisitorBase
	{
		public override Expression VisitSqlDefaultIfEmptyExpression(SqlDefaultIfEmptyExpression node)
		{
			Visit(node.InnerExpression);

			return node;
		}

		internal override Expression VisitSqlReaderIsNullExpression(SqlReaderIsNullExpression node)
		{
			return node;
		}

		internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			Visit(node.Expression);

			return node;
		}

		internal override Expression VisitSqlPathExpression(SqlPathExpression node)
		{
			Visit(node.Path.AsReadOnly());

			return node;
		}
	}
}
