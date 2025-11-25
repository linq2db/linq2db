using System;
using System.Collections.Generic;
using System.Data.Common;

using Npgsql;

namespace LinqToDB.LINQPad;

internal sealed class PostgreSQLProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.PostgreSQL  , "Detect Dialect Automatically", true),
		new(ProviderName.PostgreSQL92, "PostgreSQL 9.2 Dialect"            ),
		new(ProviderName.PostgreSQL93, "PostgreSQL 9.3 Dialect"            ),
		new(ProviderName.PostgreSQL95, "PostgreSQL 9.5 Dialect"            ),
		new(ProviderName.PostgreSQL13, "PostgreSQL 13 Dialect"             ),
		new(ProviderName.PostgreSQL15, "PostgreSQL 15 Dialect"             ),
		new(ProviderName.PostgreSQL18, "PostgreSQL 18 Dialect"             ),
	];

	public PostgreSQLProvider()
		: base(ProviderName.PostgreSQL, "PostgreSQL", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
		NpgsqlConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return NpgsqlFactory.Instance;
	}
}
