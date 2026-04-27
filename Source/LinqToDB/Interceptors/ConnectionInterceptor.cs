using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Base class with no-op implementations for <see cref="IConnectionInterceptor"/>.
	/// </summary>
	/// <remarks>
	/// Derive from this class when overriding only selected physical connection open callbacks.
	/// This class observes connection opening only; it is not called when a physical connection is closed.
	/// For callback timing, see <see cref="IConnectionInterceptor"/>.
	/// </remarks>
	public abstract class ConnectionInterceptor : IConnectionInterceptor
	{
		/// <inheritdoc />
		public virtual void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
		{
		}

		/// <inheritdoc />
		public virtual Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		public virtual void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
		{
		}

		/// <inheritdoc />
		public virtual Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
