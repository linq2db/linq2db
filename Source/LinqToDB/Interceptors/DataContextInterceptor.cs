using System;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.Interceptors
{
	public abstract class DataContextInterceptor : IDataContextInterceptor
	{
		public virtual void OnClosed(DataContextEventData eventData) { }

		public virtual void OnClosing(DataContextEventData eventData) { }

		public virtual Task OnClosedAsync(DataContextEventData eventData) => TaskEx.CompletedTask;

		public virtual Task OnClosingAsync(DataContextEventData eventData) => TaskEx.CompletedTask;
	}
}
