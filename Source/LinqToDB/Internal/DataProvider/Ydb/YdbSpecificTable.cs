using LinqToDB.DataProvider.Ydb;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	/// <summary>
	/// Internal implementation of a YDB-specific table wrapper.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	sealed class YdbSpecificTable<TSource>
		: DatabaseSpecificTable<TSource>,
			IYdbSpecificTable<TSource>
		where TSource : notnull
	{
		public YdbSpecificTable(ITable<TSource> table) : base(table) { }
	}
}
