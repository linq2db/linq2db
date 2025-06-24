namespace LinqToDB.DataProvider.SqlCe
{
	public interface ISqlCeSpecificTable<out TSource> : ITable<TSource>
		where TSource : notnull
	{
	}
}
