using System;

namespace LinqToDB.Tools
{
	public static class ActivityService
	{
		public static IActivity? Start(ActivityID metric)
		{
			return _factory?.Invoke(metric);
		}

		static Func<ActivityID,IActivity>? _factory;

		public static void SetFactory(Func<ActivityID,IActivity>? factory)
		{
			_factory = factory;
		}
	}
}
