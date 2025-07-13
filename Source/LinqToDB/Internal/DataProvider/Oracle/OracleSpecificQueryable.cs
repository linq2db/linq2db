using System.Linq;

using LinqToDB.DataProvider.Oracle;

namespace LinqToDB.Internal.DataProvider.Oracle
{
	sealed class OracleSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, IOracleSpecificQueryable<TSource>
	{
		public OracleSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
