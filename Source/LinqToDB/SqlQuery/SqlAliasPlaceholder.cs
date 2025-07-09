using System;

namespace LinqToDB.SqlQuery
{
	public sealed class SqlAliasPlaceholder : SqlExpressionBase
	{
		public static readonly SqlAliasPlaceholder Instance = new();

		SqlAliasPlaceholder() { }

		public override QueryElementType ElementType => QueryElementType.SqlAliasPlaceholder;

		public override QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer.Append("%ts%");
		}

		public override int GetElementHashCode()
		{
			return 0;
		}

		public override bool Equals(ISqlExpression? other)
		{
			return ReferenceEquals(other, this);
		}

		public override bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return comparer(this, other);
		}

		public override bool CanBeNullable(NullabilityContext nullability) => false;

		public override int  Precedence => SqlQuery.Precedence.Primary;
		public override Type SystemType => typeof(object);
	}
}
