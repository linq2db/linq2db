namespace LinqToDB.DataProvider.Ydb
{
	public interface IYdbSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
