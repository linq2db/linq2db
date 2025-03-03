namespace LinqToDB.DataProvider.SQLite
{
	public interface ISQLiteSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
