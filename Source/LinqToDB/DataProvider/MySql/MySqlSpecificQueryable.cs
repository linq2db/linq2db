using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.MySql
{
	sealed class MySqlSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IMySqlSpecificQueryable<TSource>
	{
		public MySqlSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
