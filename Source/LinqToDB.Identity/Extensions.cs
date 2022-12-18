using System;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Identity
{
	internal static class Extensions
	{
		public static async ValueTask InsertAndSetIdentity<TEntity>(this IDataContext db, TEntity obj, CancellationToken cancellationToken)
			where TEntity : class
		{
			var ed             = db.MappingSchema.GetEntityDescriptor(typeof(TEntity));
			var identityColumn = ed.Columns.SingleOrDefault(_ => _.IsIdentity);

			if (identityColumn != null)
			{
				var identity = await db.InsertWithIdentityAsync(obj, token: cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				var type     = identity.GetType();

				if (type != identityColumn.MemberType)
				{
					var ex = db.MappingSchema.GetConvertExpression(identity.GetType(), identityColumn.MemberType)
						?? throw new InvalidOperationException($"Cannot build conversion from {identity.GetType()} to {identityColumn.MemberType}");

					identity = ex.Compile().DynamicInvoke(identity);
				}

				identityColumn.MemberAccessor.SetValue(obj, identity);
			}
			else
			{
				await db.InsertAsync(obj, token: cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		public static Task<int> UpdateConcurrent<TEntity>(this IDataContext db, TEntity obj, CancellationToken cancellationToken)
			where TEntity : class
		{
			const string ID    = "Id";
			const string STAMP = "ConcurrencyStamp";

			var p = Expression.Parameter(typeof(TEntity));
			var o = Expression.Constant(obj);

			// r => r.Id.Equals(obj.Id) && r.ConcurrencyStamp == obj.ConcurrencyStamp
			var filter = Expression.Lambda<Func<TEntity, bool>>(
				Expression.AndAlso(
					Expression.Call(
						Expression.PropertyOrField(p, ID),
						"Equals",
						Array.Empty<Type>(),
						Expression.PropertyOrField(o, ID)),
					Expression.Equal(
						Expression.PropertyOrField(p, STAMP),
						Expression.PropertyOrField(o, STAMP)))
				, p);

			var ed    = db.MappingSchema.GetEntityDescriptor(typeof(TEntity));
			var query = db.GetTable<TEntity>().Where(filter).AsUpdatable();

			foreach (var column in ed.Columns)
			{
				if (column.IsPrimaryKey || column.SkipOnUpdate || column.IsIdentity)
					continue;

				if (column.MemberName == STAMP)
				{
					var expr  = Expression.Lambda<Func<TEntity, string>>(Expression.PropertyOrField(p, STAMP), p);
					var stamp = Guid.NewGuid().ToString();
					query     = query.Set(expr, stamp);

					column.MemberAccessor.SetValue(obj, stamp);
				}
				else
				{
					var expr = Expression.Lambda<Func<TEntity, object?>>(
						Expression.Convert(
							Expression.PropertyOrField(p, column.MemberName),
							typeof(object)),
						p);

					query = query.Set(expr, column.MemberAccessor.GetValue(obj));
				}
			}

			return query.UpdateAsync(cancellationToken);
		}
	}
}
