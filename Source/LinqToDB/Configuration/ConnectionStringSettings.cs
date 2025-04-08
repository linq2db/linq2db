namespace LinqToDB.Configuration
{
	/// <summary>
	/// Provides explicitly-defined <see cref="IConnectionStringSettings"/> implementation.
	/// </summary>
	public class ConnectionStringSettings : IConnectionStringSettings
	{
		public ConnectionStringSettings(
				string name,
				string connectionString,
				string providerName)
		{
			Name             = name;
			ConnectionString = connectionString;
			ProviderName     = providerName;
		}

		public string ConnectionString { get; }
		public string Name             { get; }
		public string ProviderName     { get; }
		public bool   IsGlobal => false;
	}
}
