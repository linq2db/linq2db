using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB
{
	using Expressions;
	using JetBrains.Annotations;
	using Linq;
	using LinqToDB.Async;
	using System.Collections.Generic;

	public static partial class LinqExtensions
	{
		#region MethodInfo
		private static readonly MethodInfo _mergeMethodInfo1                          = MemberHelper.MethodOf(() => Merge<int>(null))                                                .GetGenericMethodDefinition();
		private static readonly MethodInfo _mergeMethodInfo2                          = MemberHelper.MethodOf(() => Merge<int>(null, null))                                          .GetGenericMethodDefinition();
		private static readonly MethodInfo _mergeIntoMethodInfo1                      = MemberHelper.MethodOf(() => MergeInto<int, int>(null, null))                                 .GetGenericMethodDefinition();
		private static readonly MethodInfo _mergeIntoMethodInfo2                      = MemberHelper.MethodOf(() => MergeInto<int, int>(null, null, null))                           .GetGenericMethodDefinition();
		private static readonly MethodInfo _usingMethodInfo1                          = MemberHelper.MethodOf(() => Using<int, int>(null, (IQueryable<int>)null))                    .GetGenericMethodDefinition();
		private static readonly MethodInfo _usingMethodInfo2                          = MemberHelper.MethodOf(() => Using<int, int>(null, (IEnumerable<int>)null))                   .GetGenericMethodDefinition();
		private static readonly MethodInfo _usingTargetMethodInfo                     = MemberHelper.MethodOf(() => UsingTarget<int>(null))                                          .GetGenericMethodDefinition();
		private static readonly MethodInfo _onMethodInfo1                             = MemberHelper.MethodOf(() => On<int, int, int>(null, null, null))                             .GetGenericMethodDefinition();
		private static readonly MethodInfo _onMethodInfo2                             = MemberHelper.MethodOf(() => On<int, int>(null, null))                                        .GetGenericMethodDefinition();
		private static readonly MethodInfo _onTargetKeyMethodInfo                     = MemberHelper.MethodOf(() => OnTargetKey<int>(null))                                          .GetGenericMethodDefinition();
		private static readonly MethodInfo _insertWhenNotMatchedMethodInfo1           = MemberHelper.MethodOf(() => InsertWhenNotMatched<int>(null))                                 .GetGenericMethodDefinition();
		private static readonly MethodInfo _insertWhenNotMatchedMethodInfo2           = MemberHelper.MethodOf(() => InsertWhenNotMatched<int, int>(null, null))                      .GetGenericMethodDefinition();
		private static readonly MethodInfo _insertWhenNotMatchedAndMethodInfo1        = MemberHelper.MethodOf(() => InsertWhenNotMatchedAnd<int>(null, null))                        .GetGenericMethodDefinition();
		private static readonly MethodInfo _insertWhenNotMatchedAndMethodInfo2        = MemberHelper.MethodOf(() => InsertWhenNotMatchedAnd<int, int>(null, null, null))             .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedMethodInfo1              = MemberHelper.MethodOf(() => UpdateWhenMatched<int>(null))                                    .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedMethodInfo2              = MemberHelper.MethodOf(() => UpdateWhenMatched<int, int>(null, null))                         .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedAndMethodInfo1           = MemberHelper.MethodOf(() => UpdateWhenMatchedAnd<int>(null, null))                           .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedAndMethodInfo2           = MemberHelper.MethodOf(() => UpdateWhenMatchedAnd<int, int>(null, null, null))                .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedThenDeleteMethodInfo1    = MemberHelper.MethodOf(() => UpdateWhenMatchedThenDelete<int>(null, null))                    .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedThenDeleteMethodInfo2    = MemberHelper.MethodOf(() => UpdateWhenMatchedThenDelete<int, int>(null, null, null))         .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedAndThenDeleteMethodInfo1 = MemberHelper.MethodOf(() => UpdateWhenMatchedAndThenDelete<int>(null, null, null))           .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenMatchedAndThenDeleteMethodInfo2 = MemberHelper.MethodOf(() => UpdateWhenMatchedAndThenDelete<int, int>(null, null, null, null)).GetGenericMethodDefinition();
		private static readonly MethodInfo _deleteWhenMatchedMethodInfo               = MemberHelper.MethodOf(() => DeleteWhenMatched<int, int>(null))                               .GetGenericMethodDefinition();
		private static readonly MethodInfo _deleteWhenMatchedAndMethodInfo            = MemberHelper.MethodOf(() => DeleteWhenMatchedAnd<int, int>(null, null))                      .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenNotMatchedBySourceMethodInfo    = MemberHelper.MethodOf(() => UpdateWhenNotMatchedBySource<int, int>(null, null))              .GetGenericMethodDefinition();
		private static readonly MethodInfo _updateWhenNotMatchedBySourceAndMethodInfo = MemberHelper.MethodOf(() => UpdateWhenNotMatchedBySourceAnd<int, int>(null, null, null))     .GetGenericMethodDefinition();
		private static readonly MethodInfo _deleteWhenNotMatchedBySourceMethodInfo    = MemberHelper.MethodOf(() => DeleteWhenNotMatchedBySource<int, int>(null))                    .GetGenericMethodDefinition();
		private static readonly MethodInfo _deleteWhenNotMatchedBySourceAndMethodInfo = MemberHelper.MethodOf(() => DeleteWhenNotMatchedBySourceAnd<int, int>(null, null))           .GetGenericMethodDefinition();
		private static readonly MethodInfo _executeMergeMethodInfo                    = MemberHelper.MethodOf(() => Merge<int, int>(null))                                           .GetGenericMethodDefinition();
		#endregion

		private class MergeQuery<TTarget, TSource> :
			IMergeableUsing<TTarget>,
			IMergeableOn<TTarget, TSource>,
			IMergeableSource<TTarget, TSource>,
			IMergeable<TTarget, TSource>
		{
			public MergeQuery(IQueryable<TTarget> query)
			{
				Query = query;
			}

			public IQueryable<TTarget> Query { get; }
		}

		#region source/target configuration
		/// <summary>
		/// Starts merge operation definition from target table.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <returns>Returns merge command builder, that contains only target.</returns>
		[Pure, LinqTunnel]
		public static IMergeableUsing<TTarget> Merge<TTarget>(
			[NotNull] this ITable<TTarget> target)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));

			var query = target.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_mergeMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { target.Expression }));

			return new MergeQuery<TTarget, TTarget>(query);
		}

		/// <summary>
		/// Starts merge operation definition from target table.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="hint">Database-specific merge hint.</param>
		/// <returns>Returns merge command builder, that contains only target.</returns>
		[Pure, LinqTunnel]
		public static IMergeableUsing<TTarget> Merge<TTarget>(
			[NotNull]                    this ITable<TTarget> target,
			[NotNull, SqlQueryDependent]      string          hint)
		{
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (hint   == null) throw new ArgumentNullException(nameof(hint));

			var query = target.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_mergeMethodInfo2.MakeGenericMethod(typeof(TTarget)),
					new[] { target.Expression, Expression.Constant(hint) }));

			return new MergeQuery<TTarget, TTarget>(query);
		}

		/// <summary>
		/// Starts merge operation definition from source query.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableOn<TTarget, TSource> MergeInto<TTarget, TSource>(
			[NotNull] this IQueryable<TSource> source,
			[NotNull]      ITable<TTarget>     target)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));

			var query = target.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_mergeIntoMethodInfo1.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { source.Expression, target.Expression }));

			return new MergeQuery<TTarget, TSource>(query);
		}

		/// <summary>
		/// Starts merge operation definition from source query.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="source">Source data query.</param>
		/// <param name="target">Target table.</param>
		/// <param name="hint">Database-specific merge hint.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableOn<TTarget, TSource> MergeInto<TTarget, TSource>(
			[NotNull]                    this IQueryable<TSource> source,
			[NotNull]                         ITable<TTarget>     target,
			[NotNull, SqlQueryDependent]      string              hint)
		{
			if (source == null) throw new ArgumentNullException(nameof(source));
			if (target == null) throw new ArgumentNullException(nameof(target));
			if (hint   == null) throw new ArgumentNullException(nameof(hint));

			var query = target.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_mergeIntoMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { source.Expression, target.Expression, Expression.Constant(hint) }));

			return new MergeQuery<TTarget, TSource>(query);
		}

		/// <summary>
		/// Adds source query to merge command definition.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="source">Source data query.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableOn<TTarget, TSource> Using<TTarget, TSource>(
			[NotNull] this IMergeableUsing<TTarget> merge,
			[NotNull]      IQueryable<TSource>      source)
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_usingMethodInfo1.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, source.Expression }));

			return new MergeQuery<TTarget, TSource>(query);
		}

		/// <summary>
		/// Adds source collection to merge command definition.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="source">Source data collection.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableOn<TTarget, TSource> Using<TTarget, TSource>(
			[NotNull] this IMergeableUsing<TTarget> merge,
			[NotNull]      IEnumerable<TSource>     source)
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (source == null) throw new ArgumentNullException(nameof(source));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_usingMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Constant(source) }));

			return new MergeQuery<TTarget, TSource>(query);
		}

		/// <summary>
		/// Sets target table as merge command source.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <returns>Returns merge command builder with source and target set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableOn<TTarget, TTarget> UsingTarget<TTarget>(
			[NotNull] this IMergeableUsing<TTarget> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_usingTargetMethodInfo.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeableSource<TTarget, TSource> On<TTarget, TSource, TKey>(
			[NotNull]                this IMergeableOn<TTarget, TSource>  merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TKey>> targetKey,
			[NotNull, InstantHandle]      Expression<Func<TSource, TKey>> sourceKey)
		{
			if (merge     == null) throw new ArgumentNullException(nameof(merge));
			if (targetKey == null) throw new ArgumentNullException(nameof(targetKey));
			if (sourceKey == null) throw new ArgumentNullException(nameof(sourceKey));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_onMethodInfo1.MakeGenericMethod(typeof(TTarget), typeof(TSource), typeof(TKey)),
					new[] { mergeQuery.Expression, Expression.Quote(targetKey), Expression.Quote(sourceKey) }));

			return new MergeQuery<TTarget, TSource>(query);
		}

		/// <summary>
		/// Adds definition of matching of target and source records using match condition.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <typeparam name="TSource">Source record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <param name="matchCondition">Rule to match/join target and source records.</param>
		/// <returns>Returns merge command builder with source, target and match (ON) set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableSource<TTarget, TSource> On<TTarget, TSource>(
			[NotNull]                this IMergeableOn<TTarget, TSource>           merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, bool>> matchCondition)
		{
			if (merge          == null) throw new ArgumentNullException(nameof(merge));
			if (matchCondition == null) throw new ArgumentNullException(nameof(matchCondition));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_onMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(matchCondition) }));

			return new MergeQuery<TTarget, TSource>(query);
		}

		/// <summary>
		/// Adds definition of matching of target and source records using primary key columns.
		/// </summary>
		/// <typeparam name="TTarget">Target record type.</typeparam>
		/// <param name="merge">Merge command builder.</param>
		/// <returns>Returns merge command builder with source, target and match (ON) set.</returns>
		[Pure, LinqTunnel]
		public static IMergeableSource<TTarget, TTarget> OnTargetKey<TTarget>(
			[NotNull] this IMergeableOn<TTarget, TTarget> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_onTargetKeyMethodInfo.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TTarget> InsertWhenNotMatched<TTarget>(
			[NotNull] this IMergeableSource<TTarget, TTarget> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_insertWhenNotMatchedMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TTarget> InsertWhenNotMatchedAnd<TTarget>(
			[NotNull]                this IMergeableSource<TTarget, TTarget> merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, bool>>    searchCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_insertWhenNotMatchedAndMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition) }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> InsertWhenNotMatched<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource> merge,
			[NotNull, InstantHandle]      Expression<Func<TSource, TTarget>> setter)
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_insertWhenNotMatchedMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(setter) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> InsertWhenNotMatchedAnd<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource> merge,
			[NotNull, InstantHandle]      Expression<Func<TSource, bool>>    searchCondition,
			[NotNull, InstantHandle]      Expression<Func<TSource, TTarget>> setter)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_insertWhenNotMatchedAndMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition), Expression.Quote(setter) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TTarget> UpdateWhenMatched<TTarget>(
			[NotNull] this IMergeableSource<TTarget, TTarget> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TTarget> UpdateWhenMatchedAnd<TTarget>(
			[NotNull]                this IMergeableSource<TTarget, TTarget>       merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TTarget, bool>> searchCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedAndMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition) }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> UpdateWhenMatched<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource>          merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, TTarget>> setter)
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(setter) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> UpdateWhenMatchedAnd<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource>          merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, bool>>    searchCondition,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, TTarget>> setter)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedAndMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition), Expression.Quote(setter) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TTarget> UpdateWhenMatchedThenDelete<TTarget>(
			[NotNull]                this IMergeableSource<TTarget, TTarget>       merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TTarget, bool>> deleteCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedThenDeleteMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression, Expression.Quote(deleteCondition) }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TTarget> UpdateWhenMatchedAndThenDelete<TTarget>(
			[NotNull]                this IMergeableSource<TTarget, TTarget>       merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TTarget, bool>> searchCondition,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TTarget, bool>> deleteCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			var mergeQuery = ((MergeQuery<TTarget, TTarget>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedAndThenDeleteMethodInfo1.MakeGenericMethod(typeof(TTarget)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition), Expression.Quote(deleteCondition) }));

			return new MergeQuery<TTarget, TTarget>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> UpdateWhenMatchedThenDelete<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource>          merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, TTarget>> setter,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, bool>>    deleteCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedThenDeleteMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(setter), Expression.Quote(deleteCondition) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> UpdateWhenMatchedAndThenDelete<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource>          merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, bool>>    searchCondition,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, TTarget>> setter,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, bool>>    deleteCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));
			if (deleteCondition == null) throw new ArgumentNullException(nameof(deleteCondition));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenMatchedAndThenDeleteMethodInfo2.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition), Expression.Quote(setter), Expression.Quote(deleteCondition) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> DeleteWhenMatched<TTarget, TSource>(
			[NotNull] this IMergeableSource<TTarget, TSource> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_deleteWhenMatchedMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> DeleteWhenMatchedAnd<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource>       merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TSource, bool>> searchCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_deleteWhenMatchedAndMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> UpdateWhenNotMatchedBySource<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource> merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TTarget>> setter)
		{
			if (merge  == null) throw new ArgumentNullException(nameof(merge));
			if (setter == null) throw new ArgumentNullException(nameof(setter));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenNotMatchedBySourceMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(setter) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> UpdateWhenNotMatchedBySourceAnd<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource> merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, bool>>    searchCondition,
			[NotNull, InstantHandle]      Expression<Func<TTarget, TTarget>> setter)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));
			if (setter          == null) throw new ArgumentNullException(nameof(setter));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_updateWhenNotMatchedBySourceAndMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition), Expression.Quote(setter) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> DeleteWhenNotMatchedBySource<TTarget, TSource>(
			[NotNull] this IMergeableSource<TTarget, TSource> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_deleteWhenNotMatchedBySourceMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression }));

			return new MergeQuery<TTarget, TSource>(query);
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
		[Pure, LinqTunnel]
		public static IMergeable<TTarget, TSource> DeleteWhenNotMatchedBySourceAnd<TTarget, TSource>(
			[NotNull]                this IMergeableSource<TTarget, TSource> merge,
			[NotNull, InstantHandle]      Expression<Func<TTarget, bool>>    searchCondition)
		{
			if (merge           == null) throw new ArgumentNullException(nameof(merge));
			if (searchCondition == null) throw new ArgumentNullException(nameof(searchCondition));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;
			var query = mergeQuery.Provider.CreateQuery<TTarget>(
				Expression.Call(
					null,
					_deleteWhenNotMatchedBySourceAndMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					new[] { mergeQuery.Expression, Expression.Quote(searchCondition) }));

			return new MergeQuery<TTarget, TSource>(query);
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
		public static int Merge<TTarget, TSource>(
			[NotNull] this IMergeable<TTarget, TSource> merge)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;

			var currentQuery = ProcessSourceQueryable?.Invoke(mergeQuery) ?? mergeQuery;

			return currentQuery.Provider.Execute<int>(
				Expression.Call(
					null,
					_executeMergeMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
					currentQuery.Expression));
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
			[NotNull] this IMergeable<TTarget, TSource> merge,
			               CancellationToken            token = default)
		{
			if (merge == null) throw new ArgumentNullException(nameof(merge));

			var mergeQuery = ((MergeQuery<TTarget, TSource>)merge).Query;

			var currentQuery = ProcessSourceQueryable?.Invoke(mergeQuery) ?? mergeQuery;

			var expr = Expression.Call(
				null,
				_executeMergeMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
				currentQuery.Expression);

			if (currentQuery is IQueryProviderAsync query)
				return await query.ExecuteAsync<int>(expr, token);

			return await TaskEx.Run(() => currentQuery.Provider.Execute<int>(expr), token);
		}
		#endregion
	}
}
