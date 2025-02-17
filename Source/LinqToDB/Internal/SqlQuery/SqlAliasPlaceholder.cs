using System;

namespace LinqToDB.Internal.SqlQuery
{
	public class SqlAliasPlaceholder : ISqlExpression
	{
		public static readonly SqlAliasPlaceholder Instance = new();

		SqlAliasPlaceholder() { }

#if DEBUG
		public string DebugText => this.ToDebugString();
#endif

		public QueryElementType ElementType => QueryElementType.SqlAliasPlaceholder;

		public QueryElementTextWriter ToString(QueryElementTextWriter writer)
		{
			return writer.Append("%ts%");
		}

		public bool Equals(ISqlExpression? other)
		{
			return other == this;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return comparer(this, other);
		}

		public bool CanBeNullable(NullabilityContext nullability) => false;
		public bool CanBeNull => false;
		public int Precedence => LinqToDB.SqlQuery.Precedence.Primary;
		public Type SystemType => typeof(object);
	}
}
