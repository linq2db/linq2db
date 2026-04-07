#if !NETFRAMEWORK
using DuckDB.NET.Data;

namespace LinqToDB.LINQPad;

internal sealed class DuckDBProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.DuckDB, "DuckDB", true),
	];

	public DuckDBProvider()
		: base(ProviderName.DuckDB, "DuckDB", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings) => null;

	public override DbProviderFactory? GetProviderFactory(string providerName)
	{
		return DuckDBClientFactory.Instance;
	}
}
#endif
