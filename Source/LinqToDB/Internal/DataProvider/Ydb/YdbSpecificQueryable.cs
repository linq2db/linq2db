using System.Linq;

using LinqToDB.DataProvider.Ydb;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Internal implementation of a YDB-specific queryable wrapper.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	sealed class YdbSpecificQueryable<TSource>
		: DatabaseSpecificQueryable<TSource>,
			IYdbSpecificQueryable<TSource>
	{
		public YdbSpecificQueryable(IQueryable<TSource> queryable) : base(queryable) { }
	}
}
