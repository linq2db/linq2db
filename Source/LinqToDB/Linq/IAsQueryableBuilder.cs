using System;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace LinqToDB.Linq
{
	/// <summary>
	/// Initial stage of <see cref="LinqExtensions.AsQueryable{T}(IEnumerable{T},IDataContext,Expression{Func{IAsQueryableBuilder{T},IAsQueryableExceptBuilder{T}}})"/>
	/// configuration. The caller must pick a default rendering mode (<see cref="Parameterize"/> or <see cref="Inline"/>)
	/// before any further configuration is available. All chain methods are marker-only — the chain is captured as an
	/// expression tree and interpreted at query-build time. Calling them outside an <see cref="Expression"/> context
	/// is undefined behaviour.
	/// </summary>
	/// <typeparam name="T">Element type of the source enumerable.</typeparam>
	public interface IAsQueryableBuilder<T>
	{
		/// <summary>
		/// Render every column of every row as a SQL parameter.
		/// </summary>
		IAsQueryableExceptBuilder<T> Parameterize();

		/// <summary>
		/// Render every column of every row as an inlined SQL literal. Columns whose mapping produces a
		/// <see cref="LinqToDB.Data.DataParameter"/> are always rendered as SQL parameters regardless of this
		/// setting, because the <c>DataParameter</c> carries provider metadata that cannot be inlined.
		/// </summary>
		IAsQueryableExceptBuilder<T> Inline();
	}
}
