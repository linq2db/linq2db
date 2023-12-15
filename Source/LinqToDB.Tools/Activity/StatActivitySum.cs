using System;
using System.Linq;

namespace LinqToDB.Tools.Activity
{
	sealed class StatActivitySum(string name, params StatActivity[] metrics) : IStatActivity
	{
		public StatActivity[] Metrics   { get; } = metrics;
		public string         Name      { get; } = name;
		public TimeSpan       Elapsed   => new(Metrics.Sum(m => m.Elapsed.Ticks));
		public long           CallCount => Metrics.Sum(m => m.CallCount);
	}
}
