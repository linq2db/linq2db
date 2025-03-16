using System.Linq;

using LinqToDB.DataProvider.SqlCe;

namespace LinqToDB.Internal.DataProvider.SqlCe
{
	sealed class SqlCeSpecificQueryable<TSource> : DatabaseSpecificQueryable<TSource>, ISqlCeSpecificQueryable<TSource>
	{
		public SqlCeSpecificQueryable(IQueryable<TSource> queryable) : base(queryable)
		{
		}
	}
}
