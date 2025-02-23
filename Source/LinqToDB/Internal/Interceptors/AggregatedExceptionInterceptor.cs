using System;

using LinqToDB.Interceptors;

namespace LinqToDB.Internal.Interceptors
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
