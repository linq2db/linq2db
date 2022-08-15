using System;
using System.Linq.Expressions;
using LinqToDB.Linq.Builder;

namespace LinqToDB.Expressions
{
	class SqlEagerLoadExpression: Expression
	{
		public ContextRefExpression ContextRef { get; }
		public Expression           Path       { get; }

		public Expression    SequenceExpression { get; }

		public SqlEagerLoadExpression(ContextRefExpression contextRef, Expression path, Expression sequenceExpression)
		{
			ContextRef         = contextRef;
			Path               = path;
			SequenceExpression = sequenceExpression;
		}

		public override string ToString()
		{
			return $"Eager({BuildContextDebuggingHelper.GetContextInfo(ContextRef.BuildContext)}: {ContextRef.Type.Name})";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => Path.Type;
	}
}
