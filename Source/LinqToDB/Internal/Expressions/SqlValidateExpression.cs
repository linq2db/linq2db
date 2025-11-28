using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	public sealed class SqlValidateExpression : Expression
	{
		public Expression                   InnerExpression { get; }
		public Func<Expression, Expression> Validator       { get; }

		public SqlValidateExpression(Expression innerExpression, Func<Expression, Expression> validator)
		{
			InnerExpression = innerExpression;
			Validator       = validator;
		}

		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override bool           CanReduce => true;
		public override Type           Type      => InnerExpression.Type;

		public override Expression Reduce() => Validator(InnerExpression);

		public SqlValidateExpression Update(Expression innerExpression)
		{
			if (ReferenceEquals(InnerExpression, innerExpression))
			{
				return this;
			}

			return new SqlValidateExpression(innerExpression, Validator);
		}

		bool Equals(SqlValidateExpression other)
		{
			return InnerExpression.Equals(other.InnerExpression) && Validator.Equals(other.Validator);
		}

		public override bool Equals(object? obj)
		{
			return ReferenceEquals(this, obj) || obj is SqlValidateExpression other && Equals(other);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(InnerExpression, Validator);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlValidateExpression(this);
			return base.Accept(visitor);
		}

		public override string ToString()
		{
			return $"V({InnerExpression})";
		}
	}
}
