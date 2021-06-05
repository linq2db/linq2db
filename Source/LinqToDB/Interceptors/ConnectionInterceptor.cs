using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public abstract class ConnectionInterceptor : IConnectionInterceptor
	{
		public virtual void ConnectionOpened(ConnectionOpenedEventData eventData, DbConnection connection)
		{
		}

		public virtual Task ConnectionOpenedAsync(ConnectionOpenedEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			return TaskEx.CompletedTask;
		}

		public virtual void ConnectionOpening(ConnectionOpeningEventData eventData, DbConnection connection)
		{
		}

		public virtual Task ConnectionOpeningAsync(ConnectionOpeningEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			return TaskEx.CompletedTask;
		}
	}
}
