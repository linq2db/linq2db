using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Common;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Contains reference to filter function defined by <see cref="EntityMappingBuilder{T}.HasQueryFilter(Expression{Func{T, IDataContext, bool}})"/>
	/// or one of its keyed overloads. Multiple instances of this attribute may be applied to the same entity to declare
	/// several named filters; at query time the filters are AND-combined unless suppressed by
	/// <see cref="LinqExtensions.IgnoreFilters{TSource}(System.Linq.IQueryable{TSource}, System.Type[])"/>
	/// or its keyed counterparts.
	/// </summary>
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true)]
	public class QueryFilterAttribute : MappingAttribute
	{
		/// <summary>
		/// Filter identifier used to address the filter individually via the keyed
		/// <see cref="EntityMappingBuilder{T}.HasQueryFilter(string, Expression{Func{T, IDataContext, bool}}?)"/>
		/// overloads and to selectively disable it via
		/// <see cref="LinqExtensions.IgnoreFilters{TSource}(System.Linq.IQueryable{TSource}, System.Collections.Generic.IEnumerable{string}, System.Type[])"/>.
		/// <para>
		/// A <see langword="null"/> or empty value identifies the default (anonymous) filter slot, populated by the
		/// non-keyed <c>HasQueryFilter</c> overloads.
		/// </para>
		/// </summary>
		public string?           FilterKey    { get; set; }

		/// <summary>
		/// Filter LambdaExpression. <code>Expression&lt;Func&lt;TEntity, IDataContext, bool&gt;&gt;</code>
		/// <para>
		/// For example (e, db) => e.IsDeleted == false
		/// </para>
		/// </summary>
		public LambdaExpression? FilterLambda { get; set; }

		// we cannot use
		// <see cref="System.Func{System.Linq.IQueryable{T},LinqToDB.IDataContext,System.Linq.IQueryable{T}}"/>
		// as it produce compiler/documentation errors due https://github.com/dotnet/csharplang/issues/401
		/// <summary>
		/// Filter function of type <see cref="Func{T1, T2, TResult}"/>, where
		/// <list type="bullet">
		/// <item>- T1 and TResult are <see cref="IQueryable{T}"/></item>
		/// <item>- T2 is <see cref="IDataContext"/></item>
		/// </list>
		/// </summary>
		public Delegate?         FilterFunc   { get; set; }

		public override string GetObjectID()
		{
			// Length-prefix the user-controlled FilterKey so a key containing '.' cannot collide with the
			// id-segment boundaries (the '.' separators alone are ambiguous once a segment can embed one).
			return string.Create(CultureInfo.InvariantCulture, $"{FilterKey?.Length ?? 0}:{FilterKey}.{IdentifierBuilder.GetObjectID(FilterLambda)}.{IdentifierBuilder.GetObjectID(FilterFunc?.Method)}");
		}
	}
}
