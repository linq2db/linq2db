using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.PostgreSQL
{
	sealed class PostgreSQLSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IPostgreSQLSpecificQueryable<TSource>
	{
		public PostgreSQLSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
