using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Intercepts physical database connection open events.
	/// </summary>
	/// <remarks>
	/// Use this interface for connection-scoped initialization and diagnostics that require direct access to the <see cref="DbConnection"/>.
	/// Methods are called before and after physical <see cref="DbConnection.Open"/> or <see cref="DbConnection.OpenAsync(CancellationToken)"/> calls.
	/// This interface observes connection opening only; it is not called when a physical connection is closed.
	/// For simpler before/after-open callbacks, see <see cref="DataOptionsExtensions.UseBeforeConnectionOpened(DataOptions, System.Action{DbConnection}, System.Func{DbConnection, CancellationToken, Task}?)"/>
	/// and <see cref="DataOptionsExtensions.UseAfterConnectionOpened(DataOptions, System.Action{DbConnection}, System.Func{DbConnection, CancellationToken, Task}?)"/>.
	/// </remarks>
	public interface IConnectionInterceptor : IInterceptor
	{
		/// <summary>
		/// Called before <see cref="DbConnection.Open"/> is invoked.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		void ConnectionOpening(ConnectionEventData eventData, DbConnection connection);
		/// <summary>
		/// Called before <see cref="DbConnection.OpenAsync(CancellationToken)"/> is invoked.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken);
		/// <summary>
		/// Called after <see cref="DbConnection.Open"/> completes.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		void ConnectionOpened(ConnectionEventData eventData, DbConnection connection);
		/// <summary>
		/// Called after <see cref="DbConnection.OpenAsync(CancellationToken)"/> completes.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken);
	}
}
