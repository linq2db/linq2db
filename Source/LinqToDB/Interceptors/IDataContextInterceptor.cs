using System.Threading.Tasks;
namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Intercepts <see cref="IDataContext"/> close and disposal lifecycle events.
	/// </summary>
	/// <remarks>
	/// Use this interface when behavior should run at the data-context lifetime boundary rather than for individual commands.
	/// Methods are called before and after <see cref="IDataContext.Close"/> or <see cref="IDataContext.CloseAsync"/>.
	/// This interface observes context close events, not physical connection close events.
	/// Register implementations through <see cref="DataOptionsExtensions.UseInterceptor(DataOptions, IInterceptor)"/>.
	/// </remarks>
	public interface IDataContextInterceptor : IInterceptor
	{
		/// <summary>
		/// Called before <see cref="IDataContext"/> instance is closed by <see cref="IDataContext.Close"/>.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		void OnClosing(DataContextEventData eventData);
		/// <summary>
		/// Called after <see cref="IDataContext"/> instance is closed by <see cref="IDataContext.Close"/>.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		void OnClosed(DataContextEventData eventData);
		/// <summary>
		/// Called before <see cref="IDataContext"/> instance is closed by <see cref="IDataContext.CloseAsync"/>.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		Task OnClosingAsync(DataContextEventData eventData);
		/// <summary>
		/// Called after <see cref="IDataContext"/> instance is closed by <see cref="IDataContext.CloseAsync"/>.
		/// </summary>
		/// <param name="eventData">Additional data for event.</param>
		Task OnClosedAsync(DataContextEventData eventData);
	}
}
