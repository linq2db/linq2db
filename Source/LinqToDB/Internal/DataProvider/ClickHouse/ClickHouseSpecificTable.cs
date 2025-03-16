using LinqToDB.DataProvider.ClickHouse;

namespace LinqToDB.Internal.DataProvider.ClickHouse
{
	sealed class ClickHouseSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IClickHouseSpecificTable<TSource>
		where TSource : notnull
	{
		public ClickHouseSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
