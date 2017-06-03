using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Data
{
	/// <summary>
	/// Contains extension methods for merge API.
	/// </summary>
	[PublicAPI]
	public static class MergeExtensions
	{
		#region From
		/// <summary>
		/// Configure merge command's source, which has different type compared to target, using client-side
		/// collection of objects and custom match predicate.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="target">Merge target table.</param>
		/// <param name="source">Merge source collection.</param>
		/// <param name="matchPredicate">Custom merge match predicate.</param>
		/// <returns>Returns merge command build interface.</returns>
		public static IMergeSource<TTarget, TSource> From<TTarget, TSource>(
			this ITable<TTarget> target,
			IEnumerable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
				where TTarget : class
				where TSource : class
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (source == null)
				throw new ArgumentNullException("source");

			if (matchPredicate == null)
				throw new ArgumentNullException("matchPredicate");

			return new MergeDefinition<TTarget, TSource>(target, source, matchPredicate);
		}

		/// <summary>
		/// Configure merge command's source, which has different type compared to target, using query or table
		/// and custom match predicate.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="target">Merge target table.</param>
		/// <param name="source">Merge source query or table.</param>
		/// <param name="matchPredicate">Custom merge match predicate.</param>
		/// <returns>Returns merge command build interface.</returns>
		public static IMergeSource<TTarget, TSource> From<TTarget, TSource>(
			this ITable<TTarget> target,
			IQueryable<TSource> source,
			Expression<Func<TTarget, TSource, bool>> matchPredicate)
				where TTarget : class
				where TSource : class
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (source == null)
				throw new ArgumentNullException("source");

			if (matchPredicate == null)
				throw new ArgumentNullException("matchPredicate");

			return new MergeDefinition<TTarget, TSource>(target, source, matchPredicate);
		}

		/// <summary>
		/// Configure merge command's source, which has the same type as target, using client-side collection and match
		/// on primary key columns.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="target">Merge target table.</param>
		/// <param name="source">Merge source collection.</param>
		/// <returns>Returns merge command build interface.</returns>
		public static IMergeSource<TEntity> FromSame<TEntity>(
			this ITable<TEntity> target,
			IEnumerable<TEntity> source)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (source == null)
				throw new ArgumentNullException("source");

			return new MergeDefinition<TEntity, TEntity>(target, source, null);
		}

		/// <summary>
		/// Configure merge command's source, which has the same type as target, using query or table and match on primary
		/// key columns.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="target">Merge target table.</param>
		/// <param name="source">Merge source query or table.</param>
		/// <returns>Returns merge command build interface.</returns>
		public static IMergeSource<TEntity> FromSame<TEntity>(this ITable<TEntity> target, IQueryable<TEntity> source)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (source == null)
				throw new ArgumentNullException("source");

			return new MergeDefinition<TEntity, TEntity>(target, source, null);
		}

		/// <summary>
		/// Configure merge command's source, which has the same type as target, using client-side collection and custom
		/// match predicate.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="target">Merge target table.</param>
		/// <param name="source">Merge source collection.</param>
		/// <param name="matchPredicate">Custom merge match predicate.</param>
		/// <returns>Returns merge command build interface.</returns>
		public static IMergeSource<TEntity> FromSame<TEntity>(
			this ITable<TEntity> target,
			IEnumerable<TEntity> source,
			Expression<Func<TEntity, TEntity, bool>> matchPredicate)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (source == null)
				throw new ArgumentNullException("source");

			if (matchPredicate == null)
				throw new ArgumentNullException("matchPredicate");

			return new MergeDefinition<TEntity, TEntity>(target, source, matchPredicate);
		}

		/// <summary>
		/// Configure merge command's source, which has the same type as target, using query or table and custom
		/// match predicate.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="target">Merge target table.</param>
		/// <param name="source">Merge source query or table.</param>
		/// <param name="matchPredicate">Custom merge match predicate.</param>
		/// <returns>Returns merge command build interface.</returns>
		public static IMergeSource<TEntity> FromSame<TEntity>(
			this ITable<TEntity> target,
			IQueryable<TEntity> source,
			Expression<Func<TEntity, TEntity, bool>> matchPredicate)
				where TEntity : class
		{
			if (target == null)
				throw new ArgumentNullException("target");

			if (source == null)
				throw new ArgumentNullException("source");

			if (matchPredicate == null)
				throw new ArgumentNullException("matchPredicate");

			return new MergeDefinition<TEntity, TEntity>(target, source, matchPredicate);
		}
		#endregion

		#region Insert
		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using data from the same fields of source record
		/// for each new record from source, not processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Insert<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(null, null));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using data from the same fields of source record
		/// for each new record from source that passes filtering with specified predicate, if it wasn't
		/// processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over source record.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(predicate, null));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using user-defined values for target columns
		/// for each new record from source that passes filtering with specified predicate, if it wasn't
		/// processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="create">Create record expression using source record. Expression should be a call to target
		/// record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity>> create)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (create == null)
				throw new ArgumentNullException("create");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(null, create));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using user-defined values for target columns
		/// for each new record from source that passes filtering with specified predicate, if it wasn't
		/// processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over source record.</param>
		/// <param name="create">Create record expression using source record. Expression should be a call to target
		/// record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Insert<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity>> create)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (create == null)
				throw new ArgumentNullException("create");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Insert(predicate, create));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using user-defined values for target columns
		/// for each new record from source, not processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="create">Create record expression using source record. Expression should be a call to target
		/// record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> Insert<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TSource, TTarget>> create)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (create == null)
				throw new ArgumentNullException("create");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Insert(null, create));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using user-defined values for target columns
		/// for each new record from source that passes filtering with specified predicate, if it wasn't
		/// processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over source record.</param>
		/// <param name="create">Create record expression using source record. Expression should be a call to target
		/// record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> Insert<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TSource, bool>> predicate,
			Expression<Func<TSource, TTarget>> create)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (create == null)
				throw new ArgumentNullException("create");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Insert(predicate, create));
		}
		#endregion

		#region Update
		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Update<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(null, null));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(predicate, null));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(null, update));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target and source records.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Update<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Update(predicate, update));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> Update<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Update(null, update));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target and source records.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> Update<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> predicate,
			Expression<Func<TTarget, TSource, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Update(predicate, update));
		}
		#endregion

		#region UpdateWithDelete
		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// After that it removes updated records if they are matched by delete predicate.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="deletePredicate">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> UpdateWithDelete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> deletePredicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (deletePredicate == null)
				throw new ArgumentNullException("deletePredicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateWithDelete(null, null, deletePredicate));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// After that it removes updated records if they are matched by delete predicate.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="updatePredicate">Update execution condition over target and source records.</param>
		/// <param name="deletePredicate">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> UpdateWithDelete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> updatePredicate,
			Expression<Func<TEntity, TEntity, bool>> deletePredicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (updatePredicate == null)
				throw new ArgumentNullException("updatePredicate");

			if (deletePredicate == null)
				throw new ArgumentNullException("deletePredicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateWithDelete(updatePredicate, null, deletePredicate));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// After that it removes updated records if they are matched by delete predicate.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <param name="deletePredicate">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> UpdateWithDelete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, TEntity>> update,
			Expression<Func<TEntity, TEntity, bool>> deletePredicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (update == null)
				throw new ArgumentNullException("update");

			if (deletePredicate == null)
				throw new ArgumentNullException("deletePredicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateWithDelete(null, update, deletePredicate));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// After that it removes updated records if they are matched by delete predicate.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="updatePredicate">Update execution condition over target and source records.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <param name="deletePredicate">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> UpdateWithDelete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> updatePredicate,
			Expression<Func<TEntity, TEntity, TEntity>> update,
			Expression<Func<TEntity, TEntity, bool>> deletePredicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (updatePredicate == null)
				throw new ArgumentNullException("updatePredicate");

			if (update == null)
				throw new ArgumentNullException("update");

			if (deletePredicate == null)
				throw new ArgumentNullException("deletePredicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateWithDelete(updatePredicate, update, deletePredicate));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// After that it removes updated records if they matched by delete predicate.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <param name="deletePredicate">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> UpdateWithDelete<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, TTarget>> update,
			Expression<Func<TTarget, TSource, bool>> deletePredicate)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (update == null)
				throw new ArgumentNullException("update");

			if (deletePredicate == null)
				throw new ArgumentNullException("deletePredicate");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateWithDelete(null, update, deletePredicate));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// After that it removes updated records if they matched by delete predicate.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="updatePredicate">Update execution condition over target and source records.</param>
		/// <param name="update">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <param name="deletePredicate">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> UpdateWithDelete<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> updatePredicate,
			Expression<Func<TTarget, TSource, TTarget>> update,
			Expression<Func<TTarget, TSource, bool>> deletePredicate)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (updatePredicate == null)
				throw new ArgumentNullException("updatePredicate");

			if (update == null)
				throw new ArgumentNullException("update");

			if (deletePredicate == null)
				throw new ArgumentNullException("deletePredicate");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateWithDelete(updatePredicate, update, deletePredicate));
		}
		#endregion

		#region Delete
		/// <summary>
		/// Adds new delete operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched in source and target,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Delete<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Delete(null));
		}

		/// <summary>
		/// Adds new delete operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched in source and target,
		/// if it was matched by operation predicate and wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> Delete<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.Delete(predicate));
		}

		/// <summary>
		/// Adds new delete operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched in source and target,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> Delete<TTarget, TSource>(this IMergeSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Delete(null));
		}

		/// <summary>
		/// Adds new delete operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched in source and target,
		/// if it was matched by operation predicate and wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> Delete<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> predicate)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Delete(predicate));
		}
		#endregion

		#region UpdateBySource
		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new update by source operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table for each record that was matched only in target
		/// using user-defined values for target columns, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="update">Update record expression using target record. Expression should be a call to
		/// target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> UpdateBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateBySource(null, update));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new update by source operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table for each record that was matched only in target
		/// using user-defined values for target columns, if it passed filtering by operation predicate and
		/// wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target record.</param>
		/// <param name="update">Update record expression using target record. Expression should be a call to
		/// target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> UpdateBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate,
			Expression<Func<TEntity, TEntity>> update)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.UpdateBySource(predicate, update));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new update by source operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table for each record that was matched only in target
		/// using user-defined values for target columns, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="update">Update record expression using target record. Expression should be a call to
		/// target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> UpdateBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateBySource(null, update));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new update by source operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table for each record that was matched only in target
		/// using user-defined values for target columns, if it passed filtering by operation predicate and
		/// wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target record.</param>
		/// <param name="update">Update record expression using target record. Expression should be a call to
		/// target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> UpdateBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> predicate,
			Expression<Func<TTarget, TTarget>> update)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			if (update == null)
				throw new ArgumentNullException("update");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateBySource(predicate, update));
		}
		#endregion

		#region DeleteBySource
		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new delete by source operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched only in target
		/// and wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> DeleteBySource<TEntity>(this IMergeSource<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.DeleteBySource(null));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new delete by source operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched only in target
		/// and passed filtering with operation predicate, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target record.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TEntity> DeleteBySource<TEntity>(
			this IMergeSource<TEntity> merge,
			Expression<Func<TEntity, bool>> predicate)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			return ((MergeDefinition<TEntity, TEntity>)merge).AddOperation(
				MergeDefinition<TEntity, TEntity>.Operation.DeleteBySource(predicate));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new delete by source operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched only in target
		/// and wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> DeleteBySource<TTarget, TSource>(this IMergeSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.DeleteBySource(null));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new delete by source operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched only in target
		/// and passed filtering with operation predicate, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="predicate">Operation execution condition over target record.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMerge<TTarget, TSource> DeleteBySource<TTarget, TSource>(
			this IMergeSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> predicate)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			if (predicate == null)
				throw new ArgumentNullException("predicate");

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.DeleteBySource(predicate));
		}
		#endregion

		#region Merge
		/// <summary>
		/// Executes merge command and returns total number of target records, affected by merge operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command definition.</param>
		/// <returns>Returns number of target table records, affected by merge comand.</returns>
		public static int Merge<TTarget, TSource>(this IMerge<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			var definition = (MergeDefinition<TTarget, TSource>)merge;


			var dataConnection = definition.Target.DataContextInfo.DataContext as DataConnection;

			if (dataConnection == null)
				throw new ArgumentException("DataContext must be of DataConnection type.");

			return dataConnection.DataProvider.Merge(dataConnection, definition);
		}

		/// <summary>
		/// Executes merge command and returns total number of target records, affected by merge operations.
		/// </summary>
		/// <typeparam name="TEntity">Target and source records type.</typeparam>
		/// <param name="merge">Merge command definition.</param>
		/// <returns>Returns number of target table records, affected by merge comand.</returns>
		public static int Merge<TEntity>(this IMerge<TEntity> merge)
				where TEntity : class
		{
			if (merge == null)
				throw new ArgumentNullException("merge");

			return Merge<TEntity, TEntity>((MergeDefinition<TEntity, TEntity>)merge);
		}
		#endregion
	}

	/// <summary>
	/// Represents merge command source and target configutation without operations with different types for source and
	/// target records, which cannot be executed, because it lacks operations.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeSource<TTarget, TSource>
	{
	}

	/// <summary>
	/// Represents merge command source and target configutation without operations with the same type for source and
	/// target records, which cannot be executed, because it lacks operations.
	/// </summary>
	/// <typeparam name="TEntity">Target and source records type.</typeparam>
	public interface IMergeSource<TEntity>
	{
	}

	/// <summary>
	/// Represents merge command with operations with different types for source and target records, which could be
	/// executed.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMerge<TTarget, TSource> : IMergeSource<TTarget, TSource>
	{
	}

	/// <summary>
	/// Represents merge command with operations with the same type for source and target records, which could be executed.
	/// </summary>
	/// <typeparam name="TEntity">Target and source records type.</typeparam>
	public interface IMerge<TEntity> : IMergeSource<TEntity>
	{
	}
}
