using System.Text;

using NUnit.Framework;

namespace Tests
{
	public static class BaselinesManager
	{
		public static void LogQuery(string message)
		{
			var ctx = CustomTestContext.Get();

			if (!ctx.Get<bool>(CustomTestContext.BASELINE_DISABLED))
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

		public static void Dump(bool isRemote, string? providerSuffix = null)
		{
			// A failed test can leave a truncated/partial baseline: SQL captured only up to the point it
			// threw, or only one of the direct/remote contexts ran. Skip the dump for failed tests so CI
			// never commits a partial file; a passing (re-)run regenerates the baseline in full.
			if (TestContext.CurrentContext.Result.FailCount > 0)
				return;

			if (TestConfiguration.BaselinesPath != null)
			{
				var baseline = CustomTestContext.Get().Get<StringBuilder>(CustomTestContext.BASELINE);
				if (baseline != null)
					BaselinesWriter.Write(TestConfiguration.BaselinesPath, baseline.ToString(), isRemote, providerSuffix);
			}
		}
	}
}
