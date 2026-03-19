namespace LinqToDB.Data
{
	/// <summary>
	/// Defines the action to take when conflicts occur during bulk copy operations.
	/// </summary>
	public enum ConflictAction
	{
		/// <summary>
		/// Default behavior - conflicts will be handled according to the database's normal behavior (typically will fail).
		/// </summary>
		Default = 0,

		/// <summary>
		/// Ignore conflicts during bulk copy operation.
		/// Supported for following databases:
		/// <list type="bullet">
		/// <item>MySql/MariaDB - uses INSERT IGNORE INTO statement (only works with <see cref="BulkCopyType.MultipleRows"/>)</item>
		/// <item>PostgreSQL - adds ON CONFLICT DO NOTHING clause to INSERT INTO statement (only works with <see cref="BulkCopyType.MultipleRows"/>)</item>
		/// <item>SQLite - uses INSERT OR IGNORE INTO statement (only works with <see cref="BulkCopyType.MultipleRows"/>)</item>
		/// </list>
		/// </summary>
		Ignore = 1,
	}
}
