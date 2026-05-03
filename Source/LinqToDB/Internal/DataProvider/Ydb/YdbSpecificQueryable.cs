using LinqToDB.DataProvider.Ydb;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	sealed class YdbSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query),
		IYdbSpecificQueryable<TSource>;
}
