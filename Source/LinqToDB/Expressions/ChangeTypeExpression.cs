using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class ChangeTypeExpression : Expression
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

		public override string ToString()
		{
			return "(" + Type + ")" + Expression;
		}

		protected bool Equals(ChangeTypeExpression other)
		{
			return _type.Equals(other._type) && Expression.Equals(other.Expression);
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
			unchecked
			{
				return (_type.GetHashCode() * 397) ^ Expression.GetHashCode();
			}
		}
	}
}
