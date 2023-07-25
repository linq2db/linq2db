using System;

namespace LinqToDB.Tools
{
	public static class Metrics
	{
		public static IActivity? Start(Metric metric)
		{
			return _factory?.Invoke(metric);
		}

		static Func<Metric,IActivity>? _factory;

		public static void SetMetricFactory(Func<Metric,IActivity>? factory)
		{
			_factory = factory;
		}
	}
}
