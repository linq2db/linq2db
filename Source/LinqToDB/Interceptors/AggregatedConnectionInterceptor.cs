using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	sealed class AggregatedConnectionInterceptor : AggregatedInterceptor<IConnectionInterceptor>, IConnectionInterceptor
	{
		public void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					interceptor.ConnectionOpening(eventData, connection);
			});
		}

		public async Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await interceptor.ConnectionOpeningAsync(eventData, connection, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		public void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					interceptor.ConnectionOpened(eventData, connection);
			});
		}

		public async Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await interceptor.ConnectionOpenedAsync(eventData, connection, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
