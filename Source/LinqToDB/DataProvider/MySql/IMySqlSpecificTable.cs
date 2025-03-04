namespace LinqToDB.DataProvider.MySql
{
	public interface IMySqlSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
