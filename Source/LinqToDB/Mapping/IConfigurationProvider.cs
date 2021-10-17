namespace LinqToDB.Mapping
{
	/// <summary>
	/// Provides information about configuration.
	/// Implemented by configuration-dependent attributes.
	/// </summary>
	public interface IConfigurationProvider
	{
		/// <summary>
		/// Get configuration, associated with implementor instance.
		/// </summary>
		string? Configuration { get; }
	}
}
