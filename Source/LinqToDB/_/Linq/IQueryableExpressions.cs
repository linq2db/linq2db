using LinqToDB;
using LinqToDB.Linq;

namespace System.Linq
{
		public static class IQueryableExpressions
	{
		public static T? FirstOrInsert<T>(this IQueryable<T> queryable, IValueInsertable<T> insertable)
		{
			var t = queryable.FirstOrDefault();
			if (t == null)
			{
				insertable.Insert();
				t = queryable.FirstOrDefault();
			}
			return t;
		}

	}
}
