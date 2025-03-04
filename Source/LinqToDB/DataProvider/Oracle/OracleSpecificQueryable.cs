using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.Oracle
{
	sealed class OracleSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IOracleSpecificQueryable<TSource>
	{
		public OracleSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
