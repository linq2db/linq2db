using System;
using System.Linq;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivitySum : IStatActivity
	{
		public StatActivitySum(string name, params StatActivity[] metrics)
		{
			Name     = name;
			_metrics = metrics;
		}

		readonly StatActivity[] _metrics;

		public string Name { get; }

		public TimeSpan Elapsed => new (_metrics.Sum(m => m.Elapsed.Ticks));

		public long CallCount => _metrics.Sum(m => m.CallCount);
	}
}
