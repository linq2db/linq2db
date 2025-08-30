using LinqToDB.DataProvider.PostgreSQL;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IPostgreSQLSpecificTable<TSource>
			where TSource : notnull
		{
			public PostgreSQLSpecificTable(ITable<TSource> table) : base(table)
			{
			}
		}
}
