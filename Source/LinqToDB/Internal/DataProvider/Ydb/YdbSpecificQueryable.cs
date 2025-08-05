using System.Linq;

using LinqToDB.DataProvider.Ydb;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	sealed class YdbSpecificQueryable<TSource>
		: DatabaseSpecificQueryable<TSource>,
			IYdbSpecificQueryable<TSource>
	{
		public YdbSpecificQueryable(IQueryable<TSource> queryable) : base(queryable) { }
	}
}
