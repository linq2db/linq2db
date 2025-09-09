using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;

using Microsoft.Data.Sqlite;

namespace LinqToDB.LINQPad;

internal sealed class SQLiteProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.SQLiteClassic, "Official Client (System.Data.SQLite)"   ),
		new(ProviderName.SQLiteMS,      "Microsof Client (Microsoft.Data.Sqlite)")
	];

	public SQLiteProvider()
		: base(ProviderName.SQLite, "SQLite", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
		if (providerName == ProviderName.SQLiteClassic)
			SQLiteConnection.ClearAllPools();
		else
			SqliteConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (providerName == ProviderName.SQLiteClassic)
			return SQLiteFactory.Instance;
		else
			return SqliteFactory.Instance;
	}
}
