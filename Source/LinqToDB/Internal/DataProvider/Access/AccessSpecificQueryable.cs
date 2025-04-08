using System.Linq;

using LinqToDB.DataProvider.Access;

namespace LinqToDB.Internal.DataProvider.Access
{
	sealed class AccessSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IAccessSpecificQueryable<TSource>
	{
		public AccessSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
