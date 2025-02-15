using System.Text;

namespace Tests
{
	public static class BaselinesManager
	{
		public static void LogQuery(string message)
		{
			var ctx = CustomTestContext.Get();

			if (ctx.Get<bool>(CustomTestContext.BASELINE_DISABLED) != true)
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

		public static void Dump(string? providerSuffix = null)
		{
			if (TestConfiguration.BaselinesPath != null)
			{
				var baseline = CustomTestContext.Get().Get<StringBuilder>(CustomTestContext.BASELINE);
				if (baseline != null)
					BaselinesWriter.Write(TestConfiguration.BaselinesPath, baseline.ToString(), providerSuffix);
			}
		}
	}
}
