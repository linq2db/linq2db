using System;

namespace LinqToDB.Tools
{
	public static class ActivityService
	{
		public static IActivity? Start(ActivityID activityID)
		{
			var factory = _factory;

			if (factory == null)
				return null;

			var list = factory.GetInvocationList();

			if (list.Length == 1)
				return factory(activityID);

			var activities = new IActivity[list.Length];

			for (var i = 0; i < list.Length; i++)
				activities[i] = ((Func<ActivityID, IActivity>)list[i])(activityID);

			return new MultiActivity(activities);
		}

		static Func<ActivityID,IActivity>? _factory;

		public static void AddFactory(Func<ActivityID,IActivity>? factory)
		{
			_factory += factory;
		}

		class MultiActivity : IActivity
		{
			readonly IActivity[] _activities;

			public MultiActivity(IActivity[] activities)
			{
				_activities = activities;
			}

			public void Dispose()
			{
				foreach (var activity in _activities)
					activity.Dispose();
			}
		}
	}
}
