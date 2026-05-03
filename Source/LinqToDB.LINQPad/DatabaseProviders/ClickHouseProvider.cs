using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

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

	public override void ClearAllPools(string providerName)
	{
		// octonica provider doesn't implement connection pooling
		// client provider use http connections pooling
		if (string.Equals(providerName, ProviderName.ClickHouseMySql, StringComparison.Ordinal))
			MySqlConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(metadata_modification_time) FROM system.tables WHERE database = database()").FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.ClickHouseDriver, StringComparison.Ordinal))
			return new ClickHouseConnectionFactory();
#if !NETFRAMEWORK
		if (string.Equals(providerName, ProviderName.ClickHouseOctonica, StringComparison.Ordinal))
			return new ClickHouseDbProviderFactory();
#endif

		return MySqlConnectorFactory.Instance;
	}
}
