namespace LinqToDB.Schema
{
	// Offtopic: https://english.stackexchange.com/questions/56431
	/// <summary>
	/// Table or view column descriptor.
	/// </summary>
	/// <param name="Name">Column name.</param>
	/// <param name="Description">Optional column description.</param>
	/// <param name="Type">Column type.</param>
	/// <param name="Nullable">Column allows <c>NULL</c> values.</param>
	/// <param name="Insertable">Flag indicating that column accepts user-provided values for insert operations.</param>
	/// <param name="Updatable">Flag indicating that column accepts user-provided values for update operations.</param>
	/// <param name="Ordinal">Column ordinal.</param>
	public sealed record Column(string Name, string? Description, DatabaseType Type, bool Nullable, bool Insertable, bool Updatable, int? Ordinal)
	{
		public override string ToString() => Name;
	}
}
