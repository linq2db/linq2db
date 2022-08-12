using System;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB
{
	public static class ITableExtensions
	{
		public static IModificationHandler<T> GetModificationHandler<T>(this ITable<T> table, Expression<Func<T, bool>> wherePredicate) where T : class => new ModificationHandler<T>(table, wherePredicate);
		public static int Insert<T>(this ITable<T> table, T obj) where T : notnull => table.DataContext.Insert(obj);
		public static int InsertIfNotExists<T>(this ITable<T> table, T obj, Expression<Func<T, bool>> predicate) where T : notnull => table.Any(predicate) ? 0 : table.DataContext.Insert(obj);
		public static T FirstOrInsertWithOutput<T>(this ITable<T> table, T obj, Expression<Func<T, bool>> predicate) where T : notnull => table.FirstOrDefault(predicate) ?? table.InsertWithOutput(obj);
	}
}
