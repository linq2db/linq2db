using System;

namespace LinqToDB.Interceptors
{
	public interface IInterceptable
	{
		void InterceptorAdded(IInterceptor interceptor);
	}

	public interface IInterceptable<T> : IInterceptable
		where T : IInterceptor
	{
		T? Interceptor { get; set; }
	}
}
