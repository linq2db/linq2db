using System;
using System.Diagnostics;
using System.Threading;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivity(string name, ActivityID activityID) : IStatActivity
	{
		public string     Name       { get; } = name;
		public ActivityID ActivityID { get; } = activityID;

		private long     _elapsedTicks;
		public  TimeSpan  Elapsed => new(_elapsedTicks);

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
			Interlocked.Add(ref _elapsedTicks, time.Ticks);
		}

		sealed class Watcher : ActivityBase
		{
			readonly StatActivity _metric;
			readonly Stopwatch    _stopwatch = new();

			public Watcher(StatActivity metric) : base(metric.ActivityID)
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

		sealed class WatcherLowRes(StatActivity metric) : ActivityBase(metric.ActivityID)
		{
			readonly DateTime _time = DateTime.Now;

			public override void Dispose()
			{
				metric.Stop(DateTime.Now - _time);
			}
		}
	}
}
