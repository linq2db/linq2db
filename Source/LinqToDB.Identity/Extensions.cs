using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using LinqToDB.Data;

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
				var identity = await db.InsertWithIdentityAsync(obj, token: cancellationToken).ConfigureAwait(false);
				identity     = db.MappingSchema.ChangeType(identity, identityColumn.MemberType);

				identityColumn.MemberAccessor.SetValue(obj, identity);
			}
			else
			{
				await db.InsertAsync(obj, token: cancellationToken).ConfigureAwait(false);
			}
		}

		public static async ValueTask BulkCopyAsync<TEntity>(this IDataContext db, IEnumerable<TEntity> items, CancellationToken cancellationToken)
			where TEntity : class
		{
			var ed             = db.MappingSchema.GetEntityDescriptor(typeof(TEntity));
			var identityColumn = ed.Columns.SingleOrDefault(_ => _.IsIdentity);

			// No identity column: bulk-insert via the table API (works for both DataConnection and DataContext).
			// Use the ITable<T> overload explicitly - a DataConnection receiver would otherwise re-bind to this
			// same IDataContext extension and recurse.
			if (identityColumn == null)
			{
				await db.GetTable<TEntity>().BulkCopyAsync(items, cancellationToken).ConfigureAwait(false);
				return;
			}

			// Identity column present: insert per row so the generated key is read back.
			foreach (var item in items)
				await InsertAndSetIdentity(db, item, cancellationToken).ConfigureAwait(false);
		}
	}
}
