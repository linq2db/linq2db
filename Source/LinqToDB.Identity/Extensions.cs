using System;
using System.Linq;
using System.Linq.Expressions;
using LinqToDB.Mapping;

namespace LinqToDB.Identity
{
	internal static class Extensions
	{
		public static T TryInsertAndSetIdentity<T>(this IDataContext db, T obj)
			where T : class
		{
			var ms = db.MappingSchema;
			var od = ms.GetEntityDescriptor(obj.GetType());

			var identity = od.Columns.FirstOrDefault(_ => _.IsIdentity);
			if (identity != null)
			{
				var res = db.InsertWithIdentity(obj);
				ms.SetValue(obj, res, identity);
			}
			else
			{
				db.Insert(obj);
			}

			return obj;
		}

		public static void SetValue(this MappingSchema ms, object o, object val, ColumnDescriptor column)
		{
			var ex = ms.GetConvertExpression(val.GetType(), column.MemberType);

			column.MemberAccessor.SetValue(o, ex.Compile().DynamicInvoke(val));
		}

		public static int UpdateConcurrent<T, TKey>(this IDataContext dc, T obj)
			where T : class, IConcurrency<TKey>
			where TKey : IEquatable<TKey>
		{
			var stamp = Guid.NewGuid().ToString();

			var query = dc.GetTable<T>()
				.Where(_ => _.Id.Equals(obj.Id) && _.ConcurrencyStamp == obj.ConcurrencyStamp)
				.Set(_ => _.ConcurrencyStamp, stamp);

			var ed = dc.MappingSchema.GetEntityDescriptor(typeof(T));
			var p = Expression.Parameter(typeof(T));
			foreach (
				var column in
				ed.Columns.Where(
					_ => _.MemberName != nameof(IConcurrency<TKey>.ConcurrencyStamp) && !_.IsPrimaryKey && !_.SkipOnUpdate))
			{
				var expr = Expression
					.Lambda<Func<T, object>>(
						Expression.Convert(Expression.PropertyOrField(p, column.MemberName), typeof(object)),
						p);

				var val = column.MemberAccessor.Getter(obj);
				query = query.Set(expr, val);
			}

			var res = query.Update();
			obj.ConcurrencyStamp = stamp;

			return res;
		}
	}
}