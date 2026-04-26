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
	/// For simpler before/after-open callbacks, see <see cref="DataOptionsExtensions.UseBeforeConnectionOpened(DataOptions, System.Action{DbConnection}, System.Func{DbConnection, CancellationToken, Task}?)"/>
	/// and <see cref="DataOptionsExtensions.UseAfterConnectionOpened(DataOptions, System.Action{DbConnection}, System.Func{DbConnection, CancellationToken, Task}?)"/>.
	/// </remarks>
	public interface IConnectionInterceptor : IInterceptor
	{
		/// <summary>
		/// Event, triggered before connection open.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		void ConnectionOpening(ConnectionEventData eventData, DbConnection connection);
		/// <summary>
		/// Event, triggered before asynchronous connection open.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken);
		/// <summary>
		/// Event, triggered after connection opened.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		void ConnectionOpened(ConnectionEventData eventData, DbConnection connection);
		/// <summary>
		/// Event, triggered after connection opened asynchronously.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken);
	}
}