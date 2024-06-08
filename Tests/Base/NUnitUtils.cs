namespace Tests
{
	using NUnit.Framework.Interfaces;

	internal static class NUnitUtils
	{
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
