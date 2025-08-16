using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;

namespace LinqToDB.LINQPad;

internal sealed class SQLiteProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.SQLiteClassic, "SQLite")
	];

	public SQLiteProvider()
		: base(ProviderName.SQLite, "SQLite", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
		SQLiteConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return SQLiteFactory.Instance;
	}
}
