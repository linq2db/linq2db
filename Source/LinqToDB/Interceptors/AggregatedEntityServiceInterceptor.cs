namespace LinqToDB.Interceptors
{
	class AggregatedEntityServiceInterceptor : AggregatedInterceptor<IEntityServiceInterceptor>, IEntityServiceInterceptor
	{
		protected override AggregatedInterceptor<IEntityServiceInterceptor> Create()
		{
			return new AggregatedEntityServiceInterceptor();
		}

		public object EntityCreated(EntityCreatedEventData eventData, object entity)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					entity = interceptor.EntityCreated(eventData, entity);
				return entity;
			});
		}
	}
}
