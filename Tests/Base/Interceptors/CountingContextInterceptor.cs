using System.Threading.Tasks;

using LinqToDB.Interceptors;

namespace Tests
{
	public sealed class CountingContextInterceptor : DataContextInterceptor
	{
		public bool OnClosedTriggered { get; set; }
		public bool OnClosedAsyncTriggered { get; set; }
		public bool OnClosingTriggered { get; set; }
		public bool OnClosingAsyncTriggered { get; set; }

		public int OnClosedCount { get; set; }
		public int OnClosedAsyncCount { get; set; }
		public int OnClosingCount { get; set; }
		public int OnClosingAsyncCount { get; set; }

		public override void OnClosed(DataContextEventData eventData)
		{
			OnClosedTriggered = true;
			OnClosedCount++;
			base.OnClosed(eventData);
		}

		public override Task OnClosedAsync(DataContextEventData eventData)
		{
			OnClosedAsyncTriggered = true;
			OnClosedAsyncCount++;
			return base.OnClosedAsync(eventData);
		}

		public override void OnClosing(DataContextEventData eventData)
		{
			OnClosingTriggered = true;
			OnClosingCount++;
			base.OnClosing(eventData);
		}

		public override Task OnClosingAsync(DataContextEventData eventData)
		{
			OnClosingAsyncTriggered = true;
			OnClosingAsyncCount++;
			return base.OnClosingAsync(eventData);
		}
	}
}
