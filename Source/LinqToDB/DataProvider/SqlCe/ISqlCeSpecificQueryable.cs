using System.Linq;

namespace LinqToDB.DataProvider.SqlCe
{
	public interface ISqlCeSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
