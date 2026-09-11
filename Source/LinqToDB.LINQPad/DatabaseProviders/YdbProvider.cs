#if !NETFRAMEWORK
using System;
using System.Collections.Generic;
using System.Data.Common;

using Ydb.Sdk.Ado;

namespace LinqToDB.LINQPad;

internal sealed class YdbProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.Ydb, "YDB", true),
	];

	public YdbProvider()
		: base(ProviderName.Ydb, "YDB", _providers)
	{
	}

	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName) => [("Ydb.Sdk", NuGetPackageVersions.Ydb_Sdk)];

	public override void ClearAllPools(string providerName)
	{
		YdbConnection.ClearAllPools().GetAwaiter().GetResult();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings) => null;

	public override DbProviderFactory? GetProviderFactory(string providerName)
	{
		return YdbProviderFactory.Instance;
	}
}
#endif
