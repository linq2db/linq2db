using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public sealed class ConstantPlaceholderExpression : Expression
	{
		public ConstantPlaceholderExpression(Type constantType)
		{
			ConstantType = constantType;
		}

		public          Type           ConstantType { get; }
		public override ExpressionType NodeType     => ExpressionType.Extension;
		public override Type           Type         => ConstantType;
		public override bool           CanReduce    => true;

		public override Expression Reduce()
		{
			return Default(ConstantType);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase expressionVisitor)
				return expressionVisitor.VisitConstantPlaceholder(this);

			return visitor.Visit(this);
		}

		bool Equals(ConstantPlaceholderExpression other)
		{
			return ConstantType.Equals(other.ConstantType);
		}

		public override bool Equals(object? obj)
		{
			if (ReferenceEquals(null, obj))
			{
				return false;
			}

			if (ReferenceEquals(this, obj))
			{
				return true;
			}

			if (obj.GetType() != this.GetType())
			{
				return false;
			}

			return Equals((ConstantPlaceholderExpression)obj);
		}

		public override int GetHashCode()
		{
			return ConstantType.GetHashCode();
		}

		public override string ToString()
		{
			return $"placeholder<{ConstantType}>";
		}
	}
}
