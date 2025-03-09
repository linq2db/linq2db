using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.ClickHouse
{
	sealed class ClickHouseSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IClickHouseSpecificTable<TSource>
		where TSource : notnull
	{
		public ClickHouseSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
