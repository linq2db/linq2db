using System.Linq;

using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	sealed class ClickHouseSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), IClickHouseSpecificQueryable<TSource>;
}
