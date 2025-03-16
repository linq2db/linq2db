using LinqToDB.DataProvider.SqlCe;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	sealed class SqlCeSpecificTable<TSource> : DatabaseSpecificTable<TSource>, ISqlCeSpecificTable<TSource>
		where TSource : notnull
	{
		public SqlCeSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
