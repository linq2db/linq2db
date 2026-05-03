using LinqToDB.DataProvider.PostgreSQL;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), IPostgreSQLSpecificQueryable<TSource>;
}
