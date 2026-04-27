using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Initial stage of <see cref="LinqExtensions.AsQueryable{T}(IEnumerable{T},IDataContext,Expression{Func{IAsQueryableBuilder{T},IAsQueryableExceptBuilder{T}}})"/>
	/// configuration. The caller must pick a default rendering mode (<see cref="Parameterize"/> or <see cref="Inline"/>)
	/// before any further configuration is available. All chain methods are marker-only and throw at runtime;
	/// the chain is captured as an expression tree and interpreted at query-build time.
	/// </summary>
	/// <typeparam name="T">Element type of the source enumerable.</typeparam>
	public interface IAsQueryableBuilder<T>
	{
		/// <summary>
		/// Render every column of every row as a SQL parameter.
		/// </summary>
		IAsQueryableExceptBuilder<T> Parameterize();

		/// <summary>
		/// Render every column of every row as an inlined SQL literal.
		/// </summary>
		IAsQueryableExceptBuilder<T> Inline();
	}
}
