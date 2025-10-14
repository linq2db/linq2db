namespace LinqToDB.DataProvider.SqlServer
{
	public interface ISqlServerSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
