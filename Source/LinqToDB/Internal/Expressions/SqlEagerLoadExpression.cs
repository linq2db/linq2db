using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	sealed class SqlEagerLoadExpression : Expression, IEquatable<SqlEagerLoadExpression>
	{
		public Expression  SequenceExpression { get; }
		public Expression? Predicate          { get; }

		public SqlEagerLoadExpression(Expression sequenceExpression, Expression? predicate = null)
		{
			SequenceExpression = sequenceExpression;
			Predicate          = predicate;
		}

		public override string ToString()
		{
			if (Predicate != null)
			{
				return $"Eager({SequenceExpression} AND {Predicate})::{Type.Name}";
			}

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

			if (Predicate == null)
			{
				if (other.Predicate != null)
					return false;
			}
			else if (other.Predicate == null)
			{
				return false;
			}
			else if (!ExpressionEqualityComparer.Instance.Equals(Predicate, other.Predicate))
			{
				return false;
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

			if (obj.GetType() != GetType())
			{
				return false;
			}

			return Equals((SqlEagerLoadExpression)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				ExpressionEqualityComparer.Instance.GetHashCode(SequenceExpression),
				ExpressionEqualityComparer.Instance.GetHashCode(Predicate)
			);
		}

		public static bool operator ==(SqlEagerLoadExpression? left, SqlEagerLoadExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlEagerLoadExpression? left, SqlEagerLoadExpression? right)
		{
			return !Equals(left, right);
		}

		public SqlEagerLoadExpression AppendPredicate(Expression predicate)
		{
			if (Predicate != null)
			{
				predicate = AndAlso(Predicate, predicate);
			}

			return new SqlEagerLoadExpression(SequenceExpression, predicate);
		}

		public SqlEagerLoadExpression Update(Expression sequenceExpression, Expression? predicate = null)
		{
			if (ExpressionEqualityComparer.Instance.Equals(SequenceExpression, sequenceExpression) &&
			    ExpressionEqualityComparer.Instance.Equals(Predicate, predicate))
			{
				return this;
			}

			return new SqlEagerLoadExpression(sequenceExpression, predicate);
		}
	}
}
