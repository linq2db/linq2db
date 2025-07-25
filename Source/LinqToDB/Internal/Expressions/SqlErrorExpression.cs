using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Expressions;
using LinqToDB.Internal.Common;

namespace LinqToDB.Internal.Expressions
{
	public sealed class SqlErrorExpression : Expression
	{
		public SqlErrorExpression(Expression? expression, string? message, Type resultType, bool isCritical)
		{
			Expression   = expression;
			Message      = message;
			ResultType   = resultType;
			IsCritical   = isCritical;
		}

		public SqlErrorExpression(Expression expression) : this(expression, null, expression.Type, false)
		{
		}

		public SqlErrorExpression(string message, Type resultType) : this(null, message, resultType, false)
		{
		}

		public SqlErrorExpression(Expression? expression, string? message, Type resultType) : this(expression, message, resultType, false)
		{
		}

		public Expression? Expression   { get; }
		public Type        ResultType   { get; }
		public string?     Message      { get; }
		public bool        IsCritical   { get; }

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
			return new SqlErrorExpression(Expression, Message, type, IsCritical);
		}

		public Exception CreateException()
		{
			return CreateException(Expression, Message);
		}

		public static SqlErrorExpression EnsureError(Expression expression)
		{
			if (expression is SqlErrorExpression error)
				return error.WithType(expression.Type);
			return new SqlErrorExpression(expression);
		}

		public static SqlErrorExpression EnsureError(Expression expression, Type resultType)
		{
			if (expression is SqlErrorExpression error)
				return error.WithType(resultType);
			return new SqlErrorExpression(expression, null, resultType, false);
		}

		public static Exception CreateException(string message)
		{
			return new LinqToDBException(message);
		}

		public static Exception CreateException(Expression? expression, string? message)
		{
			string messageText;

			if (expression != null)
			{
				if (expression is SqlErrorExpression sqlError)
				{
					expression = PrepareExpression(sqlError.Expression);
				}

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

			return new LinqToDBException(messageText);
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

				if (e is SqlEagerLoadExpression eagerLoad)
				{
					return PrepareExpression(eagerLoad.SequenceExpression);
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
