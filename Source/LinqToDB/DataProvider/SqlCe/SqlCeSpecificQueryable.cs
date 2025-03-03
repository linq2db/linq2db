using System.Linq;

using LinqToDB.Internal.DataProvider;

namespace LinqToDB.DataProvider.SqlCe
{
	sealed class SqlCeSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlCeSpecificQueryable<TSource>
	{
		public SqlCeSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
