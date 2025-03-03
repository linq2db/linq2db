using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.SqlCe
{
	sealed class SqlCeSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlCeSpecificTable<TSource>
		where TSource : notnull
	{
		public SqlCeSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
