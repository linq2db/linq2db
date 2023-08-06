using System;
using System.Linq;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivitySum : IStatActivity
	{
		public StatActivitySum(string name, params StatActivity[] metrics)
		{
			Name     = name;
			Metrics = metrics;
		}

		public readonly StatActivity[] Metrics;

		public string Name { get; }

		public TimeSpan Elapsed => new (Metrics.Sum(m => m.Elapsed.Ticks));

		public long CallCount => Metrics.Sum(m => m.CallCount);
	}
}
