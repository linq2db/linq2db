using LinqToDB.DataProvider.SQLite;

namespace LinqToDB.Internal.DataProvider.SQLite
{
	sealed class SQLiteSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISQLiteSpecificTable<TSource>
		where TSource : notnull
	{
		public SQLiteSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
