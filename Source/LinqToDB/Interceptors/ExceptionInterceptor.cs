using System;

namespace LinqToDB.Interceptors
{
	public class ExceptionInterceptor : IExceptionInterceptor
	{
		public virtual void ProcessException(ExceptionEventData eventData, Exception exception) { }
	}
}
