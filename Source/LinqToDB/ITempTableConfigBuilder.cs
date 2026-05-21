using System;

namespace LinqToDB
{
	/// <summary>
	/// Fluent builder for configuring temp-table materialisation of an inline-rows source —
	/// shared between per-call <see cref="LinqExtensions.AsQueryable{TElement}(System.Collections.Generic.IEnumerable{TElement},IDataContext,System.Linq.Expressions.Expression{System.Func{Linq.IAsQueryableBuilder{TElement},Linq.IAsQueryableExceptBuilder{TElement}}})"/>
	/// chains and global <see cref="DataOptions"/> defaults configured via
	/// <c>DataOptionsExtensions.UseTempTablesForLocalCollections</c> /
	/// <c>UseTempTablesForContains</c>. Each setter returns the same builder for chaining;
	/// only properties explicitly set override the provider-level defaults.
	/// </summary>
	public interface ITempTableConfigBuilder
	{
		/// <summary>
		/// Materialise into a real temp table when the source row count exceeds this value at
		/// query-execute time; below the threshold the inline-rows form is used. Required for
		/// the temp-table strategy to fire — without it the builder is a no-op.
		/// </summary>
		ITempTableConfigBuilder Threshold(int value);

		/// <summary>
		/// Tie the temp table's lifetime to the surrounding <see cref="IDataContext"/> instead of
		/// the single query execution. The table is created once on first execution and reused
		/// across subsequent executions of the same <see cref="System.Linq.IQueryable{T}"/>; it is
		/// dropped when the data context closes or disposes. Requires the context to expose
		/// <see cref="IDisposableTracker"/> via
		/// <see cref="LinqToDB.Internal.Infrastructure.IInfrastructure{T}"/>.
		/// </summary>
		ITempTableConfigBuilder DisposeWithConnection();

		/// <summary>
		/// Configure the <see cref="LinqToDB.Data.BulkCopyOptions"/> used to populate the temp
		/// table. The inner builder is invoked once at LINQ-translation time with a fresh
		/// <see cref="ITempTableBulkCopyOptionsBuilder"/>; the accumulated options are forwarded
		/// to the underlying <c>TempTable&lt;T&gt;</c> bulk-copy call at execute time. Only the
		/// subset of <c>BulkCopyOptions</c> relevant to auto-temp-table creation is exposed —
		/// see the builder interface for the full rationale.
		/// </summary>
		ITempTableConfigBuilder ConfigureBulkCopy(Func<ITempTableBulkCopyOptionsBuilder, ITempTableBulkCopyOptionsBuilder> configure);
	}
}
