using LinqToDB.Data;

namespace LinqToDB.LINQPad;

/// <summary>
/// Base class for generated contexts and context for direct use.
/// </summary>
public class LINQPadDataConnection : DataConnection
{
	/// <summary>
	/// Constructor for inherited context.
	/// </summary>
	/// <param name="providerName">Provider name.</param>
	/// <param name="providerPath">Optional provider assembly path.</param>
	/// <param name="connectionString">Connection string must have password manager tokens replaced already.</param>
	protected LINQPadDataConnection(string? providerName, string? providerPath, string? connectionString)
		: base(
			new DataOptions()
			  .UseConnectionString(connectionString ?? throw new LinqToDBLinqPadException("Connection string missing"))
			  .UseDataProvider(DatabaseProviders.GetDataProvider(providerName, connectionString, providerPath)))
	{
	}

	/// <summary>
	/// Constructor for use from driver code directly.
	/// </summary>
	internal LINQPadDataConnection(ConnectionSettings settings)
		: this(
			settings.Connection.Provider,
			settings.Connection.ProviderPath,
			settings.Connection.GetFullConnectionString())
	{
		if (settings.Connection.CommandTimeout != null)
			CommandTimeout = settings.Connection.CommandTimeout.Value;
	}
}
