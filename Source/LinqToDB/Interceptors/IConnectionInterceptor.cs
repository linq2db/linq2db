using System;
using System.Data.Common;

namespace LinqToDB.Interceptors
{
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
