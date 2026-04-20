using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

using LinqToDB.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		#region Upsert

		// ---------------------------------------------------------------------
		// Design notes — see issue #2558 and the plan under
		// ~/.claude/plans/i-would-like-to-linked-nest.md.
		//
		// All chain methods (.Match, .Set, .Ignore, .Insert, .Update, .SkipInsert,
		// .SkipUpdate, .When, .DoNothing) are *marker-only*: they throw if invoked
		// at runtime. They exist solely to provide C# overload resolution inside
		// the `configure` expression tree passed to an Upsert entry-method.
		//
		// The whole configure argument is captured as an Expression<Func<…>> and
		// walked by UpsertBuilder (Source/LinqToDB/Internal/Linq/Builder/UpsertBuilder.cs),
		// which turns it into a SqlInsertOrUpdateStatement.
		// ---------------------------------------------------------------------

		static readonly MethodInfo _upsertItemMethodInfo =
			MemberHelper.MethodOf(() => Upsert<int>(null!, default!, null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Performs an Upsert (insert-or-update) of a single entity into the target table.
		/// Equivalent to <c>Upsert(target, item, u =&gt; u)</c>: all mapped target columns are written from <paramref name="item"/>;
		/// the match condition is the target table's primary key.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to upsert.</param>
		/// <returns>Number of affected records.</returns>
		public static int Upsert<T>(this ITable<T> target, T item)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);

			return Upsert(target, item, UpsertIdentity<T>.Instance);
		}

		/// <summary>
		/// Performs an Upsert (insert-or-update) of a single entity into the target table, configured by a fluent builder.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to upsert.</param>
		/// <param name="configure">Fluent configuration expression: match condition, per-branch <c>Set</c> / <c>Ignore</c>, etc.</param>
		/// <returns>Number of affected records.</returns>
		public static int Upsert<T>(
			                this ITable<T>                                                         target,
			                T                                                                      item,
			[InstantHandle] Expression<Func<IUpsertable<T, T>, IUpsertable<T, T>>>                 configure)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_upsertItemMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Constant(item, typeof(T)),
				Expression.Quote(configure));

			return currentSource.Execute<int>(expr);
		}

		static readonly MethodInfo _upsertItemAsyncMethodInfo =
			MemberHelper.MethodOf(() => UpsertAsync<int>(null!, default!, null!, default)).GetGenericMethodDefinition();

		/// <summary>
		/// Asynchronously performs an Upsert (insert-or-update) of a single entity into the target table.
		/// Equivalent to <c>UpsertAsync(target, item, u =&gt; u)</c>.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to upsert.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task yielding the number of affected records.</returns>
		public static Task<int> UpsertAsync<T>(this ITable<T> target, T item, CancellationToken token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);

			return UpsertAsync(target, item, UpsertIdentity<T>.Instance, token);
		}

		/// <summary>
		/// Asynchronously performs an Upsert (insert-or-update) of a single entity into the target table, configured by a fluent builder.
		/// </summary>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to upsert.</param>
		/// <param name="configure">Fluent configuration expression.</param>
		/// <param name="token">Optional asynchronous operation cancellation token.</param>
		/// <returns>Task yielding the number of affected records.</returns>
		public static Task<int> UpsertAsync<T>(
			                this ITable<T>                                                         target,
			                T                                                                      item,
			[InstantHandle] Expression<Func<IUpsertable<T, T>, IUpsertable<T, T>>>                 configure,
			                CancellationToken                                                      token = default)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(item);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_upsertItemAsyncMethodInfo.MakeGenericMethod(typeof(T)),
				currentSource.Expression,
				Expression.Constant(item, typeof(T)),
				Expression.Quote(configure),
				Expression.Constant(token, typeof(CancellationToken)));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		// ---------------------------------------------------------------------
		// Entry points — IEnumerable<TSource> bulk source (Phase 4)
		// Signatures exist so the public API surface is complete; the builder
		// rejects them in Phase 1 with a clear LinqToDBException.
		// ---------------------------------------------------------------------

		static readonly MethodInfo _upsertEnumerableMethodInfo =
			MemberHelper.MethodOf(() => Upsert<int, int>(null!, (IEnumerable<int>)null!, null!)).GetGenericMethodDefinition();

		/// <summary>
		/// Performs an Upsert of every element in <paramref name="items"/> into the target table, configured by a fluent builder.
		/// </summary>
		public static int Upsert<TTarget, TSource>(
			                this ITable<TTarget>                                                                  target,
			                IEnumerable<TSource>                                                                  items,
			[InstantHandle] Expression<Func<IUpsertable<TTarget, TSource>, IUpsertable<TTarget, TSource>>>         configure)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(items);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_upsertEnumerableMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
				currentSource.Expression,
				Expression.Constant(items, typeof(IEnumerable<TSource>)),
				Expression.Quote(configure));

			return currentSource.Execute<int>(expr);
		}

		static readonly MethodInfo _upsertEnumerableAsyncMethodInfo =
			MemberHelper.MethodOf(() => UpsertAsync<int, int>(null!, (IEnumerable<int>)null!, null!, default)).GetGenericMethodDefinition();

		/// <summary>Asynchronously performs an Upsert of every element in <paramref name="items"/> into the target table.</summary>
		public static Task<int> UpsertAsync<TTarget, TSource>(
			                this ITable<TTarget>                                                                  target,
			                IEnumerable<TSource>                                                                  items,
			[InstantHandle] Expression<Func<IUpsertable<TTarget, TSource>, IUpsertable<TTarget, TSource>>>         configure,
			                CancellationToken                                                                     token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(items);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();

			var expr = Expression.Call(
				null,
				_upsertEnumerableAsyncMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
				currentSource.Expression,
				Expression.Constant(items, typeof(IEnumerable<TSource>)),
				Expression.Quote(configure),
				Expression.Constant(token, typeof(CancellationToken)));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		// ---------------------------------------------------------------------
		// Entry points — IQueryable<TSource> server-side source (Phase 4)
		// ---------------------------------------------------------------------

		static readonly MethodInfo _upsertQueryableMethodInfo =
			MemberHelper.MethodOf(() => Upsert<int, int>(null!, (IQueryable<int>)null!, null!)).GetGenericMethodDefinition();

		/// <summary>Performs an Upsert of every row produced by <paramref name="source"/> into the target table, configured by a fluent builder.</summary>
		public static int Upsert<TTarget, TSource>(
			                this ITable<TTarget>                                                                  target,
			                IQueryable<TSource>                                                                   source,
			[InstantHandle] Expression<Func<IUpsertable<TTarget, TSource>, IUpsertable<TTarget, TSource>>>         configure)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();
			var queryableSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_upsertQueryableMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
				currentSource.Expression,
				queryableSource.Expression,
				Expression.Quote(configure));

			return currentSource.Execute<int>(expr);
		}

		static readonly MethodInfo _upsertQueryableAsyncMethodInfo =
			MemberHelper.MethodOf(() => UpsertAsync<int, int>(null!, (IQueryable<int>)null!, null!, default)).GetGenericMethodDefinition();

		/// <summary>Asynchronously performs an Upsert of every row produced by <paramref name="source"/> into the target table.</summary>
		public static Task<int> UpsertAsync<TTarget, TSource>(
			                this ITable<TTarget>                                                                  target,
			                IQueryable<TSource>                                                                   source,
			[InstantHandle] Expression<Func<IUpsertable<TTarget, TSource>, IUpsertable<TTarget, TSource>>>         configure,
			                CancellationToken                                                                     token = default)
			where TTarget : notnull
		{
			ArgumentNullException.ThrowIfNull(target);
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(configure);

			var currentSource = target.GetLinqToDBSource();
			var queryableSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				_upsertQueryableAsyncMethodInfo.MakeGenericMethod(typeof(TTarget), typeof(TSource)),
				currentSource.Expression,
				queryableSource.Expression,
				Expression.Quote(configure),
				Expression.Constant(token, typeof(CancellationToken)));

			return currentSource.ExecuteAsync<int>(expr, token);
		}

		/// <summary>Mirror overload with the source as receiver and target as argument.</summary>
		public static int Upsert<TTarget, TSource>(
			                this IQueryable<TSource>                                                              source,
			                ITable<TTarget>                                                                       target,
			[InstantHandle] Expression<Func<IUpsertable<TTarget, TSource>, IUpsertable<TTarget, TSource>>>         configure)
			where TTarget : notnull
			=> Upsert(target, source, configure);

		/// <summary>Asynchronous mirror overload with the source as receiver and target as argument.</summary>
		public static Task<int> UpsertAsync<TTarget, TSource>(
			                this IQueryable<TSource>                                                              source,
			                ITable<TTarget>                                                                       target,
			[InstantHandle] Expression<Func<IUpsertable<TTarget, TSource>, IUpsertable<TTarget, TSource>>>         configure,
			                CancellationToken                                                                     token = default)
			where TTarget : notnull
			=> UpsertAsync(target, source, configure, token);

		// ---------------------------------------------------------------------
		// Chain methods — markers only. Throw at runtime; present only so that
		// the C# compiler resolves them inside the `configure` expression tree.
		// ---------------------------------------------------------------------

		const string UpsertChainMarkerMessage =
			"Upsert builder methods are intended for use inside an Expression<Func<IUpsertable<T,S>, …>> " +
			"passed to Upsert / UpsertAsync; direct invocation is not supported.";

		/// <summary>
		/// Defines the match condition used to decide between INSERT and UPDATE.
		/// When omitted, the target table's primary key is used (same as today's <c>InsertOrUpdate</c>).
		/// </summary>
		public static IUpsertable<TTarget, TSource> Match<TTarget, TSource>(
			                this IUpsertable<TTarget, TSource>            upsertable,
			[InstantHandle] Expression<Func<TTarget, TSource, bool>>      matchCondition)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value for <b>both</b> INSERT and UPDATE branches from a context-free expression.</summary>
		public static IUpsertable<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertable<TTarget, TSource>        upsertable,
			[InstantHandle] Expression<Func<TTarget, TV>>             field,
			[InstantHandle] Expression<Func<TV>>                      value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value for <b>both</b> INSERT and UPDATE branches from the source row.</summary>
		public static IUpsertable<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertable<TTarget, TSource>        upsertable,
			[InstantHandle] Expression<Func<TTarget, TV>>             field,
			[InstantHandle] Expression<Func<TSource, TV>>             value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Excludes a target column from <b>both</b> INSERT and UPDATE.</summary>
		public static IUpsertable<TTarget, TSource> Ignore<TTarget, TSource, TV>(
			                this IUpsertable<TTarget, TSource>     upsertable,
			[InstantHandle] Expression<Func<TTarget, TV>>          field)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Skips the INSERT branch entirely — UPDATE-IF-EXISTS semantics.</summary>
		public static IUpsertable<TTarget, TSource> SkipInsert<TTarget, TSource>(
			this IUpsertable<TTarget, TSource> upsertable)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Skips the UPDATE branch entirely — INSERT-IF-NOT-EXISTS semantics.</summary>
		public static IUpsertable<TTarget, TSource> SkipUpdate<TTarget, TSource>(
			this IUpsertable<TTarget, TSource> upsertable)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Configures the INSERT branch.</summary>
		public static IUpsertable<TTarget, TSource> Insert<TTarget, TSource>(
			                this IUpsertable<TTarget, TSource>                                                                                    upsertable,
			[InstantHandle] Expression<Func<IUpsertInsertBuilder<TTarget, TSource>, IUpsertInsertBuilder<TTarget, TSource>>>                       configure)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Configures the UPDATE branch.</summary>
		public static IUpsertable<TTarget, TSource> Update<TTarget, TSource>(
			                this IUpsertable<TTarget, TSource>                                                                                    upsertable,
			[InstantHandle] Expression<Func<IUpsertUpdateBuilder<TTarget, TSource>, IUpsertUpdateBuilder<TTarget, TSource>>>                       configure)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		// --- INSERT branch markers ---

		/// <summary>Adds a source-row predicate: insert only when the predicate holds (<c>WHEN NOT MATCHED AND …</c>).</summary>
		public static IUpsertInsertBuilder<TTarget, TSource> When<TTarget, TSource>(
			                this IUpsertInsertBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TSource, bool>>             condition)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Marks the INSERT branch as explicitly empty.</summary>
		public static IUpsertInsertBuilder<TTarget, TSource> DoNothing<TTarget, TSource>(
			this IUpsertInsertBuilder<TTarget, TSource> builder)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value during INSERT from a context-free expression.</summary>
		public static IUpsertInsertBuilder<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertInsertBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field,
			[InstantHandle] Expression<Func<TV>>                        value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value during INSERT from the source row.</summary>
		public static IUpsertInsertBuilder<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertInsertBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field,
			[InstantHandle] Expression<Func<TSource, TV>>               value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Excludes a target column from the INSERT statement.</summary>
		public static IUpsertInsertBuilder<TTarget, TSource> Ignore<TTarget, TSource, TV>(
			                this IUpsertInsertBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		// --- UPDATE branch markers ---

		/// <summary>Adds a target/source-row predicate: update only when the predicate holds (<c>WHEN MATCHED AND …</c>).</summary>
		public static IUpsertUpdateBuilder<TTarget, TSource> When<TTarget, TSource>(
			                this IUpsertUpdateBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TSource, bool>>    condition)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Marks the UPDATE branch as explicitly empty.</summary>
		public static IUpsertUpdateBuilder<TTarget, TSource> DoNothing<TTarget, TSource>(
			this IUpsertUpdateBuilder<TTarget, TSource> builder)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value during UPDATE from a context-free expression.</summary>
		public static IUpsertUpdateBuilder<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertUpdateBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field,
			[InstantHandle] Expression<Func<TV>>                        value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value during UPDATE from the source row.</summary>
		public static IUpsertUpdateBuilder<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertUpdateBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field,
			[InstantHandle] Expression<Func<TSource, TV>>               value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Sets a target column's value during UPDATE from both the current target row and the source row.</summary>
		public static IUpsertUpdateBuilder<TTarget, TSource> Set<TTarget, TSource, TV>(
			                this IUpsertUpdateBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field,
			[InstantHandle] Expression<Func<TTarget, TSource, TV>>      value)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		/// <summary>Excludes a target column from the UPDATE statement.</summary>
		public static IUpsertUpdateBuilder<TTarget, TSource> Ignore<TTarget, TSource, TV>(
			                this IUpsertUpdateBuilder<TTarget, TSource> builder,
			[InstantHandle] Expression<Func<TTarget, TV>>               field)
			where TTarget : notnull
			=> throw new NotSupportedException(UpsertChainMarkerMessage);

		#endregion

		// ---------------------------------------------------------------------
		// Cached identity lambda for the bare Upsert(target, item) overload.
		// ---------------------------------------------------------------------

		static class UpsertIdentity<T>
			where T : notnull
		{
			public static readonly Expression<Func<IUpsertable<T, T>, IUpsertable<T, T>>> Instance = u => u;
		}

		// ---------------------------------------------------------------------
		// Internal concrete builder type. Instances are not constructed by the
		// current Phase-1 entry methods (which walk the configure Expression
		// tree directly), but the type is kept for later phases: the internal
		// translator / Merge-reuse paths may need to hand out or accept an
		// IUpsertable<TTarget,TSource> reference tied to a concrete class.
		// Kept intentionally minimal — all mutable state the old Phase-0 builder
		// held has moved into UpsertBuilder's expression-tree walk.
		// ---------------------------------------------------------------------

		internal sealed class Upsertable<TTarget, TSource> :
			IUpsertable<TTarget, TSource>,
			IUpsertInsertBuilder<TTarget, TSource>,
			IUpsertUpdateBuilder<TTarget, TSource>
			where TTarget : notnull
		{
		}
	}
}
