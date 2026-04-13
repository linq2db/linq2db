using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServerSpecificTable<TSource>(ITable<TSource> table)
		: DatabaseSpecificTable<TSource>(table), ISqlServerSpecificTable<TSource>
		where TSource : notnull;
}
