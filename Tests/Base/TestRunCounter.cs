using System;
using System.Collections.Generic;
using System.Reflection;

using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal.Execution;

namespace Tests
{
	/// <summary>
	/// Counts the tests a run will execute, for the <c>--test-progress</c> heartbeat's total. NUnit builds the run's
	/// work-item tree from the filter the adapter passes down (so <c>--filter</c> is already applied) before the root
	/// suite starts; this walks that tree and counts the leaves that will run the reporter's <c>ITestAction</c>.
	/// <para>
	/// NUnit exposes the tree only through its dispatcher's private top-level work item, so the root item is found by
	/// reflection. Every member returns <see langword="null"/> rather than throwing when that fails, and the caller
	/// falls back to the discovered count.
	/// </para>
	/// </summary>
	public static class TestRunCounter
	{
		/// <summary>
		/// Number of tests under <paramref name="root"/> that the run will execute, or <see langword="null"/> when
		/// the root work item cannot be resolved from <paramref name="dispatcher"/>.
		/// </summary>
		public static long? TryCount(IWorkItemDispatcher? dispatcher, ITest root)
		{
			try
			{
				var item = TryResolveRootWorkItem(dispatcher, root);

				return item == null ? null : CountActionTests(item);
			}
			catch
			{
				// Never let progress reporting break a test run.
				return null;
			}
		}

		/// <summary>
		/// The work item <paramref name="dispatcher"/> holds for <paramref name="root"/>, or <see langword="null"/>.
		/// A field value is accepted only when it belongs to <paramref name="root"/>: the parallel dispatcher swaps
		/// its top-level item while it runs isolated work.
		/// </summary>
		public static WorkItem? TryResolveRootWorkItem(IWorkItemDispatcher? dispatcher, ITest root)
		{
			if (dispatcher == null)
				return null;

			foreach (var field in FindWorkItemFields(dispatcher.GetType()))
				if (field.GetValue(dispatcher) is WorkItem item && ReferenceEquals(item.Test, root))
					return item;

			return null;
		}

		/// <summary>Instance fields of <paramref name="dispatcherType"/> (and its base types) that can hold a <see cref="WorkItem"/>.</summary>
		public static IReadOnlyList<FieldInfo> FindWorkItemFields(Type dispatcherType)
		{
			var result = new List<FieldInfo>();

			for (var type = dispatcherType; type != null; type = type.BaseType)
				foreach (var field in type.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
					if (typeof(WorkItem).IsAssignableFrom(field.FieldType))
						result.Add(field);

			return result;
		}

		/// <summary>Number of leaves under <paramref name="item"/> that will run their test actions.</summary>
		public static long CountActionTests(WorkItem item)
		{
			if (!WillRun(item))
				return 0;

			if (item is not CompositeWorkItem composite)
				return 1;

			var count = 0L;

			foreach (var child in composite.Children)
				count += CountActionTests(child);

			return count;
		}

		// The predicate NUnit applies to both kinds of item: SimpleWorkItem.MakeTestCommand wraps the actions only for
		// these, and CompositeWorkItem.PerformWork skips every other suite together with its children.
		static bool WillRun(WorkItem item)
		{
			return item.Test.RunState switch
			{
				RunState.Runnable => true,
				RunState.Explicit => item.Filter.IsExplicitMatch(item.Test),
				_                 => false,
			};
		}
	}
}
