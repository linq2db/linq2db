namespace LinqToDB.Schema
{
	/// <summary>
	/// Service to map database type to .net type with <see cref="DataType"/> hint.
	/// </summary>
	public interface ITypeMappingProvider
	{
		/// <summary>
		/// Returns mapping information for specific database type: .net type and optionally <see cref="DataType"/> hint enum.
		/// </summary>
		/// <param name="databaseType">Database type.</param>
		/// <returns>Mapping to .net type or <c>null</c>, if no mapping defined for specified database type.</returns>
		TypeMapping? GetTypeMapping(DatabaseType databaseType);
	}
}
