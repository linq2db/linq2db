using System;
using System.Threading.Tasks;

using LinqToDB.Tools;

namespace LinqToDB.Interceptors
{
	sealed class AggregatedDataContextInterceptor : AggregatedInterceptor<IDataContextInterceptor>, IDataContextInterceptor
	{
		protected override AggregatedInterceptor<IDataContextInterceptor> Create()
		{
			return new AggregatedDataContextInterceptor();
		}

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
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		public async Task OnClosedAsync(DataContextEventData eventData)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await using (ActivityService.StartAndConfigureAwait(ActivityID.DataContextInterceptorOnClosedAsync))
						await interceptor.OnClosedAsync(eventData)
							.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
