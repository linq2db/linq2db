namespace LinqToDB.Metadata
{
	/// <summary>
	/// Defines generated metadata source.
	/// </summary>
	public enum MetadataSource
	{
		/// <summary>
		/// No metadata generated. User configure own mappings or models used as-is without explicit mappings.
		/// </summary>
		None,
		/// <summary>
		/// Generated model annotated with mapping attributes.
		/// </summary>
		Attributes,
		/// <summary>
		/// Context contains static instance of mapping schema with mappings, configured using fluent mapper.
		/// </summary>
		FluentMapping,
	}
}
