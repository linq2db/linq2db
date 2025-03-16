using System.Linq;

using LinqToDB.DataProvider.ClickHouse;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	sealed class ClickHouseSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IClickHouseSpecificQueryable<TSource>
	{
		public ClickHouseSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
