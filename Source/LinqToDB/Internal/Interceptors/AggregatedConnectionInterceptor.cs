using System.Data.Common;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.Interceptors;
using LinqToDB.Tools;

namespace LinqToDB.Internal.Interceptors
{
	sealed class AggregatedConnectionInterceptor : AggregatedInterceptor<IConnectionInterceptor>, IConnectionInterceptor
	{
		public void ConnectionOpening(ConnectionEventData eventData, DbConnection connection)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpening))
						interceptor.ConnectionOpening(eventData, connection);
			});
		}

		public async Task ConnectionOpeningAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionInterceptorConnectionOpeningAsync))
						await interceptor.ConnectionOpeningAsync(eventData, connection, cancellationToken)
							.ConfigureAwait(false);
			}).ConfigureAwait(false);
		}

		public void ConnectionOpened(ConnectionEventData eventData, DbConnection connection)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.ConnectionInterceptorConnectionOpened))
						interceptor.ConnectionOpened(eventData, connection);
			});
		}

		public async Task ConnectionOpenedAsync(ConnectionEventData eventData, DbConnection connection, CancellationToken cancellationToken)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.ConnectionInterceptorConnectionOpenedAsync))
						await interceptor.ConnectionOpenedAsync(eventData, connection, cancellationToken)
							.ConfigureAwait(false);
			}).ConfigureAwait(false);
		}
	}
}
