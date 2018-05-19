namespace LinqToDB.Data
{
	/// <summary>
	/// Bulk copy implementation type.
	/// For more details on support level by provider see 
	/// <a href="https://github.com/linq2db/linq2db/wiki/Bulk-Copy">this article</a>.
	/// </summary>
	public enum BulkCopyType
	{
		/// <summary>
		/// LINQ To DB will select copy method based on current provider.
		/// Default method usually set at [PROVIDER_NAME_HERE]Tools.DefaultBulkCopyType.
		/// </summary>
		Default = 0,
		/// <summary>
		/// Data will be inserted into table as a sequence of selects, row by row.
		/// </summary>
		RowByRow,
		/// <summary>
		/// Data will be inserted into table as a batch insert using INSERT FROM SELECT or similar code.
		/// If method not supported, it will be downgraded to <see cref="RowByRow"/> method.
		/// </summary>
		MultipleRows,
		/// <summary>
		/// Data will be inserted using native bulk copy functionality if supported.
		/// If method not supported, it will be downgraded to <see cref="RowByRow"/> method.
		/// </summary>
		ProviderSpecific
	}
}
