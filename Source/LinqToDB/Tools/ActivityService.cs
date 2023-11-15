using System;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LinqToDB.Tools
{
	public static class ActivityService
	{
		internal static Func<ActivityID,IActivity?> Start { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = static _ => null;

		static IActivity? StartImpl(ActivityID activityID)
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

#if NATIVE_ASYNC
		class AsyncDisposableWrapper : IAsyncDisposable
		{
			readonly ConfiguredAsyncDisposable _configured;

			public AsyncDisposableWrapper(ConfiguredAsyncDisposable configured)
			{
				_configured = configured;
			}

			public async ValueTask DisposeAsync()
			{
				await _configured.DisposeAsync();
			}
		}

		internal static IAsyncDisposable? StartAndConfigureAwait(ActivityID activityID)
		{
			var activity = Start(activityID);

			if (activity is null)
				return null;

			return new AsyncDisposableWrapper(activity.ConfigureAwait(Common.Configuration.ContinueOnCapturedContext));
		}
#else
		internal static IAsyncDisposableEx? StartAndConfigureAwait(ActivityID activityID)
		{
			return Start(activityID);
		}
#endif

		static Func<ActivityID,IActivity?>? _factory;

		/// <summary>
		/// Adds a factory method that returns an Activity object or <c>null</c> for provided <see cref="ActivityID"/> event.
		/// </summary>
		/// <param name="factory">A factory method.</param>
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

#if !NATIVE_ASYNC
			public virtual Task DisposeAsync()
			{
				Dispose();
				return TaskEx.CompletedTask;
			}
#else
			public async ValueTask DisposeAsync()
			{
				foreach (var activity in _activities)
					if (activity is not null)
						await activity.DisposeAsync().ConfigureAwait(Common.Configuration.ContinueOnCapturedContext);
			}
#endif
		}
	}
}
