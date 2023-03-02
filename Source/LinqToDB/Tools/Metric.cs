#if METRICS

using System;
using System.Diagnostics;
using System.Threading;

namespace LinqToDB.Tools
{
	public class Metric
	{
		public Metric(string name)
		{
			Name = name;
		}
		public string    Name    { get; }

		private TimeSpan _elapsed;
		public  TimeSpan Elapsed => _elapsed;

		private int      _callCount;
		public  int      CallCount => _callCount;

		public Watcher Start()
		{
			return new(this);
		}

		public void Stop(Stopwatch stopwatch)
		{
			Interlocked.Increment(ref _callCount);

			lock (this)
				_elapsed += stopwatch.Elapsed;
		}

		public class Watcher : IDisposable
		{
			readonly Metric    _metric;
			readonly Stopwatch _stopwatch = new();

			public Watcher(Metric metric)
			{
				_stopwatch.Start();
				_metric = metric;
			}

			public void Dispose()
			{
				_stopwatch.Stop();
				_metric.Stop(_stopwatch);
			}
		}
	}
}

#endif
