namespace LinqToDB.DataProvider.ClickHouse
{
	public interface IClickHouseSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
