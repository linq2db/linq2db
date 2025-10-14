using System.Linq;

using LinqToDB.DataProvider.MySql;

namespace LinqToDB.Internal.DataProvider.MySql
{
	sealed class MySqlSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IMySqlSpecificQueryable<TSource>
	{
		public MySqlSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
