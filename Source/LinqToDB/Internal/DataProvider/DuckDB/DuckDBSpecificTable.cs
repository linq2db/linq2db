using LinqToDB.DataProvider.DuckDB;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	sealed class DuckDBSpecificTable<TSource>
		: DatabaseSpecificTable<TSource>,
			IDuckDBSpecificTable<TSource>
		where TSource : notnull
	{
		public DuckDBSpecificTable(ITable<TSource> table) : base(table) { }
	}
}
