using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Internal.Linq
{
	/// <summary>
	/// A side-effecting step bound to a prepared query. Registered during SQL preparation and
	/// stored on the cached <see cref="Query"/>; <see cref="Setup"/> runs before the main query
	/// executes and <see cref="Teardown"/> runs after it completes (success or failure).
	/// Distinct from <see cref="Preamble"/>, which is owned by eager loading and uses positional
	/// return-value passing — mixing a non-eager-loading step into the preamble array would shift
	/// every eager-loader's hard-coded array index.
	/// </summary>
	abstract class QueryRunStep
	{
		/// <summary>
		/// Synchronous setup. Runs once per query execution, before any preamble or the main
		/// query SQL fires. Implementations should be idempotent against partial prior runs.
		/// </summary>
		public abstract void Setup(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters);

		/// <summary>
		/// Async equivalent of <see cref="Setup"/>.
		/// </summary>
		public abstract Task SetupAsync(IDataContext dataContext, IQueryExpressions expressions, object?[]? parameters, CancellationToken cancellationToken);

		/// <summary>
		/// Synchronous teardown. Runs in the <see langword="finally"/> of the query execute path, after the
		/// result enumerable is fully consumed or an exception unwinds the call.
		/// </summary>
		public abstract void Teardown(IDataContext dataContext);

		/// <summary>
		/// Async equivalent of <see cref="Teardown"/>.
		/// </summary>
		public abstract Task TeardownAsync(IDataContext dataContext, CancellationToken cancellationToken);
	}
}
