namespace LinqToDB.DataProvider.Ydb
{
	/// <summary>
	/// YDB-specific table, used as the entry point for YDB table extension methods.
	/// </summary>
	/// <typeparam name="TSource">Table record type.</typeparam>
	public interface IYdbSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
