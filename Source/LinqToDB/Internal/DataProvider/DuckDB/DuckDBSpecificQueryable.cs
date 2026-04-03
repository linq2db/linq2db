using LinqToDB.DataProvider.DuckDB;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	sealed class DuckDBSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query),
		IDuckDBSpecificQueryable<TSource>;
}
