using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq;
	using Linq.Builder;

	class SqlErrorExpression : Expression
	{
		public SqlErrorExpression(IBuildContext? buildContext, Expression expression)
		{
			BuildContext = buildContext;
			Expression   = expression;
		}

		public IBuildContext? BuildContext { get; }
		public Expression     Expression   { get; }

		internal SqlInfo? Sql { get; set; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => Expression.Type;
		public override bool           CanReduce => true;

		public override Expression Reduce()
		{
			throw CreateError(Expression);
		}

		public Exception CreateError()
		{
			return CreateError(Expression);
		}

		public static Exception CreateError(Expression expression)
		{
			return new LinqException($"'{PrepareExpression(expression)}' cannot be converted to SQL.");
		}

		public static void ThrowError(Expression expression)
		{
			throw CreateError(expression);
		}

		public static Expression PrepareExpression(Expression expression)
		{
			var transformed = expression.Transform(e =>
			{
				if (e is ContextRefExpression contextRef)
				{
					return Parameter(e.Type, contextRef.Alias ?? "x");
				}

				return e;
			});

			return transformed;
		}

		public override string ToString()
		{
			return $"Error: {Expression}";
		}
	}
}
