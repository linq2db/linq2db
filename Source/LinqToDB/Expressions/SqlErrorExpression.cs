using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	using Linq;
	using Common;

	public class SqlErrorExpression : Expression
	{
		public SqlErrorExpression(object? buildContext, Expression? expression, string? message, Type resultType)
		{
			BuildContext = buildContext;
			Expression   = expression;
			Message      = message;
			ResultType   = resultType;
		}

		public SqlErrorExpression(object? buildContext, Expression expression) : this(buildContext, expression, null, expression.Type)
		{
		}

		public SqlErrorExpression(string message, Type resultType) : this(null, null, message, resultType)
		{
		}

		public SqlErrorExpression(Expression? expression, string? message, Type resultType) : this(null, expression, message, resultType)
		{
		}

		public object?        BuildContext { get; }
		public Expression?    Expression   { get; }
		public Type           ResultType   { get; }
		public string?        Message      { get; }

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => ResultType;
		public override bool           CanReduce => true;

		public override Expression Reduce()
		{
			throw CreateException(Expression, Message);
		}

		public SqlErrorExpression WithType(Type type)
		{
			if (ResultType == type)
				return this;
			return new SqlErrorExpression(BuildContext, Expression, Message, type);
		}

		public Exception CreateError()
		{
			return CreateException(Expression, Message);
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
			return new SqlErrorExpression(null, expression, null, resultType);
		}

		public static Exception CreateError(string message)
		{
			return new LinqException(message);
		}

		public static Exception CreateException(Expression? expression, string? message)
		{
			string messageText;

			if (expression != null)
			{
				var expressionMessage = PrepareExpressionString(expression);
				if (expressionMessage.Contains("\n"))
				{
					messageText = $"The LINQ expression could not be converted to SQL.\nExpression:\n{expressionMessage}";
				}
				else
				{
					messageText = $"The LINQ expression '{expressionMessage}' could not be converted to SQL.";
				}

				if (message != null)
				{
					messageText += $"\nAdditional details: '{message}'";
				}
			}
			else if (message != null)
			{
				messageText = $"Translation error: '{message}'";
			}
			else
			{
				messageText = "Unknown translation error.";
			}

			return new LinqException(messageText);
		}

		public static void ThrowError(Expression expression, string? message)
		{
			throw CreateException(expression, message);
		}

		public static string PrepareExpressionString(Expression? expression)
		{
			var printer  = new ExpressionPrinter();
			var prepared = PrepareExpression(expression);
			var str      = prepared == null ? "null" : printer.PrintExpression(prepared);
			return str;
		}

		[return: NotNullIfNotNull(nameof(expression))]
		static Expression? PrepareExpression(Expression? expression)
		{
			Dictionary<Expression, string> usedNames = new (ExpressionEqualityComparer.Instance);

			var transformed = expression.Transform(e =>
			{
				if (e is ContextRefExpression contextRef)
				{
					return Parameter(e.Type, contextRef.Alias ?? "x");
				}

				if (e is SqlQueryRootExpression)
				{
					if (!usedNames.TryGetValue(e, out var name))
					{
						Utils.MakeUniqueNames([e], usedNames.Values, _ => "db", (_, n, _) => name = n);
						usedNames.Add(e, name!);
					}

					return Parameter(e.Type, name);
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
