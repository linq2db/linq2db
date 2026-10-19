using System;
using System.Collections.Generic;
using System.Data.Common;

#if NETFRAMEWORK
using IBM.Data.DB2;
#else
using IBM.Data.Db2;
#endif

namespace LinqToDB.LINQPad;

internal sealed class InformixProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.InformixDB2, "Informix"),
	];

	public InformixProvider()
		: base(ProviderName.Informix, "IBM Informix", _providers)
	{
	}

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName) => [DB2Provider.ClientPackage];
#endif

	public override void ClearAllPools(string providerName)
	{
		// Net.IBM.Data.Db2 10.x has no safe replacement: ReleaseObjectPool is obsolete-as-error there
#if NETFRAMEWORK
		DB2Connection.ReleaseObjectPool();
#endif
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// Informix provides only table creation date without time, which is useless
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return DB2Factory.Instance;
	}
}
