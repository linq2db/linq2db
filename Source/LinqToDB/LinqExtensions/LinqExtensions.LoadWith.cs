using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;

using JetBrains.Annotations;

using LinqToDB.Internal.Linq;

namespace LinqToDB
{
	public partial class LinqExtensions
	{
		/// <summary>
		/// Specifies associations, that should be loaded for each loaded record from current table.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// Some usage examples:
		/// <code>
		/// // loads records from Table1 with Reference association loaded for each Table1 record
		/// db.Table1.LoadWithAsTable(r => r.Reference);
		///
		/// // loads records from Table1 with Reference1 association loaded for each Table1 record
		/// // loads records from Reference2 association for each loaded Reference1 record
		/// db.Table1.LoadWithAsTable(r => r.Reference1.Reference2);
		///
		/// // loads records from Table1 with References collection association loaded for each Table1 record
		/// db.Table1.LoadWithAsTable(r => r.References);
		///
		/// // loads records from Table1 with Reference1 collection association loaded for each Table1 record
		/// // loads records from Reference2 collection association for each loaded Reference1 record
		/// // loads records from Reference3 association for each loaded Reference2 record
		/// // note that a way you access collection association record (by index, using First() method) doesn't affect
		/// // query results and always select all records
		/// db.Table1.LoadWithAsTable(r => r.References1[0].References2.First().Reference3);
		/// </code>
		/// </summary>
		/// <typeparam name="T">Table record mapping class.</typeparam>
		/// <param name="table">Table-like query source.</param>
		/// <param name="selector">Association selection expression.</param>
		/// <returns>Table-like query source.</returns>
		[LinqTunnel]
		[Pure]
		public static ITable<T> LoadWithAsTable<T>(
			                this ITable<T> table,
			[InstantHandle] Expression<Func<T,object?>> selector)
			where T : notnull
		{
			ArgumentNullException.ThrowIfNull(table);

			var newTable = new Table<T>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(LoadWithAsTable, table, selector),
					new[] { table.Expression, Expression.Quote(selector) })
			);

			return newTable;
		}

		abstract class LoadWithQueryableBase<TEntity>(IExpressionQuery<TEntity> query) : IExpressionQuery
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public IExpressionQuery<TEntity> Query { get; } = query;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			Expression IExpressionQuery.Expression                                   => Query.Expression;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			IDataContext IExpressionQuery.DataContext                                  => ((IExpressionQuery)Query.GetLinqToDBSource()).DataContext;

			public abstract QueryDebugView DebugView { get; }

			IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => ((IExpressionQuery)Query.GetLinqToDBSource()).GetSqlQueries(options);
		}

		sealed class LoadWithQueryable<TEntity, TProperty>(IExpressionQuery<TEntity> query) : LoadWithQueryableBase<TEntity>(query), ILoadWithQueryable<TEntity, TProperty>, IAsyncEnumerable<TEntity>
		{
			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			Type IQueryable.ElementType => Query.ElementType;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			IQueryProvider IQueryable.Provider => Query.Provider;

			[DebuggerBrowsable(DebuggerBrowsableState.Never)]
			public IQueryProvider Provider => Query.Provider;

			public Expression Expression => Query.Expression;

			public override QueryDebugView DebugView => Query.DebugView;

			IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetAsyncEnumerator(CancellationToken cancellationToken) =>
				((IAsyncEnumerable<TEntity>)Query).GetAsyncEnumerator(cancellationToken);

			IEnumerator<TEntity> IEnumerable<TEntity>.GetEnumerator() => Query.GetEnumerator();

			IEnumerator IEnumerable.GetEnumerator() => Query.GetEnumerator();
		}

		/// <summary>
		/// Specifies associations that should be loaded for each loaded record from current table.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following query loads records from Table1 with Reference association, loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference);
		///         </code>
		///     </para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References collection association loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.References);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References collection association loaded for each Table1 record. Also it limits loaded records.
		///         <code>
		///             db.Table1.LoadWith(r => r.References.Where(e => !e.IsDeleted).Take(10));
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with:
		///         - Reference1 collection association loaded for each Table1 record;
		///         - Reference2 collection association for each loaded Reference1 record;
		///         - Reference3 association for each loaded Reference2 record.
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity,TProperty> LoadWith<TEntity,TProperty>(
			this            IQueryable<TEntity>                  source,
			[InstantHandle] Expression<Func<TEntity,TProperty?>> selector)
		where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(LoadWith, source, selector),
				currentSource.Expression,
				Expression.Quote(selector));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for each loaded record from current table.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// <paramref name="loadFunc"/> parameter could be used to define additional association loading logic like filters or loading of more associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following query loads records from Table1 with Reference association, loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference);
		///         </code>
		///     </para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References collection association loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.References);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with:
		///         - Reference1 collection association loaded for each Table1 record;
		///         - Reference2 collection association for each loaded Reference1 record;
		///         - Reference3 association for each loaded Reference2 record.
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References collection association loaded for each Table1 record, where References record
		///         contains only records without "exclude" text in Name property.
		///         <code>
		///             db.Table1.LoadWith(r => r.References, r => r.Where(rr => !rr.Name.Contains("exclude")));
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References1 collection association loaded for each Table1 record, where References1 record
		///         also load Reference2 association.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1, r => r.LoadWith(rr => rr.Reference2));
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <param name="loadFunc">Defines additional logic for association load query.</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity,TProperty> LoadWith<TEntity,TProperty>(
			this            IQueryable<TEntity>                                           source,
			[InstantHandle] Expression<Func<TEntity,IEnumerable<TProperty>?>>             selector,
			[InstantHandle] Expression<Func<IQueryable<TProperty>,IQueryable<TProperty>>> loadFunc)
		where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(LoadWith, source, selector, loadFunc),
				new[] { currentSource.Expression, Expression.Quote(selector), Expression.Quote(loadFunc) });

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity, TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for each loaded record from current table.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// <paramref name="loadFunc"/> parameter could be used to define additional association loading logic like filters or loading of more associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following query loads records from Table1 with Reference association, loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference);
		///         </code>
		///     </para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References collection association loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.References);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with:
		///         - Reference1 collection association loaded for each Table1 record;
		///         - Reference2 collection association for each loaded Reference1 record;
		///         - Reference3 association for each loaded Reference2 record.
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References collection association loaded for each Table1 record, where References record
		///         contains only records without "exclude" text in Name property.
		///         <code>
		///             db.Table1.LoadWith(r => r.References, r => r.Where(rr => !rr.Name.Contains("exclude")));
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References1 collection association loaded for each Table1 record, where References1 record
		///         also load Reference2 association.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1, r => r.LoadWith(rr => rr.Reference2));
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <param name="loadFunc">Defines additional logic for association load query.</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity,TProperty> LoadWith<TEntity,TProperty>(
			this            IQueryable<TEntity>                                           source,
			[InstantHandle] Expression<Func<TEntity,TProperty?>>                          selector,
			[InstantHandle] Expression<Func<IQueryable<TProperty>,IQueryable<TProperty>>> loadFunc)
		where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(LoadWith, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for parent association, loaded by previous LoadWith/ThenLoad call in chain.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///     <para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TPreviousProperty">Type of parent association.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity,TProperty> ThenLoad<TEntity,TPreviousProperty,TProperty>(
			this            ILoadWithQueryable<TEntity,TPreviousProperty>  source,
			[InstantHandle] Expression<Func<TPreviousProperty,TProperty?>> selector)
		where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector),
				currentSource.Expression,
				Expression.Quote(selector));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for parent association, loaded by previous LoadWith/ThenLoad call in chain.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///     <para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TPreviousProperty">Type of parent association.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity,TProperty> ThenLoad<TEntity,TPreviousProperty,TProperty>(
			this            ILoadWithQueryable<TEntity,IEnumerable<TPreviousProperty>> source,
			[InstantHandle] Expression<Func<TPreviousProperty,TProperty?>>             selector)
		where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector),
				new[] { currentSource.Expression, Expression.Quote(selector) });

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for parent association, loaded by previous LoadWith/ThenLoad call in chain.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// <paramref name="loadFunc"/> parameter could be used to define additional association loading logic like filters or loading of more associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///     <para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References1 collection association loaded for each Table1 record with
		///         References2 collection association loaded for each record in References1, with filter over References2 record
		///         to include only records without "exclude" text in Name property.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r1 => r1.References2, r2 => r2.Where(rr2 => !rr2.Name.Contains("exclude")));
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TPreviousProperty">Type of parent association.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <param name="loadFunc">Defines additional logic for association load query.</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure] // ThenLoadFromSingleManyFilter
		public static ILoadWithQueryable<TEntity,TProperty> ThenLoad<TEntity,TPreviousProperty,TProperty>(
			this            ILoadWithQueryable<TEntity,TPreviousProperty>                 source,
			[InstantHandle] Expression<Func<TPreviousProperty,IEnumerable<TProperty>?>>   selector,
			[InstantHandle] Expression<Func<IQueryable<TProperty>,IQueryable<TProperty>>> loadFunc)
			where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				new[] { currentSource.Expression, Expression.Quote(selector), Expression.Quote(loadFunc) });

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for parent association, loaded by previous LoadWith/ThenLoad call in chain.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// <paramref name="loadFunc"/> parameter could be used to define additional association loading logic like filters or loading of more associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///     <para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References1 collection association loaded for each Table1 record with
		///         References2 collection association loaded for each record in References1, with filter over References2 record
		///         to include only records without "exclude" text in Name property.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r1 => r1.References2, r2 => r2.Where(rr2 => !rr2.Name.Contains("exclude")));
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TPreviousProperty">Type of parent association.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <param name="loadFunc">Defines additional logic for association load query.</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure] // Methods.LinqToDB.ThenLoadFromSingleSingleFilter
		public static ILoadWithQueryable<TEntity,TProperty> ThenLoad<TEntity,TPreviousProperty,TProperty>(
			this            ILoadWithQueryable<TEntity,TPreviousProperty>                 source,
			[InstantHandle] Expression<Func<TPreviousProperty,TProperty?>>                selector,
			[InstantHandle] Expression<Func<IQueryable<TProperty>,IQueryable<TProperty>>> loadFunc)
			where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for parent association, loaded by previous LoadWith/ThenLoad call in chain.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// <paramref name="loadFunc"/> parameter could be used to define additional association loading logic like filters or loading of more associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///     <para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References1 collection association loaded for each Table1 record with
		///         References2 collection association loaded for each record in References1, with filter over References2 record
		///         to include only records without "exclude" text in Name property.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r1 => r1.References2, r2 => r2.Where(rr2 => !rr2.Name.Contains("exclude")));
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TPreviousProperty">Type of parent association.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <param name="loadFunc">Defines additional logic for association load query.</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure] // // Methods.LinqToDB.ThenLoadFromManySingleFilter
		public static ILoadWithQueryable<TEntity,TProperty> ThenLoad<TEntity,TPreviousProperty,TProperty>(
			this            ILoadWithQueryable<TEntity,IEnumerable<TPreviousProperty>>    source,
			[InstantHandle] Expression<Func<TPreviousProperty,TProperty?>>                selector,
			[InstantHandle] Expression<Func<IQueryable<TProperty>,IQueryable<TProperty>>> loadFunc)
			where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>((IExpressionQuery<TEntity>)result);
		}

		/// <summary>
		/// Specifies associations that should be loaded for parent association, loaded by previous LoadWith/ThenLoad call in chain.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// <paramref name="loadFunc"/> parameter could be used to define additional association loading logic like filters or loading of more associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         Following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
		///     </para>
		///     <para>
		///         <code>
		///             db.Table1.LoadWith(r => r.References1[0].References2.First().Reference3);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r => r.References2).ThenLoad(r => r.Reference3);
		///         </code>
		///     </para>
		///     <para>
		///         Following query loads records from Table1 with References1 collection association loaded for each Table1 record with
		///         References2 collection association loaded for each record in References1, with filter over References2 record
		///         to include only records without "exclude" text in Name property.
		///         <code>
		///             db.Table1.LoadWith(r => r.References1).ThenLoad(r1 => r1.References2, r2 => r2.Where(rr2 => !rr2.Name.Contains("exclude")));
		///         </code>
		///     </para>
		/// </para>
		/// </example>
		/// <typeparam name="TEntity">Type of entity being queried.</typeparam>
		/// <typeparam name="TPreviousProperty">Type of parent association.</typeparam>
		/// <typeparam name="TProperty">Type of the related entity to be included.</typeparam>
		/// <param name="source">The source query.</param>
		/// <param name="selector">A lambda expression representing navigation property to be included (<c>t => t.Property1</c>).</param>
		/// <param name="loadFunc">Defines additional logic for association load query.</param>
		/// <returns>Returns new query with related data included.</returns>
		[LinqTunnel]
		[Pure] // Methods.LinqToDB.ThenLoadFromManyManyFilter
		public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
			this ILoadWithQueryable<TEntity, IEnumerable<TPreviousProperty>> source,
			[InstantHandle] Expression<Func<TPreviousProperty, IEnumerable<TProperty>>>    selector,
			[InstantHandle] Expression<Func<IQueryable<TProperty>, IQueryable<TProperty>>> loadFunc)
		where TEntity : class
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(selector);

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity, TProperty>((IExpressionQuery<TEntity>)result);
		}

		#region Eager-loading strategy marker methods

		/// <summary>
		/// Combines this collection sub-query with other same-level eager loads into a single
		/// <c>CTE + UNION ALL</c> query, reducing the total number of round-trips to the database.
		/// <para>
		/// Works both inside a <c>Select</c> projection and directly in a <see cref="LoadWith{TEntity,TProperty}(IQueryable{TEntity}, System.Linq.Expressions.Expression{System.Func{TEntity,TProperty}})"/> selector.
		/// Requires the target database to support Common Table Expressions; falls back to
		/// <see cref="AsKeyedQuery{T}(IQueryable{T})"/> when CTEs are unavailable.
		/// </para>
		/// </summary>
		/// <example>
		/// <code>
		/// // Select projection
		/// from o in db.Orders
		/// select new { Items = o.OrderItems.AsUnionQuery().ToList() }
		///
		/// // LoadWith selector
		/// db.Orders.LoadWith(o => o.OrderItems.AsUnionQuery()).ToList()
		/// </code>
		/// </example>
		/// <typeparam name="T">Element type of the collection.</typeparam>
		/// <param name="source">The collection sub-query to mark.</param>
		/// <returns><paramref name="source"/> unchanged at runtime (translation-time marker only).</returns>
		[Pure]
		public static IEnumerable<T> AsUnionQuery<T>(this IEnumerable<T> source) => source;

		/// <inheritdoc cref="AsUnionQuery{T}(IEnumerable{T})"/>
		[LinqTunnel]
		[Pure]
		public static IQueryable<T> AsUnionQuery<T>(this IQueryable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);
			var currentSource = (IQueryable<T>)(LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source);
			return currentSource.Provider.CreateQuery<T>(
				Expression.Call(null, MethodHelper.GetMethodInfo(AsUnionQuery, source), currentSource.Expression));
		}

		/// <summary>
		/// Loads this collection sub-query via its own dedicated pre-query (one query per association),
		/// fetching the full parent entity on the parent side of the join.
		/// <para>
		/// Works both inside a <c>Select</c> projection and directly in a <see cref="LoadWith{TEntity,TProperty}(IQueryable{TEntity}, System.Linq.Expressions.Expression{System.Func{TEntity,TProperty}})"/> selector.
		/// Consider <see cref="AsKeyedQuery{T}(IQueryable{T})"/> instead when the parent entity has many columns.
		/// </para>
		/// </summary>
		/// <example>
		/// <code>
		/// // Select projection
		/// from o in db.Orders
		/// select new { Items = o.OrderItems.AsSeparateQuery().ToList() }
		///
		/// // LoadWith selector
		/// db.Orders.LoadWith(o => o.OrderItems.AsSeparateQuery()).ToList()
		/// </code>
		/// </example>
		/// <typeparam name="T">Element type of the collection.</typeparam>
		/// <param name="source">The collection sub-query to mark.</param>
		/// <returns><paramref name="source"/> unchanged at runtime (translation-time marker only).</returns>
		[Pure]
		public static IEnumerable<T> AsSeparateQuery<T>(this IEnumerable<T> source) => source;

		/// <inheritdoc cref="AsSeparateQuery{T}(IEnumerable{T})"/>
		[LinqTunnel]
		[Pure]
		public static IQueryable<T> AsSeparateQuery<T>(this IQueryable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);
			var currentSource = (IQueryable<T>)(LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source);
			return currentSource.Provider.CreateQuery<T>(
				Expression.Call(null, MethodHelper.GetMethodInfo(AsSeparateQuery, source), currentSource.Expression));
		}

		/// <summary>
		/// Marks this query to use the PostQuery eager loading strategy.
		/// The main query results are buffered, distinct parent keys are extracted client-side,
		/// and child records are loaded in a single batch query using <c>WHERE key IN (...)</c>
		/// or a <c>VALUES</c> table join.
		/// <para>
		/// Applied to the root query; the strategy propagates to all contained child collections.
		/// </para>
		/// <para>
		/// <b>Fallback behavior:</b> When a child projection references non-key parent fields
		/// (e.g., <c>CompanyName = c.Name</c> inside a child <c>Select</c>), the strategy
		/// automatically falls back to the Default eager loading for that specific child.
		/// Other children in the same query continue using PostQuery.
		/// </para>
		/// </summary>
		/// <example>
		/// <code>
		/// // Root-level — applies to all children
		/// (from o in db.Orders
		///  select new { Items = o.OrderItems.ToList() }
		/// ).AsKeyedQuery().ToList()
		///
		/// // LoadWith
		/// db.Orders.LoadWith(o => o.OrderItems).AsKeyedQuery().ToList()
		/// </code>
		/// </example>
		/// <typeparam name="T">Element type of the collection.</typeparam>
		/// <param name="source">The root query to mark.</param>
		/// <returns><paramref name="source"/> unchanged at runtime (translation-time marker only).</returns>
		[LinqTunnel]
		[Pure]
		public static IQueryable<T> AsKeyedQuery<T>(this IQueryable<T> source)
		{
			ArgumentNullException.ThrowIfNull(source);
			var currentSource = (IQueryable<T>)(LinqExtensions.ProcessSourceQueryable?.Invoke(source) ?? source);
			return currentSource.Provider.CreateQuery<T>(
				Expression.Call(null, MethodHelper.GetMethodInfo(AsKeyedQuery, source), currentSource.Expression));
		}

		#endregion
		}
	}
