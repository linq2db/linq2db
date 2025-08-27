using System.Linq;

namespace LinqToDB.DataProvider.Ydb
{
	public interface IYdbSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
