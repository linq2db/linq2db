using LinqToDB.DataProvider.Access;
using LinqToDB.Internal.Linq;

namespace LinqToDB.Internal.DataProvider.Access
{
	sealed class AccessSpecificQueryable<TSource>(IExpressionQuery<TSource> query) : DatabaseSpecificQueryable<TSource>(query), IAccessSpecificQueryable<TSource>;
}
