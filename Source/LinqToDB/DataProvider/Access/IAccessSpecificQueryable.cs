using System.Linq;

namespace LinqToDB.DataProvider.Access
{
	public interface IAccessSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
