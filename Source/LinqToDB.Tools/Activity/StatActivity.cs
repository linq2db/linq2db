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

		public string     Name    { get; }

		private long     _elapsedTicks;
		private TimeSpan _elapsed;
		public  TimeSpan  Elapsed => _elapsedTicks > 0 ? new(_elapsedTicks) : _elapsed;

		private long     _callCount;
		public  long      CallCount => _callCount;

		public IActivity Start()
		{
			Interlocked.Increment(ref _callCount);

			return Stopwatch.IsHighResolution ? (IActivity)new Watcher(this) : new WatcherLowRes(this);
		}

		void Stop(Stopwatch stopwatch)
		{
			Interlocked.Add(ref _elapsedTicks, stopwatch.ElapsedTicks);
		}

		void Stop(TimeSpan time)
		{
			lock (this)
				_elapsed += time;
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

		sealed class WatcherLowRes : IActivity
		{
			readonly StatActivity _metric;
			readonly DateTime     _time;

			public WatcherLowRes(StatActivity metric)
			{
				_time   = DateTime.Now;;
				_metric = metric;
			}

			public void Dispose()
			{
				_metric.Stop(DateTime.Now - _time);
			}
		}
	}
}
