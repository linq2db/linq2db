using System;
using System.Linq.Expressions;

namespace LinqToDB.Internal.Expressions
{
	sealed class SqlReaderIsNullExpression : Expression, IEquatable<SqlReaderIsNullExpression>
	{
		public SqlPlaceholderExpression Placeholder { get; }
		public bool                     IsNot       { get; }

		public SqlReaderIsNullExpression(SqlPlaceholderExpression placeholder, bool isNot)
		{
			Placeholder = placeholder;
			IsNot       = isNot;
		}

		public override ExpressionType NodeType => ExpressionType.Extension;
		public override Type           Type     => typeof(bool);

		public SqlReaderIsNullExpression Update(SqlPlaceholderExpression placeholder)
		{
			if (ReferenceEquals(placeholder, Placeholder))
				return this;

			return new SqlReaderIsNullExpression(placeholder, IsNot);
		}

		public SqlReaderIsNullExpression WithIsNot(bool isNot)
		{
			if (IsNot == isNot)
				return this;
			return new SqlReaderIsNullExpression(Placeholder, isNot);
		}

		public override string ToString()
		{
			return IsNot ? $"IsNotDbNull({Placeholder})" : $"IsDbNull({Placeholder})";
		}

		public bool Equals(SqlReaderIsNullExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			return ExpressionEqualityComparer.Instance.Equals(Placeholder, other.Placeholder) && IsNot == other.IsNot;
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

			return Equals((SqlReaderIsNullExpression)obj);
		}

		public override int GetHashCode()
		{
			return HashCode.Combine(Placeholder, IsNot);
		}

		public static bool operator ==(SqlReaderIsNullExpression? left, SqlReaderIsNullExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlReaderIsNullExpression? left, SqlReaderIsNullExpression? right)
		{
			return !Equals(left, right);
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
				return baseVisitor.VisitSqlReaderIsNullExpression(this);
			return base.Accept(visitor);
		}
	}
}
