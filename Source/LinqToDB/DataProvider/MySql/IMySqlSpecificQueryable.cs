using System.Linq;

namespace LinqToDB.DataProvider.MySql
{
	public interface IMySqlSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
