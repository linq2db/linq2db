namespace LinqToDB.Identity
{
	/// <summary>
	/// <see cref="DataOptions"/> extensions for the linq2db Identity stores.
	/// </summary>
	public static class IdentityDataOptionsExtensions
	{
		/// <summary>
		/// Tells the Identity stores that the database cascades deletes from <c>AspNetUsers</c> and <c>AspNetRoles</c> to
		/// their dependent tables, so <c>DeleteAsync</c> deletes only the user or role row.
		/// See <see cref="LinqToDBIdentityOptions.DatabaseCascadeDelete"/>.
		/// </summary>
		/// <param name="options">Options to update.</param>
		/// <param name="enabled">Whether the database cascades the deletes.</param>
		/// <returns>New options with the setting applied.</returns>
		public static DataOptions UseIdentityDatabaseCascadeDelete(this DataOptions options, bool enabled = true)
		{
			return options.WithOptions<LinqToDBIdentityOptions>(o => o with { DatabaseCascadeDelete = enabled });
		}
	}
}
