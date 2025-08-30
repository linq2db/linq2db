using System.Linq;

namespace LinqToDB.DataProvider.ClickHouse
{
	public interface IClickHouseSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
