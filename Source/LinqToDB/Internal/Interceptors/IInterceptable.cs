using LinqToDB.Interceptors;

namespace LinqToDB.Internal.Interceptors
{
	interface IInterceptable
	{
	}

	interface IInterceptable<T> : IInterceptable
		where T : IInterceptor
	{
		T? Interceptor { get; set; }
	}
}
