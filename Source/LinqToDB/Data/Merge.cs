using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Data
{
	/// <summary>
	/// Contains extension methods for merge API.
	/// </summary>
	[PublicAPI]
	public static class MergeExtensions
	{
		#region source/target configuration
		/// <summary>
		/// Starts merge operation definition from target table.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <returns>Returns merge command builder, that contains only target.</returns>
		public static IMergeableUsing<TTarget> Merge<TTarget>(this ITable<TTarget> target)
				where TTarget : class
		{
			if (target == null)
				throw new ArgumentNullException(nameof(target));

			return new MergeDefinition<TTarget, TTarget>(target);
		}

		/// <summary>
		/// Starts merge operation definition from source query.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		public static IMergeableOn<TTarget, TSource> MergeInto<TTarget, TSource>(
			this IQueryable<TSource> source,
			ITable<TTarget> target)
				where TTarget : class
				where TSource : class
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));

			return new MergeDefinition<TTarget, TSource>(target, source);
		}

		/// <summary>
		/// Adds source query to merge command definition.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="source">Source data query.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		public static IMergeableOn<TTarget, TSource> Using<TTarget, TSource>(
			this IMergeableUsing<TTarget> merge,
			IQueryable<TSource> source)
				where TTarget : class
				where TSource : class
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddSource(source);
		}

		/// <summary>
		/// Adds source collection to merge command definition.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="source">Source data collection.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		public static IMergeableOn<TTarget, TSource> Using<TTarget, TSource>(
			this IMergeableUsing<TTarget> merge,
			IEnumerable<TSource> source)
				where TTarget : class
				where TSource : class
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (source == null) throw new ArgumentNullException(nameof(source));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddSource(source);
		}

		/// <summary>
		/// Sets target table as merge command source.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		public static IMergeableOn<TTarget, TTarget> UsingTarget<TTarget>(this IMergeableUsing<TTarget> merge)
			where TTarget : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var builder = (MergeDefinition<TTarget,TTarget>)merge;
			return builder.AddSource(builder.Target);
		}
		#endregion

		#region On predicate
		/// <summary>
		/// Adds definition of matching of target and source records using key value.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <typeparam name="TKey">Source and target records join/match key type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="targetKey">Target record match key definition.</param>
		/// <param name="sourceKey">Source record match key definition.</param>
		/// <returns>Returns merge command builder with source, target and match (ON) set.</returns>
		public static IMergeable<TTarget, TSource> On<TTarget, TSource, TKey>(
			this IMergeableOn<TTarget, TSource> merge,
			Expression<Func<TTarget, TKey>> targetKey,
			Expression<Func<TSource, TKey>> sourceKey)
				where TTarget : class
				where TSource : class
		{
			if (merge     == null) throw new ArgumentNullException(nameof(merge));
			if (targetKey == null) throw new ArgumentNullException(nameof(targetKey));
			if (sourceKey == null) throw new ArgumentNullException(nameof(sourceKey));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOnKey(targetKey, sourceKey);
		}

		/// <summary>
		/// Adds definition of matching of target and source records using match condition.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="matchCondition">Rule to match/join target and source records.</param>
		/// <returns>Returns merge command builder with source, target and match (ON) set.</returns>
		public static IMergeable<TTarget, TSource> On<TTarget, TSource>(
			this IMergeableOn<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> matchCondition)
				where TTarget : class
				where TSource : class
		{
			if (merge          == null) throw new ArgumentNullException(nameof(merge));
			if (matchCondition == null) throw new ArgumentNullException(nameof(matchCondition));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOnPredicate(matchCondition);
		}

		/// <summary>
		/// Adds definition of matching of target and source records using primary key columns.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <returns>Returns merge command builder with source, target and match (ON) set.</returns>
		public static IMergeable<TTarget, TTarget> OnTargetKey<TTarget>(this IMergeableOn<TTarget, TTarget> merge)
				where TTarget : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			return (MergeDefinition<TTarget, TTarget>)merge;
		}
		#endregion

		#region Insert
		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using data from the same fields of source record
		/// for each new record from source, not processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TTarget> InsertWhenNotMatched<TTarget>(this IMergeableSource<TTarget, TTarget> merge)
				where TTarget : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddOperation(
				MergeDefinition<TTarget, TTarget>.Operation.Insert(null, null));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using data from the same fields of source record
		/// for each new record from source that passes filtering with specified predicate, if it wasn't
		/// processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="searchCondition">Operation execution condition over source record.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TTarget> InsertWhenNotMatchedAnd<TTarget>(
			this IMergeableSource<TTarget, TTarget> merge,
			Expression<Func<TTarget, bool>> searchCondition)
				where TTarget : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddOperation(
				MergeDefinition<TTarget, TTarget>.Operation.Insert(searchCondition, null));
		}

		/// <summary>
		/// Adds new insert operation to merge and returns new merge command with added operation.
		/// This operation inserts new record to target table using user-defined values for target columns
		/// for each new record from source, not processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="setter">Create record expression using source record. Expression should be a call to target
		/// record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> InsertWhenNotMatched<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TSource, TTarget>> setter)
				where TTarget : class
				where TSource : class
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Insert(null, setter));
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
		/// <param name="searchCondition">Operation execution condition over source record.</param>
		/// <param name="setter">Create record expression using source record. Expression should be a call to target
		/// record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> InsertWhenNotMatchedAnd<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TSource, bool>> searchCondition,
			Expression<Func<TSource, TTarget>> setter)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Insert(searchCondition, setter));
		}
		#endregion

		#region Update
		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TTarget> UpdateWhenMatched<TTarget>(this IMergeableSource<TTarget, TTarget> merge)
				where TTarget : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddOperation(
				MergeDefinition<TTarget, TTarget>.Operation.Update(null, null));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="searchCondition">Operation execution condition over target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TTarget> UpdateWhenMatchedAnd<TTarget>(
			this IMergeableSource<TTarget, TTarget> merge,
			Expression<Func<TTarget, TTarget, bool>> searchCondition)
				where TTarget : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddOperation(
				MergeDefinition<TTarget, TTarget>.Operation.Update(searchCondition, null));
		}

		/// <summary>
		/// Adds new update operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using user-defined values for target columns
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="setter">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> UpdateWhenMatched<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, TTarget>> setter)
				where TTarget : class
				where TSource : class
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Update(null, setter));
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
		/// <param name="searchCondition">Operation execution condition over target and source records.</param>
		/// <param name="setter">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> UpdateWhenMatchedAnd<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> searchCondition,
			Expression<Func<TTarget, TSource, TTarget>> setter)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Update(searchCondition, setter));
		}
		#endregion

		#region UpdateThenDelete
		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target, if it wasn't processed by previous operations.
		/// After that it removes updated records if they are matched by delete predicate.
		/// </summary>
		/// <typeparam name="TTarget">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="deleteCondition">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TTarget> UpdateWhenMatchedThenDelete<TTarget>(
			this IMergeableSource<TTarget, TTarget> merge,
			Expression<Func<TTarget, TTarget, bool>> deleteCondition)
				where TTarget : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddOperation(
				MergeDefinition<TTarget, TTarget>.Operation.UpdateWithDelete(null, null, deleteCondition));
		}

		/// <summary>
		/// IMPORTANT: This operation supported only by Oracle Database.
		/// Adds new update with delete operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table using data from the same fields of source record
		/// for each record that was matched in source and target and passes filtering with specified predicate,
		/// if it wasn't processed by previous operations.
		/// After that it removes updated records if they are matched by delete predicate.
		/// </summary>
		/// <typeparam name="TTarget">Target and source records type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="searchCondition">Update execution condition over target and source records.</param>
		/// <param name="deleteCondition">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TTarget> UpdateWhenMatchedAndThenDelete<TTarget>(
			this IMergeableSource<TTarget, TTarget> merge,
			Expression<Func<TTarget, TTarget, bool>> searchCondition,
			Expression<Func<TTarget, TTarget, bool>> deleteCondition)
				where TTarget : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			return ((MergeDefinition<TTarget, TTarget>)merge).AddOperation(
				MergeDefinition<TTarget, TTarget>.Operation.UpdateWithDelete(searchCondition, null, deleteCondition));
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
		/// <param name="setter">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <param name="deleteCondition">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> UpdateWhenMatchedThenDelete<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, TTarget>> setter,
			Expression<Func<TTarget, TSource, bool>> deleteCondition)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateWithDelete(null, setter, deleteCondition));
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
		/// <param name="searchCondition">Update execution condition over target and source records.</param>
		/// <param name="setter">Update record expression using target and source records.
		/// Expression should be a call to target record constructor with field/properties initializers to be recognized by API.</param>
		/// <param name="deleteCondition">Delete execution condition over updated target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> UpdateWhenMatchedAndThenDelete<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> searchCondition,
			Expression<Func<TTarget, TSource, TTarget>> setter,
			Expression<Func<TTarget, TSource, bool>> deleteCondition)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateWithDelete(searchCondition, setter, deleteCondition));
		}
		#endregion

		#region Delete
		/// <summary>
		/// Adds new delete operation to merge and returns new merge command with added operation.
		/// This operation removes record in target table for each record that was matched in source and target,
		/// if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> DeleteWhenMatched<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

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
		/// <param name="searchCondition">Operation execution condition over target and source records.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> DeleteWhenMatchedAnd<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TSource, bool>> searchCondition)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.Delete(searchCondition));
		}
		#endregion

		#region UpdateBySource
		/// <summary>
		/// IMPORTANT: This operation supported only by Microsoft SQL Server.
		/// Adds new update by source operation to merge and returns new merge command with added operation.
		/// This operation updates record in target table for each record that was matched only in target
		/// using user-defined values for target columns, if it wasn't processed by previous operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder interface.</param>
		/// <param name="setter">Update record expression using target record. Expression should be a call to
		/// target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> UpdateWhenNotMatchedBySource<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, TTarget>> setter)
				where TTarget : class
				where TSource : class
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateBySource(null, setter));
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
		/// <param name="searchCondition">Operation execution condition over target record.</param>
		/// <param name="setter">Update record expression using target record. Expression should be a call to
		/// target record constructor with field/properties initializers to be recognized by API.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> UpdateWhenNotMatchedBySourceAnd<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> searchCondition,
			Expression<Func<TTarget, TTarget>> setter)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.UpdateBySource(searchCondition, setter));
		}
		#endregion

		#region DeleteBySource
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
		public static IMergeable<TTarget, TSource> DeleteWhenNotMatchedBySource<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge)
				where TTarget : class
				where TSource : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

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
		/// <param name="searchCondition">Operation execution condition over target record.</param>
		/// <returns>Returns new merge command builder with new operation.</returns>
		public static IMergeable<TTarget, TSource> DeleteWhenNotMatchedBySourceAnd<TTarget, TSource>(
			this IMergeableSource<TTarget, TSource> merge,
			Expression<Func<TTarget, bool>> searchCondition)
				where TTarget : class
				where TSource : class
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			return ((MergeDefinition<TTarget, TSource>)merge).AddOperation(
				MergeDefinition<TTarget, TSource>.Operation.DeleteBySource(searchCondition));
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
		public static int Merge<TTarget, TSource>(this IMergeable<TTarget, TSource> merge)
			where TTarget : class
			where TSource : class
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var definition = (MergeDefinition<TTarget, TSource>)merge;

			DataConnection dataConnection;

			switch (definition.Target.DataContext)
			{
				case DataConnection dcon : dataConnection = dcon;                     break;
				case DataContext    dctx : dataConnection = dctx.GetDataConnection(); break;
				default:
					throw new ArgumentException("DataContext must be of DataConnection or DataContext type.");
			}

			return dataConnection.DataProvider.Merge(dataConnection, definition);
		}
		#endregion

		#region MergeAsync
		/// <summary>
		/// Executes merge command and returns total number of target records, affected by merge operations.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command definition.</param>
		/// <param name="token">Asynchronous operation cancellation token.</param>
		/// <returns>Returns number of target table records, affected by merge comand.</returns>
		public static async Task<int> MergeAsync<TTarget, TSource>(
			this IMergeable<TTarget, TSource> merge,
			CancellationToken token = default)
				where TTarget : class
				where TSource : class
		{
			if (merge == null)
				throw new ArgumentNullException(nameof(merge));

			var definition = (MergeDefinition<TTarget, TSource>)merge;

			DataConnection dataConnection;

			switch (definition.Target.DataContext)
			{
				case DataConnection dcon : dataConnection = dcon;                     break;
				case DataContext    dctx : dataConnection = dctx.GetDataConnection(); break;
				default:
					throw new ArgumentException("DataContext must be of DataConnection or DataContext type.");
			}

			return await dataConnection.DataProvider.MergeAsync(dataConnection, definition, token);
		}
		#endregion
	}

	/// <summary>
	/// Merge command builder that have only target table configured.
	/// Only operation available for this type of builder is source configuration.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	public interface IMergeableUsing<TTarget>
	{
	}

	/// <summary>
	/// Merge command builder that have only target table and source configured.
	/// Only operation available for this type of builder is match (ON) condition configuration.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeableOn<TTarget, TSource>
	{
	}

	/// <summary>
	/// Merge command builder that have target table, source and match (ON) condition configured.
	/// You can only add operations to this type of builder.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeableSource<TTarget, TSource>
	{
	}

	/// <summary>
	/// Merge command builder that have target table, source, match (ON) condition and at least one operation configured.
	/// You can add more operations to this type of builder or execute command.
	/// </summary>
	/// <typeparam name="TTarget">Target record type.</typeparam>
	/// <typeparam name="TSource">Source record type.</typeparam>
	public interface IMergeable<TTarget, TSource> : IMergeableSource<TTarget, TSource>
	{
	}
}
