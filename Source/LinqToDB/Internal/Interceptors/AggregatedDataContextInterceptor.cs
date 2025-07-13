using System.Threading.Tasks;

using LinqToDB.Interceptors;
using LinqToDB.Metrics;

namespace LinqToDB.Internal.Interceptors
{
	sealed class AggregatedDataContextInterceptor : AggregatedInterceptor<IDataContextInterceptor>, IDataContextInterceptor
	{
		public void OnClosing(DataContextEventData eventData)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosing))
						interceptor.OnClosing(eventData);
			});
		}

		public void OnClosed(DataContextEventData eventData)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.DataContextInterceptorOnClosed))
						interceptor.OnClosed(eventData);
			});
		}

		public async Task OnClosingAsync(DataContextEventData eventData)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosingAsync))
						await interceptor.OnClosingAsync(eventData)
							.ConfigureAwait(false);
			}).ConfigureAwait(false);
		}

		public async Task OnClosedAsync(DataContextEventData eventData)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosedAsync))
						await interceptor.OnClosedAsync(eventData)
							.ConfigureAwait(false);
			}).ConfigureAwait(false);
		}
	}
}
