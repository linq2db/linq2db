using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServerSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), ISqlServerSpecificQueryable<TSource>;
}
