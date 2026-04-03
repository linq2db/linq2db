using System.Linq;

namespace LinqToDB.DataProvider.DuckDB
{
	public interface IDuckDBSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
