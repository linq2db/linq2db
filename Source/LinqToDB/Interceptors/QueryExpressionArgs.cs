using System.Linq.Expressions;

namespace LinqToDB.Interceptors
{
	public class QueryExpressionArgs(IDataContext dataContext, Expression expression, QueryExpressionArgs.ExpressionKind kind)
	{
		public enum ExpressionKind
		{
			Query,
			ExposedQuery,
			AssociationExpression,
			QueryFilter
		}

		public IDataContext   DataContext { get; } = dataContext;
		public Expression     Expression  { get; } = expression;
		public ExpressionKind Kind        { get; } = kind;
	}
}
