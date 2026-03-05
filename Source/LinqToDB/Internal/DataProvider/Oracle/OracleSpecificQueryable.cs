using LinqToDB.DataProvider.Oracle;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class OracleSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), IOracleSpecificQueryable<TSource>;
}
