namespace LinqToDB.DataProvider.PostgreSQL
{
	public interface IPostgreSQLSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
