using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using AdoNetCore.AseClient;

using LinqToDB.Data;

namespace LinqToDB.LINQPad;

internal sealed class SybaseAseProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.SybaseManaged, "SAP/Sybase ASE"),
	];

	public SybaseAseProvider()
		: base(ProviderName.Sybase, "SAP/Sybase ASE", _providers)
	{
	}

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName) => [("AdoNetCore.AseClient", NuGetPackageVersions.AdoNetCore_AseClient)];
#endif

	public override void ClearAllPools(string providerName)
	{
		AseConnection.ClearPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(crdate) FROM sysobjects").FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return AseClientFactory.Instance;
	}
}
