namespace LinqToDB.DataProvider.ClickHouse
{
	// TODO: temporary, should be replaced with context configuration from https://github.com/linq2db/linq2db/pull/3530
	// when it is done
	public static class ClickHouseConfiguration
	{
		/// <summary>
		/// Enables -OrNull combinator for Min, Max, Sum and Avg aggregation functions to support SQL standard-compatible behavior.
		/// Default value: <c>false</c>.
		/// </summary>
		public static bool UseStandardCompatibleAggregates { get; set; }
	}
}
