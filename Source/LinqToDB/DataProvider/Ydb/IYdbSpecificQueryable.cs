using System.Linq;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Interface representing a YDB-specific LINQ-to-DB queryable source.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	public interface IYdbSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
