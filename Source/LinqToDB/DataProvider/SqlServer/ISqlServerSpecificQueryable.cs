using System.Linq;

namespace LinqToDB.DataProvider.SqlServer
{
	public interface ISqlServerSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
