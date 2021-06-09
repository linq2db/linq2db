using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public interface IDataContextInterceptor : IInterceptor
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
		/// <returns>Returns entity instace.</returns>
		object EntityCreated(DataContextEventData eventData, object entity);

		/// <summary>
		/// Event, triggered before <see cref="IDataContext" /> instance closed by <see cref="IDataContext.Close"/> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		void OnClosing(DataContextEventData eventData);

		/// <summary>
		/// Event, triggered after <see cref="IDataContext" /> instance closed by <see cref="IDataContext.Close"/> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		void OnClosed(DataContextEventData eventData);

		/// <summary>
		/// Event, triggered before <see cref="IDataContext" /> instance closed by <see cref="IDataContext.CloseAsync"/> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		Task OnClosingAsync(DataContextEventData eventData);

		/// <summary>
		/// Event, triggered after <see cref="IDataContext" /> instance closed by <see cref="IDataContext.CloseAsync"/> call.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		Task OnClosedAsync(DataContextEventData eventData);
	}
}
