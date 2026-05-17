namespace LinqToDB.Internal.Common
{
	/// <summary>
	/// Internal identity used by configuration-sensitive caches.
	/// </summary>
	/// <remarks>
	/// Different instances with equivalent configuration should return the same
	/// <see cref="ConfigurationID"/>. This lets higher-level objects compose cheap
	/// integer identities instead of repeatedly comparing complex object graphs.
	/// </remarks>
	public interface IConfigurationID
	{
		int ConfigurationID { get; }
	}
}
