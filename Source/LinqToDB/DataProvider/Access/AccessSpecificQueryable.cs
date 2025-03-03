using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.Access
{
	sealed class AccessSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IAccessSpecificQueryable<TSource>
	{
		public AccessSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
