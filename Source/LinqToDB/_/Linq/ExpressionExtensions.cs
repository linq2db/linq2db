namespace System.Linq.Expressions
{
	public static class ExpressionExtensions
	{
		public static Expression<Func<T, bool>> AndAlso<T>(this Expression<Func<T, bool>> expr1, Expression<Func<T, bool>> expr2)
		{
			if (expr1.Equals(expr2)) return expr1;
			if (expr1 == null || expr1.Equals(True<T>())) return expr2;
			if (expr2 == null || expr2.Equals(True<T>())) return expr1;
			if (new[] { expr1, expr2 }.Contains(False<T>())) return False<T>();
			ParameterExpression parameter = Expression.Parameter(typeof(T));
			Expression left = new ReplaceExpressionVisitor(expr1.Parameters[0], parameter).Visit(expr1.Body) ?? expr1;
			Expression right = new ReplaceExpressionVisitor(expr2.Parameters[0], parameter).Visit(expr2.Body) ?? expr2;
			return Expression.Lambda<Func<T, bool>>(Expression.AndAlso(left, right), parameter);
		}

		public static Expression<Func<T, bool>> Equal<T, TProp>(this Expression<Func<T, TProp>> field, TProp value) => Expression.Lambda<Func<T, bool>>(Expression.Equal(field.Body, Expression.Constant(value, typeof(TProp))), field.Parameters);
		public static Expression<Func<T, bool>> False<T>() => x => false;
		public static Expression? ReplaceExpressions<T>(this Expression<Func<T, bool>> expr)
		{
			ParameterExpression parameter = Expression.Parameter(typeof(T));
			return new ReplaceExpressionVisitor(expr.Parameters[0], parameter).Visit(expr.Body);
		}
		public static Expression<Func<T, bool>> True<T>() => x => true;

	}

	public class ReplaceExpressionVisitor : ExpressionVisitor
	{
		public ReplaceExpressionVisitor(Expression source, Expression target)
		{
			this.source = source;
			this.target = target;
		}

		private readonly Expression source;
		private readonly Expression target;

		public override Expression? Visit(Expression? node)
		{
			if (node != null && node == source) return target;
			return base.Visit(node);
		}

	}

}
