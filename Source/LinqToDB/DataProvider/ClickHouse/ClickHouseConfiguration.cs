using System;

namespace LinqToDB.DataProvider.ClickHouse
{
	public static class ClickHouseConfiguration
	{
		/// <summary>
		/// Enables -OrNull combinator for Min, Max, Sum and Avg aggregation functions to support SQL standard-compatible behavior.
		/// Default value: <c>false</c>.
		/// </summary>
		[Obsolete("Use ClickHouseOptions.Default.UseStandardCompatibleAggregates instead.")]
		public static bool UseStandardCompatibleAggregates
		{
			get => ClickHouseOptions.Default.UseStandardCompatibleAggregates;
			set => ClickHouseOptions.Default = ClickHouseOptions.Default with { UseStandardCompatibleAggregates = value };
		}
	}
}
