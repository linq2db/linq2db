namespace LinqToDB.Interceptors
{
	public interface IInterceptable
	{
	}

	public interface IInterceptable<T> : IInterceptable
		where T : IInterceptor
	{
		T? Interceptor { get; set; }
	}
}
