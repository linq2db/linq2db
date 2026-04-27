using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

using JetBrains.Annotations;

using LinqToDB.Internal.Expressions;
using LinqToDB.Internal.Linq;
using LinqToDB.Internal.Reflection;
using LinqToDB.Linq;

namespace LinqToDB
{
	public static partial class LinqExtensions
	{
		/// <summary>
		/// Converts a generic <see cref="IEnumerable{T}"/> to a Linq To DB query, with explicit control
		/// over per-column parameterisation. Compared to <see cref="AsQueryable{TElement}(IEnumerable{TElement},IDataContext)"/>,
		/// this overload renders the source as a multi-row <c>VALUES</c> clause where each cell is either
		/// a SQL parameter or an inlined SQL literal, according to the configuration lambda.
		/// </summary>
		/// <typeparam name="TElement">The type of the elements of <paramref name="source"/>.</typeparam>
		/// <param name="source">A sequence to convert.</param>
		/// <param name="dataContext">Database connection context.</param>
		/// <param name="configure">
		/// Configuration chain. Examples:
		/// <c>b =&gt; b.Parameterize()</c>,
		/// <c>b =&gt; b.Inline()</c>,
		/// <c>b =&gt; b.Parameterize().Except(p =&gt; p.Id, p =&gt; p.CreatedDate)</c>.
		/// </param>
		/// <returns>An <see cref="IQueryable{T}"/> that represents the input sequence.</returns>
		/// <exception cref="ArgumentNullException">
		/// <paramref name="source"/>, <paramref name="dataContext"/> or <paramref name="configure"/> is <see langword="null"/>.
		/// </exception>
		public static IQueryable<TElement> AsQueryable<TElement>(
			                this IEnumerable<TElement>                                                                          source,
			                     IDataContext                                                                                   dataContext,
			[InstantHandle]      Expression<Func<IAsQueryableBuilder<TElement>, IAsQueryableExceptBuilder<TElement>>> configure)
		{
			ArgumentNullException.ThrowIfNull(source);
			ArgumentNullException.ThrowIfNull(dataContext);
			ArgumentNullException.ThrowIfNull(configure);

			var query = new ExpressionQueryImpl<TElement>(dataContext,
				Expression.Call(
					null,
					MethodHelper.GetMethodInfo(AsQueryable, source, dataContext, configure),
					Expression.Constant(source),
					SqlQueryRootExpression.Create(dataContext),
					Expression.Quote(configure)));

			return query;
		}
	}
}
