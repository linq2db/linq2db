using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq;
	using Linq.Builder;

	public class SqlErrorExpression : Expression
	{
		public SqlErrorExpression(object? buildContext, Expression expression) : this(buildContext, expression, expression.Type)
		{}

		public SqlErrorExpression(object? buildContext, Expression expression, Type resultType)
		{
			BuildContext = buildContext;
			Expression   = expression;
			ResultType   = resultType;
		}

		public SqlErrorExpression(string message, Type resultType)
		{
			Message    = message;
			ResultType = resultType;
		}

		public object? BuildContext { get; }
		public Expression?    Expression   { get; }
		public Type           ResultType   { get; }
		public string?        Message      { get; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => ResultType;
		public override bool           CanReduce => true;

		public override Expression Reduce()
		{
			throw CreateError(Expression);
		}

		public SqlErrorExpression WithType(Type type)
		{
			if (ResultType == type)
				return this;
			return new SqlErrorExpression(BuildContext, Expression, type);
		}

		public Exception CreateError()
		{
			if (Expression == null)
				return CreateError(Message ?? "Unknown error.");

			return CreateError(Expression);
		}

		public static SqlErrorExpression EnsureError(object? context, Expression expression)
		{
			if (expression is SqlErrorExpression error)
				return error.WithType(expression.Type);
			return new SqlErrorExpression(context, expression);
		}

		public static SqlErrorExpression EnsureError(Expression expression, Type resultType)
		{
			if (expression is SqlErrorExpression error)
				return error.WithType(resultType);
			return new SqlErrorExpression(null, expression, resultType);
		}

		public static Exception CreateError(string message)
		{
			return new LinqException(message);
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

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlErrorExpression(this);
			return base.Accept(visitor);
		}

	}
}
