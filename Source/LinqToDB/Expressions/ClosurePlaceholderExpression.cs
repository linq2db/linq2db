using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public class ClosurePlaceholderExpression : Expression
	{
		public Expression ClosureExpression { get; }

		public ClosurePlaceholderExpression(Expression closureExpression)
		{
			ClosureExpression = closureExpression;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => ClosureExpression.Type;
		public override bool           CanReduce => true;

		public override Expression     Reduce() => ClosureExpression;

		public ClosurePlaceholderExpression Update(Expression closureExpression)
		{
			if (ReferenceEquals(ClosureExpression, closureExpression))
				return this;
			return new ClosurePlaceholderExpression(closureExpression);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitClosurePlaceholderExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"$({ClosureExpression})";
		}
	}
}
