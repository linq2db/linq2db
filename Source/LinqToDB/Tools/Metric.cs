#if METRICS

using System;
using System.Diagnostics;
using System.Text;

namespace LinqToDB.Tools
{
	public class Metric
	{
		public Metric(string name)
		{
			Name = name;
		}
		public string    Name      { get; }
		public Stopwatch Stopwatch { get; } = new();
		public int       CallCount { get; private set; }

		public IDisposable Start()
		{
			Stopwatch.Start();
			CallCount++;

			return new Stopper(this);
		}

		public void Stop()
		{
			Stopwatch.Stop();
		}

		class Stopper : IDisposable
		{
			readonly Metric _metric;

			public Stopper(Metric metric)
			{
				_metric = metric;
			}

			public void Dispose()
			{
				_metric.Stop();
			}
		}
	}
}

#endif
