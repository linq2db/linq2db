using System;
using System.Diagnostics;
using System.IO;
using System.Text;

using LinqToDB.Common;
using LinqToDB.Data;

using NUnit.Framework;
using NUnit.Framework.Internal;

namespace Tests
{
	using Model;

	static class BaselinesManager
	{
		public static void LogQuery(string message)
		{
			var ctx = CustomTestContext.Get();

			if (ctx.Get<bool>(CustomTestContext.BASELINE_DISABLED) != true)
			{
				if (message.StartsWith("BeforeExecute"))
				{
					var baseline = ctx.Get<StringBuilder>(CustomTestContext.BASELINE);
					if (baseline == null)
					{
						baseline = new StringBuilder();
						ctx.Set(CustomTestContext.BASELINE, baseline);
					}

					baseline.AppendLine(message);
				}
			}
		}

		public static void Dump()
		{
			if (TestConfiguration.BaselinesPath != null)
			{
				var baseline = CustomTestContext.Get().Get<StringBuilder>(CustomTestContext.BASELINE);
				if (baseline != null)
					BaselinesWriter.Write(TestConfiguration.BaselinesPath, baseline.ToString());
			}
		}
	}
}
