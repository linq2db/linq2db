using LinqToDB.Internal.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	public interface ISqlServerSpecificQueryable<out TSource> : IExpressionQuery<TSource>
	{
	}
}
