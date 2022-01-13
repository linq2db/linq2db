using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public interface IConnectionInterceptor : IInterceptor
	{
		/// <summary>
		/// Event, triggered before connection open.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		void ConnectionOpening(ConnectionOpeningEventData eventData, DbConnection connection);

		/// <summary>
		/// Event, triggered before asynchronous connection open.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ConnectionOpeningAsync(ConnectionOpeningEventData eventData, DbConnection connection, CancellationToken cancellationToken);

		/// <summary>
		/// Event, triggered after connection opened.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		void ConnectionOpened(ConnectionOpenedEventData eventData, DbConnection connection);

		/// <summary>
		/// Event, triggered after connection opened asynchronously.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="connection">Connection instance.</param>
		/// <param name="cancellationToken">Cancellation token.</param>
		Task ConnectionOpenedAsync(ConnectionOpenedEventData eventData, DbConnection connection, CancellationToken cancellationToken);
	}
}
