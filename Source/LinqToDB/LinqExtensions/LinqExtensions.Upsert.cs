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
		// Design notes (issue #2558):
		//
		// All chain methods (.Match, .Set, .Ignore, .Insert, .Update, .SkipInsert,
		// .SkipUpdate, .When, .DoNothing) are *marker-only* — direct invocation
		// throws NotSupportedException. They exist solely so C# overload resolution
		// succeeds inside the `configure` expression tree passed to an Upsert entry.
		//
		// The captured Expression<Func<…>> is walked by UpsertBuilder
		// (Source/LinqToDB/Internal/Linq/Builder/UpsertBuilder.cs), which produces
		// either a SqlInsertOrUpdateStatement (native ON CONFLICT path) or a
		// synthesised Merge call chain (MERGE path) depending on the configuration.
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
		/// <remarks>
		/// The <paramref name="configure"/> expression tree composes a small fluent chain. Available knobs:
		/// <list type="bullet">
		///   <item><c>.Match((t, s) =&gt; t.Col == s.Col &amp;&amp; …)</c> — row-matching predicate; defaults to the target table's primary key when omitted.</item>
		///   <item>Root-level <c>.Set(col, value)</c> / <c>.Ignore(col)</c> — apply to both the INSERT and UPDATE branches.</item>
		///   <item><c>.Insert(i =&gt; i.Set(…).Ignore(…).When(s =&gt; pred))</c> — INSERT-branch configuration. <c>.DoNothing()</c> on the branch skips INSERT.</item>
		///   <item><c>.Update(v =&gt; v.Set(…).Ignore(…).When((t, s) =&gt; pred))</c> — UPDATE-branch configuration. <c>.DoNothing()</c> on the branch skips UPDATE.</item>
		///   <item><c>.SkipInsert()</c> — UPDATE-only: matched rows updated, unmatched rows not inserted.</item>
		///   <item><c>.SkipUpdate()</c> — INSERT-only: unmatched rows inserted, matched rows left alone.</item>
		/// </list>
		/// <para>
		/// Columns without a <c>.Set</c> override are written from the matching member on <paramref name="item"/>.
		/// <c>.Ignore(col)</c> drops a column from the branch it's declared in (both branches when declared at the root).
		/// </para>
		/// <para>
		/// <b>Per-provider execution paths.</b> The runtime picks one of three shapes based on the provider's
		/// capabilities and the configured chain:
		/// <list type="bullet">
		///   <item>
		///     <b>Native single-statement upsert</b> — used when <c>.Match</c> lines up with the target's
		///     primary / unique key, no branch uses <c>.DoNothing</c>, no <c>.Insert(i =&gt; i.When(...))</c> is set,
		///     and the provider supports the feature. Emitted as
		///     <c>INSERT ... ON CONFLICT</c> (PostgreSQL 9.5+, SQLite 3.24+),
		///     <c>INSERT ... ON DUPLICATE KEY UPDATE</c> (MySQL / MariaDB),
		///     <c>MERGE</c> (SQL Server 2008+, Oracle 9i+, DB2, Firebird 2+, SAP HANA),
		///     or a provider-specific <c>UPDATE + INSERT</c> emulation (SQL Server 2005, SAP Sybase, MS Access,
		///     Informix, SQL Server Compact). <c>.Update(v =&gt; v.When(...))</c> is honored natively on
		///     SQLite, PostgreSQL, SQL Server 2008+, Oracle, DB2, and Firebird 2+; on the other listed engines
		///     the predicate forces a fallback (see below).
		///   </item>
		///   <item>
		///     <b>Synthesised MERGE lowering</b> — used for bulk <c>IEnumerable&lt;TSource&gt;</c> /
		///     <c>IQueryable&lt;TSource&gt;</c> sources, or when <c>.SkipInsert</c>, <c>.Insert(i =&gt; i.When(...))</c>,
		///     or a non-PK <c>.Match</c> is configured. Requires provider-level <c>MERGE</c> support
		///     (<see cref="Internal.SqlProvider.SqlProviderFlags.IsUpsertWithMergeLoweringSupported"/>).
		///     Not supported on SAP HANA — throws <see cref="LinqToDBException"/> at build time on that engine.
		///   </item>
		///   <item>
		///     <b>3-query <c>SELECT → UPDATE → INSERT</c> fallback</b> — used when
		///     <c>.Update(v =&gt; v.When(...))</c> is configured against a provider whose native single-statement
		///     shape cannot carry an UPDATE-branch predicate (MySQL / MariaDB, SAP Sybase, SQL Server 2005,
		///     MS Access, Informix, SQL Server Compact). The three statements run as independent commands;
		///     callers that need atomicity under concurrent writers must wrap the call in their own
		///     transaction. Set <see cref="LinqOptions.ThrowOnUpsertEmulation"/> to <see langword="true"/>
		///     to reject this fallback with <see cref="LinqToDBException"/> instead of silently emulating.
		///   </item>
		/// </list>
		/// </para>
		/// </remarks>
		/// <example>
		/// Insert-or-update with per-branch audit columns:
		/// <code>
		/// db.Users.Upsert(user, u =&gt; u
		///     .Match((t, s) =&gt; t.Id == s.Id)
		///     .Insert(i =&gt; i
		///         .Set(x =&gt; x.CreatedAt, () =&gt; DateTime.UtcNow)
		///         .Set(x =&gt; x.CreatedBy, _ =&gt; currentUser)
		///         .Ignore(x =&gt; x.UpdatedAt)
		///         .Ignore(x =&gt; x.UpdatedBy))
		///     .Update(v =&gt; v
		///         .Set(x =&gt; x.UpdatedAt, () =&gt; DateTime.UtcNow)
		///         .Set(x =&gt; x.UpdatedBy, _ =&gt; currentUser)
		///         .Ignore(x =&gt; x.CreatedAt)
		///         .Ignore(x =&gt; x.CreatedBy)));
		/// </code>
		/// Conditional update (MVCC-style version check):
		/// <code>
		/// db.Users.Upsert(user, u =&gt; u
		///     .Match((t, s) =&gt; t.Id == s.Id)
		///     .Update(v =&gt; v.When((t, s) =&gt; s.Version &gt; t.Version)));
		/// </code>
		/// </example>
		/// <typeparam name="T">Target table record type.</typeparam>
		/// <param name="target">Target table.</param>
		/// <param name="item">Entity to upsert.</param>
		/// <param name="configure">Fluent configuration expression.</param>
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
		/// <remarks>
		/// See <see cref="Upsert{T}(ITable{T}, T, Expression{Func{IUpsertable{T, T}, IUpsertable{T, T}}})"/>
		/// for the configure-chain reference and the per-provider execution-path matrix.
		/// </remarks>
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
