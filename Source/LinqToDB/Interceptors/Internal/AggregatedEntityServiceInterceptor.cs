using System;

using LinqToDB.Tools;

namespace LinqToDB.Interceptors.Internal
{
	sealed class AggregatedEntityServiceInterceptor : AggregatedInterceptor<IEntityServiceInterceptor>, IEntityServiceInterceptor
	{
		public object EntityCreated(EntityCreatedEventData eventData, object entity)
		{
			return Apply(() =>
			{
				foreach (var interceptor in Interceptors)
					using (ActivityService.Start(ActivityID.EntityServiceInterceptorEntityCreated))
						entity = interceptor.EntityCreated(eventData, entity);
				return entity;
			});
		}
	}
}
