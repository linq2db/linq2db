using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public interface IDataContextInterceptor : IInterceptor
	{
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
