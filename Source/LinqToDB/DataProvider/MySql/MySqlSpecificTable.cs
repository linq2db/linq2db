using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.MySql
{
	sealed class MySqlSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IMySqlSpecificTable<TSource>
		where TSource : notnull
	{
		public MySqlSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
