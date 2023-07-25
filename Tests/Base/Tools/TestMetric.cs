using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;

using LinqToDB.Tools;

namespace Tests.Tools
{
	public interface ITestMetric
	{
		string   Name      { get; }
		TimeSpan Elapsed   { get; }
		long     CallCount { get; }
	}

	public class TestMetric : ITestMetric
	{
		public TestMetric(string name)
		{
			Name = name;
		}

		public string    Name    { get; }

		private long _elapsedTicks;
		public  TimeSpan Elapsed => new(_elapsedTicks);

		private long     _callCount;
		public  long     CallCount => _callCount;

		public Watcher Start()
		{
			return new(this);
		}

		void Stop(Stopwatch stopwatch)
		{
			Interlocked.Increment(ref _callCount);
			Interlocked.Add(ref _elapsedTicks, stopwatch.ElapsedTicks);
		}

		public class Watcher : IActivity
		{
			readonly TestMetric _metric;
			readonly Stopwatch  _stopwatch = new();

			public Watcher(TestMetric metric)
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

	public class TestMetricSum : ITestMetric
	{
		public TestMetricSum(string name, params TestMetric[] metrics)
		{
			Name     = name;
			_metrics = metrics;
		}

		readonly TestMetric[] _metrics;

		public string   Name    { get; }

		public TimeSpan Elapsed   => new (_metrics.Sum(m => m.Elapsed.Ticks));

		public long     CallCount => _metrics.Sum(m => m.CallCount);
	}
}
