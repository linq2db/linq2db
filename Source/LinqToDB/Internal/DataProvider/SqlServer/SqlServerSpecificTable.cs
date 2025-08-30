using LinqToDB.DataProvider.SqlServer;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServerSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlServerSpecificTable<TSource>
		where TSource : notnull
	{
		public SqlServerSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
