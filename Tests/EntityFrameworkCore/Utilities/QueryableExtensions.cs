using System.Linq;

using Microsoft.EntityFrameworkCore;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public static class QueryableExtensions
	{
		public static IQueryable<T> AsLinqToDB<T>(this IQueryable<T> queryable, bool l2db)
			=> l2db ? queryable.ToLinqToDB() : queryable;

		public static IQueryable<T> AsTracking<T>(this IQueryable<T> queryable, bool tracking) 
			where T : class 
			=> tracking ? queryable.AsTracking() : queryable.AsNoTracking();
	}
}
