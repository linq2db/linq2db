using System.Linq;
using System.Linq.Expressions;

using LinqToDB.Internal.Async;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// LINQ query object represented by a LINQ expression tree and executed by a LinqToDB query provider.
	/// </summary>
	/// <remarks>
	/// <para>
	/// <see cref="IExpressionQuery{T}"/> is a composable query description:
	/// it implements <see cref="IQueryable{T}"/>/<see cref="IOrderedQueryable{T}"/> and carries the
	/// LINQ <see cref="Expression"/> that represents the current query.
	/// </para>
	/// <para>
	/// <b>Deferred execution:</b> building/composing the query does not execute it.
	/// Translation and execution happen only when the query is enumerated or explicitly materialized
	/// (sync or async) by the underlying provider.
	/// </para>
	/// <para>
	/// <b>Execution boundary:</b> the associated provider translates the expression tree into an internal SQL AST,
	/// then generates provider-specific SQL text, executes it, and materializes results.
	/// </para>
	/// <para>
	/// <b>Async support:</b> <see cref="IQueryProviderAsync"/> enables asynchronous execution paths for the same query.
	/// </para>
	/// </remarks>
	public interface IExpressionQuery<out T> : IOrderedQueryable<T>, IQueryProviderAsync, IExpressionQuery
	{
		/// <summary>
		/// Gets the LINQ expression tree that represents the current query.
		/// </summary>
		/// <remarks>
		/// This expression is produced by query composition (LINQ operators) and is used by the provider
		/// as input for translation and execution.
		/// </remarks>
		new Expression Expression { get; }
	}
}
