
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
				case "SqlServer"        : context = "SqlServer.2008"; break;
				case "SqlServer.2005.1" : context = "SqlServer.2005"; break;
				case "SqlServer.2008.1" : context = "SqlServer.2008"; break;
			}

			return context;
		}
	}
}
