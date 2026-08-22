using NUnit.Framework.Interfaces;

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
