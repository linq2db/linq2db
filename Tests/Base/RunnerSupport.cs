
using NUnit.Framework.Interfaces;

namespace Tests
{
	public static class RunnerSupport
	{
		public static string? GetConfiguration(ITest test)
		{
			var (context, isRemote) = NUnitUtils.GetContext(test);
			switch (context)
			{
				case "SQLite.Default"   : context = "Default";        break;
			}

			return context;
		}
	}
}
