namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Base class with pass-through implementations for <see cref="IEntityServiceInterceptor"/>.
	/// </summary>
	/// <remarks>
	/// Derive from this class when post-processing materialized mapped entities.
	/// For callback timing and return-value contracts, see <see cref="IEntityServiceInterceptor"/>.
	/// </remarks>
	public abstract class EntityServiceInterceptor : IEntityServiceInterceptor
	{
		/// <inheritdoc />
		public virtual object EntityCreated(EntityCreatedEventData eventData, object entity) => entity;
	}
}
