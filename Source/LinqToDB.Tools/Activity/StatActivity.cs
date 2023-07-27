using System;
using System.Diagnostics;
using System.Threading;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivity : IStatActivity
	{
		public StatActivity(string name)
		{
			Name = name;
		}

		public string    Name    { get; }

		private long _elapsedTicks;
		public  TimeSpan Elapsed => new(_elapsedTicks);

		private long     _callCount;
		public  long     CallCount => _callCount;

		public IActivity Start()
		{
			return new Watcher(this);
		}

		void Stop(Stopwatch stopwatch)
		{
			Interlocked.Increment(ref _callCount);
			Interlocked.Add(ref _elapsedTicks, stopwatch.ElapsedTicks);
		}

		sealed class Watcher : IActivity
		{
			readonly StatActivity _metric;
			readonly Stopwatch    _stopwatch = new();

			public Watcher(StatActivity metric)
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
