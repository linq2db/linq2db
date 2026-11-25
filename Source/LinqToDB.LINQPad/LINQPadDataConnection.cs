using LinqToDB.Data;

namespace LinqToDB.LINQPad;

/// <summary>
/// Context for direct use from driver code. Internal: a public <see cref="DataConnection"/> subclass breaks
/// LINQPad 5 driver discovery, see <see cref="LINQPadContextOptions"/>.
/// </summary>
internal sealed class LINQPadDataConnection : DataConnection
{
	internal LINQPadDataConnection(ConnectionSettings settings)
		: base(LINQPadContextOptions.Create(
			settings.Connection.Provider,
			settings.Connection.ProviderPath,
			settings.Connection.GetFullConnectionString()))
	{
		if (settings.Connection.CommandTimeout != null)
			CommandTimeout = settings.Connection.CommandTimeout.Value;
	}
}
