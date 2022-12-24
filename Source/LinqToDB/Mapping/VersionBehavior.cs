namespace LinqToDB.Mapping
{
	/// <summary>
	/// Defines optimistic concurrency column modification strategy. Used with <see cref="ConcurrencyPropertyAttribute" /> attribute
	/// and <see cref="ConcurrencyExtensions" /> extensions, e.g. <see cref="ConcurrencyExtensions.UpdateConcurrent{T}(IDataContext, T)"/> or <see cref="ConcurrencyExtensions.UpdateConcurrentAsync{T}(IDataContext, T, System.Threading.CancellationToken)"/> methods.
	/// </summary>
	public enum VersionBehavior
	{
		/// <summary>
		/// Column value modified by server automatically on update. E.g. SQL Server rowversion/timestamp column or database trigger.
		/// </summary>
		Auto,
		/// <summary>
		/// Column value should be incremented by 1.
		/// </summary>
		AutoIncrement,
		/// <summary>
		/// Use current timestamp value (provided by <see cref="Sql.CurrentTimestamp" /> helper).
		/// </summary>
		CurrentTimestamp
	}
}
