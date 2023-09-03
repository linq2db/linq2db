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

			return Environment.OSVersion.Platform != PlatformID.Unix ? (IActivity)new Watcher(this) : new WatcherLowRes(this);
		}

		void Stop(Stopwatch stopwatch)
		{
			Interlocked.Add(ref _elapsedTicks, stopwatch.ElapsedTicks);
		}

		void Stop(TimeSpan time)
		{
#pragma warning disable CA2002 // Do not lock on objects with weak identity
			lock (this)
				_elapsed += time;
#pragma warning restore CA2002 // Do not lock on objects with weak identity
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
