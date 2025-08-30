using System;

namespace LinqToDB.Internal.SqlQuery
{
	public abstract class SqlExpressionBase : QueryElement, ISqlExpression
	{
		public virtual bool Equals(ISqlExpression? other)
		{
			if (ReferenceEquals(this, other))
				return true;

			if (ReferenceEquals(other, null))
				return false;

			return Equals(other, SqlExtensions.DefaultComparer);
		}

		public abstract bool  Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer);
		public abstract bool  CanBeNullable(NullabilityContext nullability);
		public abstract int   Precedence { get; }
		public abstract Type? SystemType { get; }
	}
}
