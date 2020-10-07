using System;

using NUnit.Framework.Interfaces;

namespace Tests
{
	public static class RunnerSupport
	{
		public static string? GetConfiguration(ITest test)
		{
			if (!test.HasChildren && test.Method is {} method)
			{
				var parameters = method.GetParameters();

				if (test.Arguments.Length == parameters.Length)
				{
					for (var i = 0; i < parameters.Length; i++)
					{
						var p = parameters[i];

						foreach (var a in p.GetCustomAttributes<DataSourcesBaseAttribute>(true))
						{
							if (!(test.Arguments[i] is string context))
								continue;

							var queue = context.EndsWith(".LinqService")
								? context.Substring(0, context.Length - ".LinqService".Length)
								: context;

							switch (queue)
							{
								case "SqlServer"        : queue = "SqlServer.2008"; break;
								case "SqlServer.2005.1" : queue = "SqlServer.2005"; break;
								case "SqlServer.2008.1" : queue = "SqlServer.2008"; break;
							}

							return queue;
						}
					}
				}

				return "SQLite.Classic";
			}

			return null;
		}
	}
}
