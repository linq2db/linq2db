using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Linq;
	using Linq.Builder;

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
			if (table == null) throw new ArgumentNullException(nameof(table));

			var newTable = new Table<T>(table.DataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(LoadWithAsTable, table, selector),
					new[] { table.Expression, Expression.Quote(selector) })
			);

			return newTable;
		}

		abstract class LoadWithQueryableBase<TEntity> : IExpressionQuery
		{
			public LoadWithQueryableBase(IQueryable<TEntity> query)
			{
				Query = query;
			}

			//IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQuery() => (_query as IExpressionQuery)?.GetSqlQuery() ?? Array.Empty<QuerySql>();
			//public IDataContext DataContext => ;
			//public Type ElementType => _query.ElementType;
			//public IQueryProvider Provider => _query.Provider;

			public IQueryable<TEntity> Query { get; }

			Expression              IExpressionQuery.Expression                                   => Query.Expression;
			IDataContext            IExpressionQuery.DataContext                                  => ((IExpressionQuery)Query.GetLinqToDBSource()).DataContext;
			IReadOnlyList<QuerySql> IExpressionQuery.GetSqlQueries(SqlGenerationOptions? options) => ((IExpressionQuery)Query.GetLinqToDBSource()).GetSqlQueries(options);
		}

		sealed class LoadWithQueryable<TEntity, TProperty> : LoadWithQueryableBase<TEntity>, ILoadWithQueryable<TEntity, TProperty>
		{
			public LoadWithQueryable(IQueryable<TEntity> query)
				: base(query)
			{
			}

			Type           IQueryable.ElementType => Query.ElementType;
			Expression     IQueryable.Expression  => Query.Expression;
			IQueryProvider IQueryable.Provider    => Query.Provider;

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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(LoadWith, source, selector),
				currentSource.Expression,
				Expression.Quote(selector));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(LoadWith, source, selector, loadFunc),
				new[] { currentSource.Expression, Expression.Quote(selector), Expression.Quote(loadFunc) });

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity, TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(LoadWith, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector),
				currentSource.Expression,
				Expression.Quote(selector));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector),
				new[] { currentSource.Expression, Expression.Quote(selector) });

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				new[] { currentSource.Expression, Expression.Quote(selector), Expression.Quote(loadFunc) });

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity,TProperty>(result);
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
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var currentSource = source.ProcessIQueryable();

			var expr = Expression.Call(
				null,
				MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
				currentSource.Expression,
				Expression.Quote(selector),
				Expression.Quote(loadFunc));

			var result = currentSource.Provider.CreateQuery<TEntity>(expr);
			return new LoadWithQueryable<TEntity, TProperty>(result);
		}

		[LinqTunnel]
		[Pure]
		internal static TSource LoadWithInternal<TSource>(
			this TSource             source,
			LoadWithInfo             loadWith,
			MemberInfo[]?            loadWithPath)
			where TSource : class
		{
			return source;
		}
	}
}
