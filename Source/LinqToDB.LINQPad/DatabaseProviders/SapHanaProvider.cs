using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.SapHana;

#if !NETFRAMEWORK
using System.IO;
using System.Runtime.InteropServices;
#endif

namespace LinqToDB.LINQPad;

internal sealed class SapHanaProvider : DatabaseProviderBase
{
	private static readonly IReadOnlyList<ProviderInfo> _providers =
	[
		new(ProviderName.SapHanaNative, "Native Provider (Sap.Data.Hana)"),
		new(ProviderName.SapHanaOdbc  , "ODBC Provider (HANAODBC/HANAODBC32)"),
	];

	public SapHanaProvider()
		: base(ProviderName.SapHana, "SAP HANA", _providers)
	{
#if !NETFRAMEWORK
		AppDomain.CurrentDomain.AssemblyLoad += OnAssemblyLoad;

		if (AppDomain.CurrentDomain.GetAssemblies().Any(PointToPackageNatives))
			AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
#endif
	}

#if !NETFRAMEWORK
	private const string NativeClientPackage = "Sap.Data.Hana.Net.v8.0";

	public override IEnumerable<(string Id, string Version)> GetNuGetPackages(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaOdbc, StringComparison.Ordinal))
			return [("System.Data.Odbc", NuGetPackageVersions.System_Data_Odbc)];

		if (string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal))
			return [(NativeClientPackage, NuGetPackageVersions.Sap_Data_Hana_Net_v8_0)];

		return [];
	}

	// SAP's client searches for libadonetHDB around its own assembly and then in HDBDOTNETCORE, which a client
	// installation sets machine-wide to its own native - rejected by the client's exact version check. The
	// provisioned package keeps its natives in runtimes/ two levels up, so the process value is pointed there
	// before HanaConnection's type initializer runs that search; a failed one is sticky for the process.
	private static bool PointToPackageNatives(Assembly assembly)
	{
		if (assembly.FullName?.StartsWith(NativeClientPackage + ",", StringComparison.Ordinal) != true)
			return false;

		if (!string.IsNullOrEmpty(assembly.Location))
		{
			var nativePath = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(assembly.Location)!, "..", "..", "runtimes", RuntimeInformation.RuntimeIdentifier, "native"));

			if (Directory.Exists(nativePath))
				Environment.SetEnvironmentVariable("HDBDOTNETCORE", nativePath);
		}

		return true;
	}

	private static void OnAssemblyLoad(object? sender, AssemblyLoadEventArgs args)
	{
		if (PointToPackageNatives(args.LoadedAssembly))
			AppDomain.CurrentDomain.AssemblyLoad -= OnAssemblyLoad;
	}
#endif

	public override string? GetProviderDownloadUrl(string? providerName)
	{
		return "https://tools.hana.ondemand.com/#hanatools";
	}

	// the ODBC client is touched from its own non-inlined method: the assembly is loaded when a method
	// referencing its types is JIT-compiled, and the native variant does not provision System.Data.Odbc
	// (see GetNuGetPackages), so a shared body would load it for that variant too
	public override void ClearAllPools(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaOdbc, StringComparison.Ordinal))
			ReleaseOdbcPool();
		else if (string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal))
		{
			foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
			{
				var typeName = $"{SapHanaProviderAdapter.UnmanagedClientNamespace}.HanaConnection, {assemblyName}";
				Type.GetType(typeName, false)?.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static)?.InvokeExt(null, null);
			}
		}
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static void ReleaseOdbcPool() => OdbcConnection.ReleaseObjectPool();

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(time) FROM (SELECT MAX(CREATE_TIME) AS time FROM M_CS_TABLES UNION SELECT MAX(MODIFY_TIME) FROM M_CS_TABLES UNION SELECT MAX(CREATE_TIME) FROM M_RS_TABLES UNION SELECT MAX(CREATE_TIME) FROM PROCEDURES UNION SELECT MAX(CREATE_TIME) FROM FUNCTIONS)").FirstOrDefault();
	}

	public override DbProviderFactory? GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaOdbc, StringComparison.Ordinal))
			return GetOdbcFactory();

		foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
		{
			var typeName = $"{SapHanaProviderAdapter.UnmanagedProviderFactoryName}.HanaFactory, {assemblyName}";
			var instance = (DbProviderFactory?)Type.GetType(typeName, false)?.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

			if (instance != null)
				return instance;
		}

		return null;
	}

	[MethodImpl(MethodImplOptions.NoInlining)]
	private static DbProviderFactory GetOdbcFactory() => OdbcFactory.Instance;
}
