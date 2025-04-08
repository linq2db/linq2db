using System.Linq;

using LinqToDB.DataProvider.PostgreSQL;

namespace LinqToDB.Internal.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IPostgreSQLSpecificQueryable<TSource>
	{
		public PostgreSQLSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
