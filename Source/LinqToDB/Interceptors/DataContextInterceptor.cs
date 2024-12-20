using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public abstract class DataContextInterceptor : IDataContextInterceptor
	{
		public virtual void OnClosed      (DataContextEventData eventData) { }
		public virtual void OnClosing     (DataContextEventData eventData) { }
		public virtual Task OnClosedAsync (DataContextEventData eventData) => Task.CompletedTask;
		public virtual Task OnClosingAsync(DataContextEventData eventData) => Task.CompletedTask;
	}
}
