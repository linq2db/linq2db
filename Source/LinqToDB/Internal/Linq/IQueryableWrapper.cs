using System.Linq;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// Implemented by <see cref="IQueryable{T}"/> implementations, which wrap another query to add extra
	/// query-building functionality (e.g. queries, returned by <c>LoadWith</c> or database-specific hint methods).
	/// Provides access to wrapped query, so query consumers could detect linq2db query behind the wrapper.
	/// </summary>
	/// <typeparam name="TSource">Query element type.</typeparam>
	interface IQueryableWrapper<out TSource>
	{
		/// <summary>
		/// Gets wrapped query.
		/// </summary>
		IQueryable<TSource> WrappedQuery { get; }
	}
}
