using System;

namespace LinqToDB.Interceptors
{
	sealed class AggregatedEntityServiceInterceptor : AggregatedInterceptor<IEntityServiceInterceptor>, IEntityServiceInterceptor
	{
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
