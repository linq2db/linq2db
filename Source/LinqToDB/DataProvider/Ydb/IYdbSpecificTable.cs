namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// Interface representing a YDB-specific LINQ-to-DB table.
	/// </summary>
	/// <typeparam name="TSource">The type of the entity.</typeparam>
	public interface IYdbSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
