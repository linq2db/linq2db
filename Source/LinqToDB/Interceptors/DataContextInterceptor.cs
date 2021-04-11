namespace LinqToDB.Interceptors
{
	public abstract class DataContextInterceptor : IDataContextInterceptor
	{
		public virtual object EntityCreated(EntityCreatedEventData eventData, object entity) => entity;
	}
}
