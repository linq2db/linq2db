namespace LinqToDB.Interceptors
{
	interface IEntityServiceInterceptable
	{
		AggregatedInterceptor<IEntityServiceInterceptor>? Interceptors { get; }
	}
}
