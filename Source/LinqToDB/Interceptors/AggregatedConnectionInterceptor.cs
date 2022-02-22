using System;
using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	class AggregatedConnectionInterceptor : AggregatedInterceptor<IConnectionInterceptor>, IConnectionInterceptor
	{
		protected override AggregatedInterceptor<IConnectionInterceptor> Create()
		{
			return new AggregatedConnectionInterceptor();
		}

		public void ConnectionOpening(ConnectionOpeningEventData eventData, DbConnection connection)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					interceptor.ConnectionOpening(eventData, connection);
			});
		}

		public async Task ConnectionOpeningAsync(ConnectionOpeningEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await interceptor.ConnectionOpeningAsync(eventData, connection, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		public void ConnectionOpened(ConnectionOpenedEventData eventData, DbConnection connection)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					interceptor.ConnectionOpened(eventData, connection);
			});
		}

		public async Task ConnectionOpenedAsync(ConnectionOpenedEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await interceptor.ConnectionOpenedAsync(eventData, connection, cancellationToken).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
