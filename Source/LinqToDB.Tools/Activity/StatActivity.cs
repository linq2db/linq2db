using System;
using System.Diagnostics;
using System.Threading;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivity(string name) : IStatActivity
	{
		public string Name { get; } = name;

		private long     _elapsedTicks;
		private TimeSpan _elapsed;
		public  TimeSpan  Elapsed => _elapsedTicks > 0 ? new(_elapsedTicks) : _elapsed;

		private long     _callCount;
		public  long      CallCount => _callCount;

		public IActivity Start()
		{
			Interlocked.Increment(ref _callCount);

			return Environment.OSVersion.Platform != PlatformID.Unix ? new Watcher(this) : new WatcherLowRes(this);
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

		sealed class Watcher : ActivityBase
		{
			readonly StatActivity _metric;
			readonly Stopwatch    _stopwatch = new();

			public Watcher(StatActivity metric)
			{
				_stopwatch.Start();
				_metric = metric;
			}

			public override void Dispose()
			{
				_stopwatch.Stop();
				_metric.Stop(_stopwatch);
			}
		}

		sealed class WatcherLowRes(StatActivity metric) : ActivityBase
		{
			readonly DateTime _time = DateTime.Now;

			public override void Dispose()
			{
				metric.Stop(DateTime.Now - _time);
			}
		}
	}
}
