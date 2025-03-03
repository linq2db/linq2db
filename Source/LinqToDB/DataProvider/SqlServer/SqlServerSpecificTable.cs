using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.SqlServer
{
	sealed class SqlServerSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlServerSpecificTable<TSource>
		where TSource : notnull
	{
		public SqlServerSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
