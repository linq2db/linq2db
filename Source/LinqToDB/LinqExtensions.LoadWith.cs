using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

namespace LinqToDB
{
	using Async;
	using Linq;

	public partial class LinqExtensions
	{
		/// <summary>
		///     Supports queryable LoadWith/ThenLoad chaining operators.
		/// </summary>
		/// <typeparam name="TEntity"> The entity type. </typeparam>
		/// <typeparam name="TProperty"> The property type. </typeparam>
		// ReSharper disable once UnusedTypeParameter
		public interface ILoadWithQueryable<out TEntity, out TProperty> : IQueryable<TEntity>, IAsyncEnumerable<TEntity>
		{
		}

		class LoadWithQueryable<TEntity, TProperty> : ILoadWithQueryable<TEntity, TProperty>
		{
			private readonly IQueryable<TEntity> _query;

			public LoadWithQueryable(IQueryable<TEntity> query)
			{
				_query = query;
			}

			public IEnumerator<TEntity> GetEnumerator() => _query.GetEnumerator();
			IEnumerator IEnumerable.GetEnumerator()     => GetEnumerator();

			IAsyncEnumerator<TEntity> IAsyncEnumerable<TEntity>.GetEnumerator() =>
				((IAsyncEnumerable<TEntity>)_query).GetEnumerator();

			public Expression Expression   => _query.Expression;
			public Type ElementType        => _query.ElementType;
			public IQueryProvider Provider => _query.Provider;
		}

		/// <summary>
		/// Specifies associations, that should be loaded for each loaded record from current table.
		/// All associations, specified in <paramref name="selector"/> expression, will be loaded.
		/// Take into account that use of this method could require multiple queries to load all requested associations.
		/// </summary>
		/// <example>
		/// <para>
		///     <para>
		///         The following query loads records from Table1 with Reference association, loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference);
		///         </code>
		///     </para>
		///     <para>
		///			The following queries loads records from Table1 with Reference1 association and then loads records from Reference2 association for each loaded Reference1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.Reference1.Reference2);
		///         </code>
		///         Same query using ThenLoad extension.
		///         <code>
		///				db.Table1.LoadWith(r => r.Reference1).ThenLoad(r => r.Reference2);
		///         </code>
		///     </para>
		///     <para>
		///         The following query loads records from Table1 with References collection association loaded for each Table1 record.
		///         <code>
		///             db.Table1.LoadWith(r => r.References);
		///         </code>
		///     </para>
		///     <para>
		///         The following query loads loads from Table1 with Reference1 collection association loaded for each Table1 record.
		///			Loads records from Reference2 collection association for each loaded Reference1 record.
		///			Loads records from Reference3 association for each loaded Reference2 record.
		///     <para>
		///			Note that a way you access collection association record (by index, using First() method) doesn't affect query results and always select all records.
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
		/// <typeparam name="TEntity"> The type of entity being queried. </typeparam>
		/// <typeparam name="TProperty"> The type of the related entity to be included. </typeparam>
		/// <param name="source"> The source query. </param>
		/// <param name="selector"> A lambda expression representing the navigation property to be included (<c>t => t.Property1</c>). </param>
		/// <returns> A new query with the related data included. </returns>
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity, TProperty> LoadWith<TEntity, TProperty>(
			this IQueryable<TEntity> source,
			[InstantHandle] Expression<Func<TEntity, TProperty>> selector) 
		{
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var result = source.Provider.CreateQuery<TEntity>(
				Expression.Call(null,
					MethodHelper.GetMethodInfo(LoadWith, source, selector),
					new[] { source.Expression, Expression.Quote(selector) }));

			return new LoadWithQueryable<TEntity, TProperty>(result);
		}
		
		/*
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity, TProperty> LoadWith<TEntity, TProperty>(
			this IQueryable<TEntity> source,
			[InstantHandle] Expression<Func<TEntity, TProperty>> selector, 
			Func<IQueryable<TProperty>, IQueryable<TProperty>> loadFunc)
		{
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var result = source.Provider.CreateQuery<TEntity>(
				Expression.Call(null,
					MethodHelper.GetMethodInfo(LoadWith, source, selector, loadFunc),
					new[] { source.Expression, Expression.Quote(selector), Expression.Constant(loadFunc) }));

			return new LoadWithQueryable<TEntity, TProperty>(result);
		}
		*/

		/*
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity, TProperty> LoadWith<TEntity, TProperty>(
			this IQueryable<TEntity> source,
			[InstantHandle] Expression<Func<TEntity, IEnumerable<TProperty>>> selector, 
			Func<IQueryable<TProperty>, IQueryable<TProperty>> loadFunc)
		{
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var result = source.Provider.CreateQuery<TEntity>(
				Expression.Call(null,
					MethodHelper.GetMethodInfo(LoadWith, source, selector, loadFunc),
					new[] { source.Expression, Expression.Quote(selector), Expression.Constant(loadFunc) }));

			return new LoadWithQueryable<TEntity, TProperty>(result);
		}
		*/
		
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
			this ILoadWithQueryable<TEntity, TPreviousProperty> source,
			[InstantHandle] Expression<Func<TPreviousProperty, TProperty>> selector)
		where TEntity : class
		{
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));
		
			var result = source.Provider.CreateQuery<TEntity>(
				Expression.Call(null,
					MethodHelper.GetMethodInfo(ThenLoad, source, selector),
					new[] { source.Expression, Expression.Quote(selector) }));
		
			return new LoadWithQueryable<TEntity, TProperty>(result);
		}

		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
			this ILoadWithQueryable<TEntity, IEnumerable<TPreviousProperty>> source,
			[InstantHandle] Expression<Func<TPreviousProperty, TProperty>> selector)
		where TEntity : class
		{
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var result = source.Provider.CreateQuery<TEntity>(
				Expression.Call(null,
					MethodHelper.GetMethodInfo(ThenLoad, source, selector),
					new[] { source.Expression, Expression.Quote(selector) }));

			return new LoadWithQueryable<TEntity, TProperty>(result);
		}

		/*
		[LinqTunnel]
		[Pure]
		public static ILoadWithQueryable<TEntity, TProperty> ThenLoad<TEntity, TPreviousProperty, TProperty>(
			this ILoadWithQueryable<TEntity, TPreviousProperty> source,
			[InstantHandle] Expression<Func<TPreviousProperty, TProperty>> selector,
			Func<IQueryable<TProperty>, IQueryable<TProperty>> loadFunc)
		{
			if (source   == null) throw new ArgumentNullException(nameof(source));
			if (selector == null) throw new ArgumentNullException(nameof(selector));

			var result = source.Provider.CreateQuery<TEntity>(
				Expression.Call(null,
					MethodHelper.GetMethodInfo(ThenLoad, source, selector, loadFunc),
					new[] { source.Expression, Expression.Quote(selector), Expression.Constant(loadFunc) }));

			return new LoadWithQueryable<TEntity, TProperty>(result);
		}
		*/
		
	}
}
