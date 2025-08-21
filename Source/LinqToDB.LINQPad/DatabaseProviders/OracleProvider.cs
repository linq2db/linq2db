using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;

using LinqToDB.Data;

using Oracle.ManagedDataAccess.Client;

namespace LinqToDB.LINQPad;

internal sealed class OracleProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.Oracle         , "Detect Dialect Automatically", true),
		new (ProviderName.Oracle11Managed, "Oracle 11g Dialect"                ),
		new (ProviderName.OracleManaged  , "Oracle 12c Dialect"                ),
	];

	public OracleProvider()
		: base(ProviderName.Oracle, "Oracle", _providers)
	{
	}

	public override void ClearAllPools(string providerName)
	{
		OracleConnection.ClearAllPools();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(LAST_DDL_TIME) FROM USER_OBJECTS WHERE OBJECT_TYPE IN ('TABLE', 'VIEW', 'INDEX', 'FUNCTION', 'PACKAGE', 'PACKAGE BODY', 'PROCEDURE', 'MATERIALIZED VIEW') AND STATUS = 'VALID'").FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return OracleClientFactory.Instance;
	}
}
