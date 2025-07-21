using LinqToDB.DataProvider.MySql;

namespace LinqToDB.Internal.DataProvider.MySql
{
	sealed class MySqlSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IMySqlSpecificTable<TSource>
		where TSource : notnull
	{
		public MySqlSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
