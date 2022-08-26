namespace LinqToDB.Interceptors
{
	class AggregatedDataContextInterceptor : AggregatedInterceptor<IDataContextInterceptor>, IDataContextInterceptor
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
					interceptor.OnClosing(eventData);
			});
		}

		public void OnClosed(DataContextEventData eventData)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					interceptor.OnClosed(eventData);
			});
		}

		public async Task OnClosingAsync(DataContextEventData eventData)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await interceptor.OnClosingAsync(eventData).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}

		public async Task OnClosedAsync(DataContextEventData eventData)
		{
			await Apply(async () =>
			{
				foreach (var interceptor in Interceptors)
					await interceptor.OnClosedAsync(eventData).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}).ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
		}
	}
}
