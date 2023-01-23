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
				var identity = await db.InsertWithIdentityAsync(obj, token: cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
				identity     = db.MappingSchema.ChangeType(identity, identityColumn.MemberType);

				identityColumn.MemberAccessor.SetValue(obj, identity);
			}
			else
			{
				await db.InsertAsync(obj, token: cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
		}

		public static async ValueTask BulkCopyAsync<TEntity>(this IDataContext db, IEnumerable<TEntity> items, CancellationToken cancellationToken)
			where TEntity : class
		{
			var ed             = db.MappingSchema.GetEntityDescriptor(typeof(TEntity));
			var identityColumn = ed.Columns.SingleOrDefault(_ => _.IsIdentity);

			if (identityColumn == null)
			{
				if (db is DataConnection cn)
				{
					await cn.BulkCopyAsync(items, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					return;
				}
				else if (db is DataContext cx)
				{
					await cx.GetTable<TEntity>().BulkCopyAsync(items, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
					return;
				}
			}

			foreach (var item in items)
				await InsertAndSetIdentity(db, item, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
