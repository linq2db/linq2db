using System;
using System.Runtime.CompilerServices;

namespace LinqToDB.Tools
{
	public static class ActivityService
	{
		public static Func<ActivityID, IActivity?> Start { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = static _ => null;

		public static IActivity? StartImpl(ActivityID activityID)
		{
			var factory = _factory!;

			var list = factory.GetInvocationList();

			if (list.Length == 1)
				return factory(activityID);

			var activities = new IActivity?[list.Length];

			for (var i = 0; i < list.Length; i++)
				activities[i] = ((Func<ActivityID, IActivity>)list[i])(activityID);

			return new MultiActivity(activities);
		}

		static Func<ActivityID,IActivity?>? _factory;

		public static void AddFactory(Func<ActivityID,IActivity?> factory)
		{
			if (_factory == null)
			{
				_factory += factory;
				Start    = static id => _factory(id);
			}
			else
			{
				_factory += factory;
				Start    = StartImpl;
			}
		}

		class MultiActivity : IActivity
		{
			readonly IActivity?[] _activities;

			public MultiActivity(IActivity?[] activities)
			{
				_activities = activities;
			}

			public void Dispose()
			{
				foreach (var activity in _activities)
					activity?.Dispose();
			}
		}
	}
}
