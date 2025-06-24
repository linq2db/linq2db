namespace LinqToDB.DataProvider.Oracle
{
	public interface IOracleSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
