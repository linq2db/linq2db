using System;

namespace LinqToDB.SqlQuery
{
	public class SqlAliasPlaceholder : ISqlExpression
	{
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
			return other != null && other.GetType() == GetType();
		}

		public ISqlExpression Walk<TContext>(WalkOptions options, TContext context, Func<TContext, ISqlExpression, ISqlExpression> func)
		{
			return this;
		}

		public bool Equals(ISqlExpression other, Func<ISqlExpression, ISqlExpression, bool> comparer)
		{
			return comparer(this, other);
		}

		public bool CanBeNullable(NullabilityContext nullability) => false;
		public bool CanBeNull => false;
		public int Precedence => SqlQuery.Precedence.Primary;
		public Type SystemType => typeof(object);
	}
}
