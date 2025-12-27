using System.Linq.Expressions;
using System.Net;

namespace LinqToDB.Internal.Expressions.ExpressionVisitors
{

	abstract class LegacyVisitorBase : ExpressionVisitorBase
	{
		public override Expression VisitSqlValidateExpression(SqlValidateExpression node)
		{
			return node.Update(Visit(node.InnerExpression));
		}

		internal override Expression VisitSqlEagerLoadExpression(SqlEagerLoadExpression node)
		{
			return node;
		}

		internal override Expression VisitSqlErrorExpression(SqlErrorExpression node)
		{
			return Visit(node.Reduce());
		}

		public override Expression VisitDefaultValueExpression(DefaultValueExpression node)
		{
			return Visit(node.Reduce());
		}

		internal override Expression VisitSqlGenericParamAccessExpression(SqlGenericParamAccessExpression node)
		{
			return node.Update(Visit(node.Constructor));
		}

		internal override Expression VisitConvertFromDataReaderExpression(ConvertFromDataReaderExpression node)
		{
			return Visit(node.Reduce());
		}

		public override Expression VisitConstantPlaceholder(ConstantPlaceholderExpression node)
		{
			return Visit(node.Reduce());
		}

		public override Expression VisitMarkerExpression(MarkerExpression node)
		{
			return Visit(node.Reduce());
		}

		internal override Expression VisitSqlAdjustTypeExpression(SqlAdjustTypeExpression node)
		{
			return node.Update(Visit(node.Expression));
		}
	}
}
