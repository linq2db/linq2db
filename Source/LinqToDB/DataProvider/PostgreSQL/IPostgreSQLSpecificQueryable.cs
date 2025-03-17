using System.Linq;

namespace LinqToDB.DataProvider.PostgreSQL
{
	public interface IPostgreSQLSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
