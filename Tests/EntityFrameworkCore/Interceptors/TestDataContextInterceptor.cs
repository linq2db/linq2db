using System.Threading.Tasks;

using LinqToDB.Interceptors;

namespace LinqToDB.EntityFrameworkCore.Tests.Interceptors
{
	public class TestDataContextInterceptor : TestInterceptor, IDataContextInterceptor
	{
		public void OnClosed(DataContextEventData eventData)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task OnClosedAsync(DataContextEventData eventData)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}

		public void OnClosing(DataContextEventData eventData)
		{
			HasInterceptorBeenInvoked = true;
		}

		public Task OnClosingAsync(DataContextEventData eventData)
		{
			HasInterceptorBeenInvoked = true;
			return Task.CompletedTask;
		}
	}
}
