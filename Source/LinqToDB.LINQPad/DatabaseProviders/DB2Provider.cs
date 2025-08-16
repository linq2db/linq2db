using LinqToDB.Data;

using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;

#if WITH_ISERIES
using LinqToDB.DataProvider.DB2iSeries;
#endif

#if NETFRAMEWORK
using IBM.Data.DB2;

using System.IO;
using System.Reflection;
#else
using IBM.Data.Db2;
#endif

namespace LinqToDB.LINQPad;

internal sealed class DB2Provider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.DB2LUW       , "DB2 for Linux, UNIX and Windows (LUW)"),
		// zOS provider not tested at all as we don't have access to database instance
		new (ProviderName.DB2zOS       , "DB2 for z/OS"                         ),
#if WITH_ISERIES
		new (DB2iSeriesProviderName.DB2, "DB2 for i (iSeries)"                  ),
#endif
	];

	public DB2Provider()
		: base(ProviderName.DB2, "IBM DB2 (LUW, z/OS or iSeries)", _providers)
	{
#if WITH_ISERIES
		DataConnection.AddProviderDetector(DB2iSeriesTools.ProviderDetector);
#endif
	}

#if NETFRAMEWORK
	internal static void LoadAssembly()
	{
		var assemblyPath = Path.Combine(Path.GetDirectoryName(typeof(DB2Provider).Assembly.Location), "IBM.Data.DB2.DLL_provider", IntPtr.Size == 4 ? "x86" : "x64", $"IBM.Data.DB2.dll");
		if (!File.Exists(assemblyPath))
			throw new LinqToDBLinqPadException($"Failed to locate IBM.Data.DB2 assembly at {assemblyPath}");

		try
		{
			_ = Assembly.LoadFrom(assemblyPath);
		}
		catch (Exception ex)
		{
			throw new LinqToDBLinqPadException($"Failed to load IBM.Data.DB2 assembly: ({ex.GetType().Name}) {ex.Message}");
		}
	}
#endif

	public override void ClearAllPools(string providerName)
	{
		DB2Connection.ReleaseObjectPool();
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		var sql = settings.Connection.Provider switch
		{
			ProviderName.DB2LUW        => "SELECT MAX(TIME) FROM (SELECT MAX(ALTER_TIME) AS TIME FROM SYSCAT.ROUTINES UNION SELECT MAX(ALTER_TIME) AS TIME FROM SYSCAT.TABLES)",
			ProviderName.DB2zOS        => "SELECT MAX(TIME) FROM (SELECT MAX(ALTEREDTS) AS TIME FROM SYSIBM.SYSROUTINES UNION SELECT MAX(ALTEREDTS) AS TIME FROM SYSIBM.SYSTABLES)",
#if WITH_ISERIES
			DB2iSeriesProviderName.DB2 => "SELECT MAX(TIME) FROM (SELECT MAX(LAST_ALTERED) AS TIME FROM QSYS2.SYSROUTINES UNION SELECT MAX(ROUTINE_CREATED) AS TIME FROM QSYS2.SYSROUTINES UNION SELECT MAX(LAST_ALTERED_TIMESTAMP) AS TIME FROM QSYS2.SYSTABLES)",
#endif
			_                          => throw new LinqToDBLinqPadException($"Unknown DB2 provider '{settings.Connection.Provider}'")
		};

		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>(sql).FirstOrDefault();
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		return DB2Factory.Instance;
	}
}
