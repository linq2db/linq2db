using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Interceptors;

namespace Tests
{
	public sealed class CountingConnectionInterceptor : ConnectionInterceptor
	{
		public bool ConnectionOpenedTriggered { get; set; }
		public bool ConnectionOpenedAsyncTriggered { get; set; }
		public bool ConnectionOpeningTriggered { get; set; }
		public bool ConnectionOpeningAsyncTriggered { get; set; }

		public int ConnectionOpenedCount { get; set; }
		public int ConnectionOpenedAsyncCount { get; set; }
		public int ConnectionOpeningCount { get; set; }
		public int ConnectionOpeningAsyncCount { get; set; }

		public override void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
		{
			ConnectionOpenedTriggered = true;
			ConnectionOpenedCount++;
			base.ConnectionOpened(eventData, connection);
		}

		public override Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			ConnectionOpenedAsyncTriggered = true;
			ConnectionOpenedAsyncCount++;
			return base.ConnectionOpenedAsync(eventData, connection, cancellationToken);
		}

		public override void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
		{
			ConnectionOpeningTriggered = true;
			ConnectionOpeningCount++;
			base.ConnectionOpening(eventData, connection);
		}

		public override Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			ConnectionOpeningAsyncTriggered = true;
			ConnectionOpeningAsyncCount++;
			return base.ConnectionOpeningAsync(eventData, connection, cancellationToken);
		}
	}
}
