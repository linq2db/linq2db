using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public sealed class TagExpression : Expression
	{
		public Expression InnerExpression { get; }
		public object     Tag             { get; }

		public TagExpression(Expression innerExpression, object tag)
		{
			InnerExpression = innerExpression;
			Tag             = tag;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => InnerExpression.Type;
		public override bool           CanReduce => true;

		public override Expression Reduce() => InnerExpression;

		public TagExpression Update(Expression innerExpression, object tag)
		{
			if (ReferenceEquals(InnerExpression, innerExpression) && Tag.Equals(tag))
				return this;
			return new TagExpression(innerExpression, tag);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitTagExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"Tagged({Tag}, {InnerExpression})";
		}
	}
}
