using System;
using System.Linq.Expressions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlEagerLoadExpression: Expression
	{
		public IBuildContext BuildContext       { get; }
		public Expression    Path               { get; }
		public Expression    SequenceExpression { get; }

		public SqlEagerLoadExpression(IBuildContext buildContext, Expression path, Expression sequenceExpression)
		{
			BuildContext       = buildContext;
			Path               = path;
			SequenceExpression = sequenceExpression;
		}

		public override string ToString()
		{
			return $"Eager({BuildContextDebuggingHelper.GetContextInfo(BuildContext)})";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => Path.Type;
	}
}
