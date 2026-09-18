using System;
using System.Linq.Expressions;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Describes a single named query filter declared on an entity via one of the
	/// <see cref="EntityMappingBuilder{T}.HasQueryFilter(string, Expression{Func{T, IDataContext, bool}}?)"/>
	/// overloads (or the keyless equivalents that target the default slot).
	/// </summary>
	public sealed class EntityQueryFilter
	{
		/// <summary>
		/// Creates a new filter descriptor.
		/// </summary>
		/// <param name="filterKey">Filter identifier. An empty string addresses the default (anonymous) slot.</param>
		/// <param name="filterLambda">Predicate-style filter <c>Expression&lt;Func&lt;TEntity, TDataContext, bool&gt;&gt;</c>, or <see langword="null"/>.</param>
		/// <param name="filterFunc">Function-style filter <c>Func&lt;IQueryable&lt;TEntity&gt;, TDataContext, IQueryable&lt;TEntity&gt;&gt;</c>, or <see langword="null"/>.</param>
		public EntityQueryFilter(string filterKey, LambdaExpression? filterLambda, Delegate? filterFunc)
		{
			ArgumentNullException.ThrowIfNull(filterKey);

			FilterKey    = filterKey;
			FilterLambda = filterLambda;
			FilterFunc   = filterFunc;
		}

		/// <summary>
		/// Filter identifier. The empty string identifies the default (anonymous) filter slot populated by the
		/// keyless <c>HasQueryFilter</c> overloads.
		/// </summary>
		public string            FilterKey    { get; }

		/// <summary>
		/// Predicate-style filter expression of shape
		/// <c>Expression&lt;Func&lt;TEntity, TDataContext, bool&gt;&gt;</c>. <see langword="null"/> when the entry uses
		/// the <see cref="FilterFunc"/> form instead.
		/// </summary>
		public LambdaExpression? FilterLambda { get; }

		/// <summary>
		/// Function-style filter of shape
		/// <c>Func&lt;IQueryable&lt;TEntity&gt;, TDataContext, IQueryable&lt;TEntity&gt;&gt;</c>. <see langword="null"/>
		/// when the entry uses the <see cref="FilterLambda"/> form instead.
		/// </summary>
		public Delegate?         FilterFunc   { get; }
	}
}
