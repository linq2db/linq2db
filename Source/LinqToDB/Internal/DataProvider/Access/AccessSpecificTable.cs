using LinqToDB.DataProvider.Access;

namespace LinqToDB.Internal.DataProvider.Access
{
	sealed class AccessSpecificTable<TSource> : DatabaseSpecificTable<TSource>, IAccessSpecificTable<TSource>
		where TSource : notnull
	{
		public AccessSpecificTable(ITable<TSource> table) : base(table)
		{
		}
	}
}
