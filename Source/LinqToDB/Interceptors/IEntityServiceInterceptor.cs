namespace LinqToDB.Interceptors
{
	public interface IEntityServiceInterceptor : IInterceptor
	{
		/// <summary>
		/// Event, triggered when a new entity is created during query materialization.
		/// Not triggered for explicitly constructed objects.
		/// <example>
		///  In code below event could be triggered only for first query:
		///  <code>
		/// // r created by linq2db implicitly
		/// <br />
		/// from r in db.table select r;
		/// <br />
		/// <br />
		/// // Entity constructor call specified explicitly by user (projection)
		/// <br />
		/// from r in db.table select new Entity() { field = r.field };
		/// </code>
		/// </example>.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		/// <param name="entity">Materialized entity instance.</param>
		/// <returns>Returns entity instance.</returns>
		object EntityCreated(EntityCreatedEventData eventData, object entity);
	}
}
