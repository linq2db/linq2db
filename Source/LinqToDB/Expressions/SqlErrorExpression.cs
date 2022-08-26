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
			return new LinqException($"'{Expression}' cannot be converted to SQL.");
		}

		public override string ToString()
		{
			return $"Error: {Expression}";
		}
	}
}
