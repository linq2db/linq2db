using LinqToDB;

namespace Tests
{
	public static class TestExtensions
	{
		/// <summary>
		/// Use this extension to disable generation of aditional null checks on join condition for ClickHouse for nullable keys.
		/// </summary>
		public static DataOptions OmitUnsupportedCompareNulls(this DataOptions options, string context)
		{
			return options.WithOptions(options.LinqOptions.WithCompareNullsAsValues(!context.IsAnyOf(TestProvName.AllClickHouse)));
		}
	}
}
