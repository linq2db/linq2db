using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{
	abstract class TransformVisitorsBase : LegacyVisitorBase
	{
		public override Expression VisitTagExpression(TagExpression node)
		{
			return node;
		}

		internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			return node;
		}

		public override Expression VisitDefaultValueExpression(DefaultValueExpression node)
		{
			return node;
		}

		internal override Expression VisitConvertFromDataReaderExpression(ConvertFromDataReaderExpression node)
		{
			return node;
		}

		public override Expression VisitConstantPlaceholder(ConstantPlaceholderExpression node)
		{
			return node;
		}
	}
}
