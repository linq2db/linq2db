using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	/// <summary>
	/// Base class with no-op implementations for <see cref="IDataContextInterceptor"/>.
	/// </summary>
	/// <remarks>
	/// Derive from this class when overriding only selected data-context close lifecycle callbacks.
	/// These callbacks observe context close events, not physical connection close events.
	/// For callback timing, see <see cref="IDataContextInterceptor"/>.
	/// </remarks>
	public abstract class DataContextInterceptor : IDataContextInterceptor
	{
		/// <inheritdoc />
		public virtual void OnClosed      (DataContextEventData eventData) { }
		/// <inheritdoc />
		public virtual void OnClosing     (DataContextEventData eventData) { }
		/// <inheritdoc />
		public virtual Task OnClosedAsync (DataContextEventData eventData) => Task.CompletedTask;
		/// <inheritdoc />
		public virtual Task OnClosingAsync(DataContextEventData eventData) => Task.CompletedTask;
	}
}
