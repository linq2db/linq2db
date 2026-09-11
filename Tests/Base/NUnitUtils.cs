using NUnit.Framework;
using NUnit.Framework.Interfaces;
using NUnit.Framework.Internal;

namespace Tests
{
	internal static class NUnitUtils
	{
		// True for the per-provider CreateDatabase test cases. Under parallel execution they are
		// routed off the provider lane (so a provider's other tests can wait on a readiness
		// latch without deadlocking the single-thread lane).
		public static bool IsCreateDatabase(ITest test)
		{
			if (test.Method == null || test.Arguments.Length == 0)
				return false;

			foreach (var parameter in test.Method.GetParameters())
				if (parameter.GetCustomAttributes<CreateDatabaseSourcesAttribute>(true).Length != 0)
					return true;

			return false;
		}

		// True for tests marked [UsesRemoteContext] - they build a remote context from a non-remote
		// parameter value, so GetContext cannot see it from the arguments alone.
		public static bool UsesRemoteContext(ITest test)
		{
			if (test.Method == null)
				return false;

			return test.Method.GetCustomAttributes<UsesRemoteContextAttribute>(true).Length != 0
				|| test.Method.TypeInfo.GetCustomAttributes<UsesRemoteContextAttribute>(true).Length != 0;
		}

		// True for [NonParallelizable] tests and for tests inside a [NonParallelizable] fixture. Both run
		// under ResourceLaneDispatcher's exclusive write lock - the fixture case because its whole subtree
		// is executed inline while that lock is held - so nothing runs alongside them at all, which is
		// stronger than the secondary mutex. The dispatcher never asks the lane strategy about them, so a
		// classifier-based check cannot see the exclusivity; this reads the same ParallelScope.None
		// property the dispatcher routes on, walking up because the mark sits on the suite, not the leaf.
		public static bool IsGloballyExclusive(ITest test)
		{
			for (var current = test; current != null; current = current.Parent)
				if (current.Properties.Get(PropertyNames.ParallelScope) is ParallelScope scope && scope.HasFlag(ParallelScope.None))
					return true;

			return false;
		}

		public static (string? context, bool isLinqService) GetContext(ITest test)
		{
			if (test.Arguments.Length > 0)
			{
				var parameters = test.Method!.GetParameters();

				for (var i = 0; i < parameters.Length; i++)
				{
					var attr = parameters[i].GetCustomAttributes<DataSourcesBaseAttribute>(true);

					if (attr.Length != 0)
					{
						var context = (string)test.Arguments[i]!;

						if (context.IsRemote())
						{
							return (context.StripRemote(), true);
						}

						return (context, false);
					}
				}
			}

			return default;
		}
	}
}
