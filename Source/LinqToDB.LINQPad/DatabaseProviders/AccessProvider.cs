using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Data.Odbc;
using System.Data.OleDb;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using LinqToDB.DataProvider;
using LinqToDB.Internal.DataProvider;

namespace LinqToDB.LINQPad;

internal sealed class AccessProvider : DatabaseProviderBase
{
#if !NETFRAMEWORK
	private const string LibRedTroubleshoot = "LibRed is a managed Access engine that ships for .NET 11 only: the query must run on .NET 11 or newer.";
#endif

	// OLE DB is not implemented outside of Windows and there is no ODBC driver for Access on other systems;
	// hidden rather than removed there, so existing connections still load. LibRed.Ado is net11.0-only, and
	// LINQPad 5 cannot use it at all.
	// Each of OLE DB and ODBC reports a schema defect the other does not (see MergedAccessSchemaProvider), so
	// the paired entries read schema from both while queries run on the first.
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.Access      , "OLE DB"                        , IsDefault: Platform.IsWindows, IsHidden: !Platform.IsWindows),
		new (ProviderName.AccessOdbc  , "ODBC"                                                         , IsHidden: !Platform.IsWindows),
		new (ProviderName.Access      , "OLE DB, with ODBC schema merged"                              , IsHidden: !Platform.IsWindows, SecondaryName: ProviderName.AccessOdbc),
		new (ProviderName.AccessOdbc  , "ODBC, with OLE DB schema merged"                              , IsHidden: !Platform.IsWindows, SecondaryName: ProviderName.Access),
#if !NETFRAMEWORK
		new (ProviderName.AccessLibRed, "LibRed (managed)"              , IsDefault: !Platform.IsWindows, Troubleshoot: LibRedTroubleshoot, MinimumRuntime: 11),
#endif
	];

	public AccessProvider()
		: base(ProviderName.Access, "Microsoft Access", _providers)
	{
	}

	public override bool SupportsSecondaryConnection => true;

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName)
	{
		if (string.Equals(providerName, ProviderName.AccessLibRed, StringComparison.Ordinal))
			return [("LibRed.Ado", NuGetPackageVersions.LibRed_Ado)];

		if (string.Equals(providerName, ProviderName.AccessOdbc, StringComparison.Ordinal))
			return [("System.Data.Odbc", NuGetPackageVersions.System_Data_Odbc)];

		return [("System.Data.OleDb", NuGetPackageVersions.System_Data_OleDb)];
	}
#endif

	public override string? GetProviderDownloadUrl(string? providerName)
	{
		if (string.Equals(providerName, ProviderName.AccessLibRed, StringComparison.Ordinal))
			return null;

		return "https://www.microsoft.com/en-us/download/details.aspx?id=54920";
	}

	// each client is touched from its own non-inlined method: the assembly is loaded when a method
	// referencing its types is JIT-compiled, and only the packages of the provider the connection uses
	// are provisioned (see GetNuGetPackages), so a shared body would load the one that is missing
	public override void ClearAllPools(string providerName)
	{
		if (Platform.IsWindows && string.Equals(providerName, ProviderName.Access, StringComparison.Ordinal))
			ReleaseOleDbPool();

		if (string.Equals(providerName, ProviderName.AccessOdbc, StringComparison.Ordinal))
			ReleaseOdbcPool();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ReleaseOleDbPool() => OleDbConnection.ReleaseObjectPool();

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ReleaseOdbcPool() => OdbcConnection.ReleaseObjectPool();

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		var connectionString = string.Equals(settings.Connection.Provider, ProviderName.Access, StringComparison.Ordinal) ? settings.Connection.GetFullConnectionString()
			: string.Equals(settings.Connection.SecondaryProvider, ProviderName.Access, StringComparison.Ordinal) ? settings.Connection.GetFullSecondaryConnectionString()
				: null;

		if (connectionString == null || !Platform.IsWindows)
			return null;

		// only OLE DB schema has required information
		IDataProvider provider;
		if (string.Equals(settings.Connection.Provider, ProviderName.Access, StringComparison.Ordinal))
			provider = DatabaseProviders.GetDataProvider(settings);
		else
			provider = DatabaseProviders.GetDataProvider(settings.Connection.SecondaryProvider, connectionString, null);

		return ReadOleDbSchemaUpdate(provider, connectionString);
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DateTime? ReadOleDbSchemaUpdate(IDataProvider provider, string connectionString)
	{
		using var cn = (OleDbConnection)provider.CreateConnection(connectionString);
		cn.Open();

		var dt1 = cn.GetSchema("Tables"    ).Rows.Cast<DataRow>().Select(static r => (DateTime)r["DATE_MODIFIED"]).Concat([default]).Max();
		var dt2 = cn.GetSchema("Procedures").Rows.Cast<DataRow>().Select(static r => (DateTime)r["DATE_MODIFIED"]).Concat([default]).Max();
		return dt1 > dt2 ? dt1 : dt2;
	}

#if !NETFRAMEWORK
	public override IDataProvider GetDataProvider(string providerName, string connectionString)
	{
		if (string.Equals(providerName, ProviderName.AccessLibRed, StringComparison.Ordinal) && Environment.Version.Major < 11)
			throw new LinqToDBLinqPadException($"{LibRedTroubleshoot} This query runs on .NET {Environment.Version}.");

		return base.GetDataProvider(providerName, connectionString);
	}
#endif

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.AccessLibRed, StringComparison.Ordinal))
			return GetLibRedFactory();

		if (string.Equals(providerName, ProviderName.AccessOdbc, StringComparison.Ordinal))
			return GetOdbcFactory();

		return GetOleDbFactory();
	}

	// no compile-time reference: LibRed.Ado is net11.0-only, and linq2db already locates the assembly
	private static DbProviderFactory GetLibRedFactory()
	{
		var factoryType = LibRedProviderAdapter.GetInstance().ConnectionType.Assembly.GetType($"{LibRedProviderAdapter.ClientNamespace}.LibRedFactory", true)!;
		return (DbProviderFactory)factoryType.GetField("Instance")!.GetValue(null)!;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetOdbcFactory() => OdbcFactory.Instance;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetOleDbFactory() => OleDbFactory.Instance;
}
