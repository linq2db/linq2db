using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class SqlEagerLoadExpression: Expression
	{
		public Expression SequenceExpression { get; }

		public SqlEagerLoadExpression(Expression sequenceExpression)
		{
			SequenceExpression   = sequenceExpression;
		}

		public override string ToString()
		{
			return $"Eager({SequenceExpression})::{Type.Name}";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => SequenceExpression.Type;

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlEagerLoadExpression(this);
			return base.Accept(visitor);
		}
	}
}
