using System.Linq;

using LinqToDB.DataProvider.MySql;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.MySql
{
	sealed class MySqlSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), IMySqlSpecificQueryable<TSource>;
}
