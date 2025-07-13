using System.Linq;

namespace LinqToDB.DataProvider.Oracle
{
	public interface IOracleSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
