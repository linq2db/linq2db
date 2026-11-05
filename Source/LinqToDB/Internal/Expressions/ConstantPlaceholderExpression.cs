using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public sealed class ConstantPlaceholderExpression : Expression
	{
		public ConstantPlaceholderExpression(Type constantType)
			: this(constantType, -1)
		{
		}

		public ConstantPlaceholderExpression(Type constantType, int ordinal)
		{
			ConstantType = constantType;
			Ordinal      = ordinal;
		}

		public          Type           ConstantType { get; }

		/// <summary>
		/// Which object this placeholder stands for, numbered by first occurrence within one cache key; -1 when the
		/// placeholder matches any constant of its type. Two placeholders with the same ordinal match only two
		/// occurrences of the same object, so the key records which inputs are shared without recording their values.
		/// </summary>
		public          int            Ordinal      { get; }
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

			return this;
		}

		bool Equals(ConstantPlaceholderExpression other)
		{
			return ConstantType.Equals(other.ConstantType) && Ordinal == other.Ordinal;
		}

		public override bool Equals([NotNullWhen(true)] object? obj)
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
