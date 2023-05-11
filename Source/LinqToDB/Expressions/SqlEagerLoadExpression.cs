using System;
using System.Linq.Expressions;

namespace LinqToDB.Expressions
{
	class SqlEagerLoadExpression: Expression, IEquatable<SqlEagerLoadExpression>
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

		public bool Equals(SqlEagerLoadExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return ExpressionEqualityComparer.Instance.Equals(SequenceExpression, other.SequenceExpression);
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

			return Equals((SqlEagerLoadExpression)obj);
		}

		public override int GetHashCode()
		{
			return SequenceExpression.GetHashCode();
		}

		public static bool operator ==(SqlEagerLoadExpression? left, SqlEagerLoadExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlEagerLoadExpression? left, SqlEagerLoadExpression? right)
		{
			return !Equals(left, right);
		}
	}
}
