using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public sealed class ChangeTypeExpression : Expression
	{
		public const ExpressionType ChangeTypeType = (ExpressionType)1000;

		public ChangeTypeExpression(Expression expression, Type type)
		{
			Expression = expression;
			_type       = type;
		}

		readonly Type _type;

		public override Type           Type     { get { return _type;          } }
		public override ExpressionType NodeType { get { return ChangeTypeType; } }

		public Expression Expression { get; private set; }

		public override string ToString() => $"(({Type}){Expression})";

		bool Equals(ChangeTypeExpression other)
		{
			return _type == other._type && Expression.Equals(other.Expression);
		}

		public ChangeTypeExpression Update(Expression expression)
		{
			if (ReferenceEquals(Expression, expression))
				return this;

			return new ChangeTypeExpression(expression, _type);
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((ChangeTypeExpression)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(_type, Expression);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitChangeTypeExpression(this);
			return base.Accept(visitor);
		}
	}
}
