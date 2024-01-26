using System;

using LinqToDB.Tools;

namespace LinqToDB.Interceptors
{
	sealed class AggregatedEntityServiceInterceptor : AggregatedInterceptor<IEntityServiceInterceptor>, IEntityServiceInterceptor
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
					using (ActivityService.Start(ActivityID.EntityServiceInterceptorEntityCreated))
						entity = interceptor.EntityCreated(eventData, entity);
				return entity;
			});
		}
	}
}
