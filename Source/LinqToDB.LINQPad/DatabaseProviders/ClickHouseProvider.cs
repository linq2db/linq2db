using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Runtime.CompilerServices;

using ClickHouse.Driver.ADO;

using LinqToDB.Data;

using MySqlConnector;
#if !NETFRAMEWORK
using Octonica.ClickHouseClient;
#endif

namespace LinqToDB.LINQPad;

internal sealed class ClickHouseProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.ClickHouseDriver  , "HTTP(S) Interface (ClickHouse.Driver)"             ),
		new (ProviderName.ClickHouseMySql   , "MySQL Interface (MySqlConnector)"                  ),
#if !NETFRAMEWORK
		// octonica provider doesn't support NETFX or NESTANDARD
		new (ProviderName.ClickHouseOctonica, "Binary (TCP) Interface (Octonica.ClickHouseClient)"),
#endif
	];

	public ClickHouseProvider()
		: base(ProviderName.ClickHouse, "ClickHouse", _providers)
	{
	}

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName)
	{
		if (string.Equals(providerName, ProviderName.ClickHouseMySql, StringComparison.Ordinal))
			return [("MySqlConnector", NuGetPackageVersions.MySqlConnector)];

		if (string.Equals(providerName, ProviderName.ClickHouseOctonica, StringComparison.Ordinal))
			return [("Octonica.ClickHouseClient", NuGetPackageVersions.Octonica_ClickHouseClient)];

		return [("ClickHouse.Driver", NuGetPackageVersions.ClickHouse_Driver)];
	}
#endif

	// each client is touched from its own non-inlined method: the assembly is loaded when a method
	// referencing its types is JIT-compiled, and only the packages of the provider the connection uses
	// are provisioned (see GetNuGetPackages), so a shared body would load the ones that are missing
	public override void ClearAllPools(string providerName)
	{
		// octonica provider doesn't implement connection pooling
		// client provider use http connections pooling
		if (string.Equals(providerName, ProviderName.ClickHouseMySql, StringComparison.Ordinal))
			ClearMySqlPools();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ClearMySqlPools() => MySqlConnection.ClearAllPools();

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(metadata_modification_time) FROM system.tables WHERE database = database()").FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.ClickHouseDriver, StringComparison.Ordinal))
			return GetDriverFactory();
#if !NETFRAMEWORK
		if (string.Equals(providerName, ProviderName.ClickHouseOctonica, StringComparison.Ordinal))
			return GetOctonicaFactory();
#endif

		return GetMySqlFactory();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetDriverFactory() => new ClickHouseConnectionFactory();

#if !NETFRAMEWORK
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetOctonicaFactory() => new ClickHouseDbProviderFactory();
#endif

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetMySqlFactory() => MySqlConnectorFactory.Instance;
}
