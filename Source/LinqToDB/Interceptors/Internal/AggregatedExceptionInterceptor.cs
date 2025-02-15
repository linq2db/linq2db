using System;

namespace LinqToDB.Interceptors.Internal
{
	sealed class AggregatedExceptionInterceptor : AggregatedInterceptor<IExceptionInterceptor>, IExceptionInterceptor
	{
		/// <inheritdoc />
		public void ProcessException(ExceptionEventData eventData, Exception exception)
		{
			Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					interceptor.ProcessException(eventData, exception);
			});
		}
	}
}
