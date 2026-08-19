using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SQLite;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;

using Microsoft.Data.Sqlite;

namespace LinqToDB.LINQPad;

internal sealed class SQLiteProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.SQLiteClassic, "Official Client (System.Data.SQLite)"   ),
		new(ProviderName.SQLiteMS,      "Microsof Client (Microsoft.Data.Sqlite)"),
	];

#if NETFRAMEWORK
	static SQLiteProvider()
	{
		// temporary, see SQLite.Runtime.props notes
		Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sds"));

		// data adapter not implemented by MS provider
		// https://github.com/dotnet/efcore/issues/13838
		// but needed for linqpad 5 to run sql
		// so we implement own factory class with adapter and use this
		// hack to force linqpad to use our GetProviderFactory implementation
		//
		// note: this is needed for SQL panel to work with MS provider
		typeof(SqliteFactory)
			.GetField("Instance", BindingFlags.Static | BindingFlags.Public)
			?.SetValue(null, null);
	}
#endif

	public SQLiteProvider()
		: base(ProviderName.SQLite, "SQLite", _providers)
	{
	}

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SQLiteClassic, StringComparison.Ordinal))
			return [("System.Data.SQLite", NuGetPackageVersions.System_Data_SQLite), ("SourceGear.sqlite3", NuGetPackageVersions.SourceGear_sqlite3)];

		return [("Microsoft.Data.Sqlite", NuGetPackageVersions.Microsoft_Data_Sqlite), ("SQLitePCLRaw.lib.e_sqlite3", NuGetPackageVersions.SQLitePCLRaw_lib_e_sqlite3)];
	}
#endif

	// each client is touched from its own non-inlined method: the assembly is loaded when a method
	// referencing its types is JIT-compiled, and only the packages of the provider the connection uses
	// are provisioned (see GetNuGetPackages), so a shared body would load the one that is missing
	public override void ClearAllPools(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SQLiteClassic, StringComparison.Ordinal))
			ClearClassicPools();
		else
			ClearMicrosoftPools();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ClearClassicPools() => SQLiteConnection.ClearAllPools();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ClearMicrosoftPools() => SqliteConnection.ClearAllPools();

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		// no information in schema
		return null;
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SQLiteClassic, StringComparison.Ordinal))
			return GetClassicFactory();
		else
#if NETFRAMEWORK
			return MsDbProviderFactory.Instance;
#else
			return GetMicrosoftFactory();
#endif
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetClassicFactory() => SQLiteFactory.Instance;

#if !NETFRAMEWORK
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetMicrosoftFactory() => SqliteFactory.Instance;
#endif

#if NETFRAMEWORK
	sealed class MsDbProviderFactory : DbProviderFactory
	{
		private MsDbProviderFactory()
		{
		}

		public static readonly MsDbProviderFactory Instance = new();

		public override DbCommand CreateCommand() => new SqliteCommand();

		public override DbConnection CreateConnection() => new SqliteConnection();

		public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new SqliteConnectionStringBuilder();

		public override DbParameter CreateParameter() => new SqliteParameter();

		public override DbDataAdapter CreateDataAdapter() => new SqliteDataAdapter();

		sealed class SqliteDataAdapter : DbDataAdapter;
	}
#endif
}
