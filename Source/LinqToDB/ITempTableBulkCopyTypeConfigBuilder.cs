namespace LinqToDB
{
	/// <summary>
	/// Per-bulk-copy-type configuration nested inside the <see cref="ITempTableBulkCopyOptionsBuilder"/>
	/// chain. Setters apply where they're meaningful for the chosen type (e.g.
	/// <see cref="WithMaxBatchSize"/> has no effect on
	/// <see cref="ITempTableBulkCopyOptionsBuilder.UseRowByRow"/>) and are otherwise no-ops at
	/// runtime — the provider's bulk-copy implementation ignores irrelevant settings.
	/// </summary>
	public interface ITempTableBulkCopyTypeConfigBuilder
	{
		/// <summary>
		/// Maximum rows per insert batch. Higher values reduce round-trips at the cost of memory
		/// and parameter-pack budget. Meaningful for <see cref="ITempTableBulkCopyOptionsBuilder.UseMultiRows"/>
		/// and most <see cref="ITempTableBulkCopyOptionsBuilder.UseProviderSpecific"/> providers.
		/// </summary>
		ITempTableBulkCopyTypeConfigBuilder WithMaxBatchSize(int value);

		/// <summary>
		/// Per-batch command timeout in seconds. Large temp-table populations may need a longer
		/// timeout than the provider's default.
		/// </summary>
		ITempTableBulkCopyTypeConfigBuilder WithBulkCopyTimeout(int value);
	}
}
