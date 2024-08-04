using LinqToDB.Interceptors;

namespace LinqToDB.EntityFrameworkCore.BaseTests.Interceptors
{
	public class TestEntityServiceInterceptor : TestInterceptor, IEntityServiceInterceptor
	{
		public object EntityCreated(EntityCreatedEventData eventData, object entity)
		{
			HasInterceptorBeenInvoked = true;
			return entity;
		}
	}
}
