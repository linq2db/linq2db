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

namespace LinqToDB.LINQPad;

internal sealed class AccessProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new (ProviderName.Access    , "OLE DB"),
		new (ProviderName.AccessOdbc, "ODBC"  ),
	];

	public AccessProvider()
		: base(ProviderName.Access, "Microsoft Access", _providers)
	{
	}

	public override bool SupportsSecondaryConnection => true;
	public override bool AutomaticProviderSelection  => true;
	// OLE DB is not implemented outside of Windows and there is no ODBC driver for Access on other systems
	public override bool IsPlatformSupported         => Platform.IsWindows;

#if !NETFRAMEWORK
	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName)
	{
		if (string.Equals(providerName, ProviderName.AccessOdbc, StringComparison.Ordinal))
			return [("System.Data.Odbc", NuGetPackageVersions.System_Data_Odbc)];

		return [("System.Data.OleDb", NuGetPackageVersions.System_Data_OleDb)];
	}
#endif

	public override string? GetProviderDownloadUrl(string? providerName)
	{
		return "https://www.microsoft.com/en-us/download/details.aspx?id=54920";
	}

	// each client is touched from its own non-inlined method: the assembly is loaded when a method
	// referencing its types is JIT-compiled, and only the packages of the provider the connection uses
	// are provisioned (see GetNuGetPackages), so a shared body would load the one that is missing
	public override void ClearAllPools(string providerName)
	{
		if (OperatingSystem.IsWindows() && string.Equals(providerName, ProviderName.Access, StringComparison.Ordinal))
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

		if (connectionString == null || !OperatingSystem.IsWindows())
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

	public override ProviderInfo? GetProviderByConnectionString(string connectionString)
	{
		connectionString = PasswordManager.ResolvePasswordManagerFields(connectionString);

		var isOleDb = connectionString.Contains("Microsoft.Jet.OLEDB", StringComparison.OrdinalIgnoreCase)
			|| connectionString.Contains("Microsoft.ACE.OLEDB", StringComparison.OrdinalIgnoreCase);

		// we don't check for ODBC provider marker - it will fail on connection test if wrong
		return _providers[isOleDb ? 0 : 1];
	}

	public override DbProviderFactory GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.AccessOdbc, StringComparison.Ordinal))
			return GetOdbcFactory();

		return GetOleDbFactory();
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetOdbcFactory() => OdbcFactory.Instance;

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetOleDbFactory() => OleDbFactory.Instance;
}
