#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Data.Common;

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

	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName) => [("DuckDB.NET.Data.Full", NuGetPackageVersions.DuckDB_NET_Data_Full)];

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
