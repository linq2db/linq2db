namespace LinqToDB.DataProvider.Access
{
	public interface IAccessSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
