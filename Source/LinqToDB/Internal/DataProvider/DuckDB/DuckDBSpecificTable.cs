using LinqToDB.DataProvider.DuckDB;

namespace LinqToDB.Internal.DataProvider.DuckDB
{
	sealed class DuckDBSpecificTable<TSource>(ITable<TSource> table)
		: DatabaseSpecificTable<TSource>(table),
			IDuckDBSpecificTable<TSource>
		where TSource : notnull;
}
