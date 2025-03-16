using LinqToDB.DataProvider.Oracle;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class OracleSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IOracleSpecificTable<TSource>
		where TSource : notnull
	{
		public OracleSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
