using System;
using System.Linq;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivitySum(string name, params IStatActivity[] metrics) : IStatActivity
	{
		public IStatActivity[] Metrics   { get; } = metrics;
		public string          Name      { get; } = name;
		public TimeSpan        Elapsed   => new(Metrics.Sum(m => m.Elapsed.Ticks));
		public long            CallCount => Metrics.Sum(m => m.CallCount);
	}
}
