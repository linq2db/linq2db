using System;

using LinqToDB.Tools;

namespace LinqToDB.Interceptors
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
