using LinqToDB;

namespace Tests
{
	public static class TestExtensions
	{
		public static DataOptions OmitUnsupportedCompareNulls(this DataOptions options, string context)
		{
			return options.WithOptions(options.LinqOptions.WithCompareNullsAsValues(/*!context.IsAnyOf(TestProvName.AllClickHouse)*/true));
		}
	}
}
