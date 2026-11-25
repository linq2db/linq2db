using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;

using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.Expressions
{
	sealed class SqlEagerLoadExpression : Expression, IEquatable<SqlEagerLoadExpression>
	{
		public Expression  SequenceExpression { get; }
		public Expression? Predicate          { get; }

		/// <summary>
		/// The explicit batch's stream this load reads, or <see langword="null"/> for an ordinary eager load.
		/// </summary>
		public QueryBatchStreamInfo? Stream   { get; }

		readonly Type? _streamType;

		public SqlEagerLoadExpression(Expression sequenceExpression, Expression? predicate = null)
		{
			SequenceExpression = sequenceExpression;
			Predicate          = predicate;
		}

		// A streamed load is typed as the stream source the batch reads, not as the sequence.
		public SqlEagerLoadExpression(Expression sequenceExpression, Expression? predicate, QueryBatchStreamInfo stream, Type streamType)
			: this(sequenceExpression, predicate)
		{
			Stream      = stream;
			_streamType = streamType;
		}

		public override string ToString()
		{
			var stream = Stream == null ? null : $" {Stream}";

			if (Predicate != null)
				return $"Eager{stream}({SequenceExpression} AND {Predicate})::{Type.Name}";

			return $"Eager{stream}({SequenceExpression})::{Type.Name}";
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => _streamType ?? SequenceExpression.Type;

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlEagerLoadExpression(this);
			return base.Accept(visitor);
		}

		public bool Equals([NotNullWhen(true)] SqlEagerLoadExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (Stream != other.Stream)
			{
				return false;
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

			if (obj.GetType() != typeof(SqlEagerLoadExpression))
			{
				return false;
			}

			return Equals((SqlEagerLoadExpression)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(
				ExpressionEqualityComparer.Instance.GetHashCode(SequenceExpression),
				ExpressionEqualityComparer.Instance.GetHashCode(Predicate),
				Stream
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
				predicate = AndAlso(Predicate, predicate);

			return With(SequenceExpression, predicate);
		}

		public SqlEagerLoadExpression Update(Expression sequenceExpression, Expression? predicate = null)
		{
			if (ExpressionEqualityComparer.Instance.Equals(SequenceExpression, sequenceExpression) &&
			    ExpressionEqualityComparer.Instance.Equals(Predicate, predicate))
			{
				return this;
			}

			return With(sequenceExpression, predicate);
		}

		// A rebuilt node keeps the stream it reads.
		SqlEagerLoadExpression With(Expression sequenceExpression, Expression? predicate)
		{
			return Stream == null
				? new SqlEagerLoadExpression(sequenceExpression, predicate)
				: new SqlEagerLoadExpression(sequenceExpression, predicate, Stream, _streamType!);
		}
	}
}
