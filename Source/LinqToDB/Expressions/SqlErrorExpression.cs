using System;
using System.Linq.Expressions;
using LinqToDB.Linq;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
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
			return new LinqException($"'{Expression}' cannot be converted to SQL.");
		}

		public override string ToString()
		{
			return $"Error: {Expression}";
		}
	}
}
