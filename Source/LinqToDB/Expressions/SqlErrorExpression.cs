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
			throw CreateError();
		}

		public Exception CreateError()
		{
			return new LinqException($"'{PrepareExpression(Expression)}' cannot be converted to SQL.");
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
