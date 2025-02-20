using System;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;

namespace LinqToDB.Internal.Expressions
{
	public class SqlPathExpression : Expression, IEquatable<SqlPathExpression>
	{
		public SqlPathExpression(Expression[] path, Type type)
		{
			Type = type;
			Path = path;
		}

		public Expression[] Path { get; set; }

		public override bool           CanReduce => false;
		public override ExpressionType NodeType  => ExpressionType.Extension;
		public override Type           Type      { get; }

		public override string ToString()
		{
			return "$Path$->" + string.Join("->", Path.Select(p => p.ToString()));
		}

		protected override Expression Accept(ExpressionVisitor visitor)
		{
			if (visitor is ExpressionVisitorBase baseVisitor)
			{
				return baseVisitor.VisitSqlPathExpression(this);
			}

			return base.Accept(visitor);
		}

		public bool Equals(SqlPathExpression? other)
		{
			if (ReferenceEquals(null, other))
			{
				return false;
			}

			if (ReferenceEquals(this, other))
			{
				return true;
			}

			if (Type != other.Type)
			{
				return false;
			}

			return Path.SequenceEqual(other.Path, ExpressionEqualityComparer.Instance);
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

			return Equals((SqlPathExpression)obj);
		}

		public override int GetHashCode()
		{
			return Type.GetHashCode();
		}

		public static bool operator ==(SqlPathExpression? left, SqlPathExpression? right)
		{
			return Equals(left, right);
		}

		public static bool operator !=(SqlPathExpression? left, SqlPathExpression? right)
		{
			return !Equals(left, right);
		}

		public SqlPathExpression Update(Expression[] path)
		{
			if (ReferenceEquals(Path, path) || path.SequenceEqual(Path, ExpressionEqualityComparer.Instance))
				return this;

			return new SqlPathExpression(path, Type);
		}

		public SqlPathExpression WithType(Type type)
		{
			if (Type == type)
				return this;

			return new SqlPathExpression(Path, type);
		}

	}
}
