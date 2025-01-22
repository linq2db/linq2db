using System;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

using JetBrains.Annotations;

namespace LinqToDB.Tools
{
	using Data;

	/// <summary>
	/// Provides API to register factory methods that return an Activity object or <c>null</c> for provided <see cref="ActivityID"/> event.
	/// </summary>
	[PublicAPI]
	public static class ActivityService
	{
		internal static Func<ActivityID,IActivity?> Start { [MethodImpl(MethodImplOptions.AggressiveInlining)] get; private set; } = static _ => null;

		static IActivity? StartImpl(ActivityID activityID)
		{
			if (_factory == null)
				throw new InvalidOperationException();

			var factory = _factory;

			var list = factory.GetInvocationList();

			if (list.Length == 1)
				return factory(activityID);

			var activities = new IActivity?[list.Length];

			for (var i = 0; i < list.Length; i++)
				activities[i] = ((Func<ActivityID, IActivity>)list[i])(activityID);

			return new MultiActivity(activities);
		}

		internal sealed class AsyncDisposableWrapper(IActivity activity)
		{
			public ConfiguredValueTaskAwaitable DisposeAsync()
			{
				return activity.DisposeAsync().ConfigureAwait(false);
			}

			internal AsyncDisposableWrapper AddQueryInfo(DataConnection? context, DbConnection? connection, DbCommand? command)
			{
				activity.AddQueryInfo(context, connection, command);
				return this;
			}
		}

		internal static AsyncDisposableWrapper? StartAndConfigureAwait(ActivityID activityID)
		{
			var activity = Start(activityID);

			if (activity is null)
				return null;

			return new AsyncDisposableWrapper(activity);
		}

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

		sealed class MultiActivity(IActivity?[] activities) : ActivityBase(ActivityID.None)
		{
			public override IActivity AddTag(ActivityTagID key, object? value)
			{
				foreach (var activity in activities)
					activity?.AddTag(key, value);
				return this;
			}

			public override IActivity AddQueryInfo(DataConnection? context, DbConnection? connection, DbCommand? command)
			{
				foreach (var activity in activities)
					activity?.AddQueryInfo(context, connection, command);
				return this;
			}

			public override void Dispose()
			{
				foreach (var activity in activities)
					activity?.Dispose();
			}

#pragma warning disable CA2215
			public override async ValueTask DisposeAsync()
			{
				foreach (var activity in activities)
					if (activity is not null)
						await activity.DisposeAsync().ConfigureAwait(false);
			}
#pragma warning restore CA2215
		}
	}
}
