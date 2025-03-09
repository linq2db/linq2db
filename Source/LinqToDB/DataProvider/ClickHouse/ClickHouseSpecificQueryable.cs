using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.ClickHouse
{
	sealed class ClickHouseSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IClickHouseSpecificQueryable<TSource>
	{
		public ClickHouseSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
