using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	public class SqlKeyHolderExpression : Expression, IEquatable<SqlKeyHolderExpression>
	{
		public SqlKeyHolderExpression(Expression expression)
		{
			Expression = expression;
		}

		public Expression Expression { get; }

		public override bool           CanReduce => true;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      => Expression.Type;

		public override string ToString()
		{
			return $"KH({Expression})";
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
			{
				return baseVisitor.VisitSqlKeyHolderExpression(this);
			}

			return base.Accept(visitor);
		}

		public override Expression Reduce()
		{
			return Expression;
		}

		public bool Equals(SqlKeyHolderExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return Expression.Equals(other.Expression);
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

			return Equals((SqlKeyHolderExpression)obj);
		}

		public override int GetHashCode()
		{
			return Expression.GetHashCode();
		}

		public static bool operator ==(SqlKeyHolderExpression? left, SqlKeyHolderExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlKeyHolderExpression? left, SqlKeyHolderExpression? right)
		{
			return !Equals(left, right);
		}

		public SqlKeyHolderExpression Update(Expression experssion)
		{
			if (ReferenceEquals(experssion, Expression))
				return this;

			return new SqlKeyHolderExpression(experssion);
		}
	}
}
