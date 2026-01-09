using System.Collections.Generic;

namespace LinqToDB.Internal.SqlQuery
{
	public sealed class ISqlPredicateEqualityComparer : IEqualityComparer<ISqlPredicate>
	{
		public static readonly IEqualityComparer<ISqlPredicate> Instance = new ISqlPredicateEqualityComparer();

		private ISqlPredicateEqualityComparer()
		{
		}

		bool IEqualityComparer<ISqlPredicate>.Equals(ISqlPredicate? x, ISqlPredicate? y)
		{
			if (ReferenceEquals(x, y))
				return true;

			if (x is not null && y is not null)
				return x.Equals(y, SqlExtensions.DefaultComparer);

			return false;
		}

		int IEqualityComparer<ISqlPredicate>.GetHashCode(ISqlPredicate obj)
		{
			return obj.GetElementHashCode();
		}
	}
}
