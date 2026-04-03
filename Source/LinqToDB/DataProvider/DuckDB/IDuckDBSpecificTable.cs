namespace LinqToDB.DataProvider.DuckDB
{
	public interface IDuckDBSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
