using System.Linq;

namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// YDB-specific query, used as the entry point for YDB query extension methods.
	/// </summary>
	/// <typeparam name="TSource">Query record type.</typeparam>
	public interface IYdbSpecificQueryable<out TSource> : IQueryable<TSource>
	{
	}
}
