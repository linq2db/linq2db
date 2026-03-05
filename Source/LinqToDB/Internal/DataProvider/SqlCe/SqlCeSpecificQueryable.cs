using LinqToDB.DataProvider.SqlCe;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	sealed class SqlCeSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), ISqlCeSpecificQueryable<TSource>;
}
