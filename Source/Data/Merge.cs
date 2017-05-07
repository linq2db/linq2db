using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data
{
	[PublicAPI]
	public static class MergeExtensions
	{
		#region From
		public static IMergeSource<TTarget, TSource> From<TTarget, TSource>(
			this ITable<TTarget> target,
			IEnumerable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
				where TTarget : class
				where TSource : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (matchPredicate == null)
				throw new ArgumentNullException(nameof(matchPredicate));

			return new MergeDefinition<TTarget, TSource>(target, source, matchPredicate);
		}

		public static IMergeSource<TTarget, TSource> From<TTarget, TSource>(
			this ITable<TTarget> target,
			IQueryable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
				where TTarget : class
				where TSource : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (matchPredicate == null)
				throw new ArgumentNullException(nameof(matchPredicate));

			return new MergeDefinition<TTarget, TSource>(target, source, matchPredicate);
		}

		public static IMergeSource<TEntity> From<TEntity>(
			this ITable<TEntity> target,
			IEnumerable<TEntity> source)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MergeDefinition<TEntity, TEntity>(target, source, null);
		}

		public static IMergeSource<TEntity> From<TEntity>(this ITable<TEntity> target, IQueryable<TEntity> source)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			return new MergeDefinition<TEntity, TEntity>(target, source, null);
		}

		public static IMergeSource<TEntity> From<TEntity>(
			this ITable<TEntity> target,
			IEnumerable<TEntity> source,
			Expression<Func<TEntity, TEntity, bool>> matchPredicate)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (matchPredicate == null)
				throw new ArgumentNullException(nameof(matchPredicate));

			return new MergeDefinition<TEntity, TEntity>(target, source, matchPredicate);
		}

		public static IMergeSource<TEntity> From<TEntity>(
			this ITable<TEntity> target,
			IQueryable<TEntity> source,
			Expression<Func<TEntity, TEntity, bool>> matchPredicate)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			if (source == null)
				throw new ArgumentNullException(nameof(source));

			if (matchPredicate == null)
				throw new ArgumentNullException(nameof(matchPredicate));

			return new MergeDefinition<TEntity, TEntity>(target, source, matchPredicate);
		}
		#endregion

		#region Insert
		public static IMerge<TEntity> Insert<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(null, null));
		}

		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(predicate, null));
		}

		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity>> create)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (create == null)
				throw new ArgumentNullException(nameof(create));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(null, create));
		}

		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity>> create)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (create == null)
				throw new ArgumentNullException(nameof(create));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(predicate, create));
		}

		public static IMerge<TTarget, TSource> Insert<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TSource, TTarget>> create)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (create == null)
				throw new ArgumentNullException(nameof(create));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Insert(null, create));
		}

		public static IMerge<TTarget, TSource> Insert<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TSource, bool>> predicate,
			Expression<Func<TSource, TTarget>> create)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (create == null)
				throw new ArgumentNullException(nameof(create));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Insert(predicate, create));
		}
		#endregion

		#region Update
		public static IMerge<TEntity> Update<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(null, null));
		}

		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(predicate, null));
		}

		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(null, update));
		}

		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(predicate, update));
		}

		public static IMerge<TTarget, TSource> Update<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Update(null, update));
		}

		public static IMerge<TTarget, TSource> Update<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> predicate,
			Expression<Func<TTarget, TSource, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Update(predicate, update));
		}
		#endregion

		#region Delete
		public static IMerge<TEntity> Delete<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Delete(null));
		}

		public static IMerge<TEntity> Delete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Delete(predicate));
		}

		public static IMerge<TTarget, TSource> Delete<TTarget, TSource>(this IMergeSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Delete(null));
		}

		public static IMerge<TTarget, TSource> Delete<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> predicate)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Delete(predicate));
		}
		#endregion

		#region UpdateBySource
		public static IMerge<TEntity> UpdateBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateBySource(null, update));
		}

		public static IMerge<TEntity> UpdateBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateBySource(predicate, update));
		}

		public static IMerge<TTarget, TSource> UpdateBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateBySource(null, update));
		}

		public static IMerge<TTarget, TSource> UpdateBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> predicate,
			Expression<Func<TTarget, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			if (update == null)
				throw new ArgumentNullException(nameof(update));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateBySource(predicate, update));
		}
		#endregion

		#region DeleteBySource
		public static IMerge<TEntity> DeleteBySource<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.DeleteBySource(null));
		}

		public static IMerge<TEntity> DeleteBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.DeleteBySource(predicate));
		}

		public static IMerge<TTarget, TSource> DeleteBySource<TTarget, TSource>(this IMergeSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.DeleteBySource(null));
		}

		public static IMerge<TTarget, TSource> DeleteBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> predicate)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			if (predicate == null)
				throw new ArgumentNullException(nameof(predicate));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.DeleteBySource(predicate));
		}
		#endregion

		#region Merge
		public static int Merge<TTarget, TSource>(this IMerge<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			var definition = (MergeDefinition<TTarget, TSource>)merge;


			var dataConnection = definition.Target.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, definition);
		}

		public static int Merge<TEntity>(this IMerge<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			return Merge<TEntity, TEntity>((MergeDefinition<TEntity, TEntity>)merge);
		}
		#endregion
	}

	/// <summary>
	/// Represents merge operation source and target configutation without operations.
	/// </summary>
	public interface IMergeSource<TTarget, TSource>
	{
	}

	/// <summary>
	/// Represents merge operation source and target configutation without operations.
	/// </summary>
	public interface IMergeSource<TEntity>
	{
	}

	/// <summary>
	/// Represents merge operation configuration.
	/// </summary>
	public interface IMerge<TTarget, TSource> : IMergeSource<TTarget, TSource>
	{
	}

	/// <summary>
	/// Represents merge operation configuration.
	/// </summary>
	public interface IMerge<TEntity> : IMergeSource<TEntity>
	{
	}
}
