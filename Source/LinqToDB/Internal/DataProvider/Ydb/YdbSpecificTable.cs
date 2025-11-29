using LinqToDB.DataProvider.Ydb;

namespace LinqToDB.Internal.DataProvider.Ydb
{
	sealed class YdbSpecificTable<TSource>
		: DatabaseSpecificTable<TSource>,
			IYdbSpecificTable<TSource>
		where TSource : notnull
	{
		public YdbSpecificTable(ITable<TSource> table) : base(table) { }
	}
}
