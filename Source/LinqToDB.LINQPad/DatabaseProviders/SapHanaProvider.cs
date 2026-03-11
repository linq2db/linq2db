using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.Odbc;
using System.Linq;
using System.Reflection;

using LinqToDB.Data;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.DataProvider.SapHana;

#if !NETFRAMEWORK
using System.IO;
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
	}

	public override string? GetProviderDownloadUrl(string? providerName)
	{
		return "https://tools.hana.ondemand.com/#hanatools";
	}

	public override void ClearAllPools(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaOdbc, StringComparison.Ordinal))
			OdbcConnection.ReleaseObjectPool();
		else if (string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal))
		{
			foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
			{
				var typeName = $"{SapHanaProviderAdapter.UnmanagedClientNamespace}.HanaConnection, {assemblyName}";
				Type.GetType(typeName, false)?.GetMethod("ClearAllPools", BindingFlags.Public | BindingFlags.Static)?.InvokeExt(null, null);
			}
		}
	}

	public override DateTime? GetLastSchemaUpdate(ConnectionSettings settings)
	{
		using var db = new LINQPadDataConnection(settings);
		return db.Query<DateTime?>("SELECT MAX(time) FROM (SELECT MAX(CREATE_TIME) AS time FROM M_CS_TABLES UNION SELECT MAX(MODIFY_TIME) FROM M_CS_TABLES UNION SELECT MAX(CREATE_TIME) FROM M_RS_TABLES UNION SELECT MAX(CREATE_TIME) FROM PROCEDURES UNION SELECT MAX(CREATE_TIME) FROM FUNCTIONS)").FirstOrDefault();
	}

	public override DbProviderFactory? GetProviderFactory(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaOdbc, StringComparison.Ordinal))
			return OdbcFactory.Instance;

		foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
		{
			var typeName = $"{SapHanaProviderAdapter.UnmanagedProviderFactoryName}.HanaFactory, {assemblyName}";
			var instance = (DbProviderFactory?)Type.GetType(typeName, false)?.GetField("Instance", BindingFlags.Public | BindingFlags.Static)?.GetValue(null);

			if (instance != null)
				return instance;
		}

		return null;
	}

#if !NETFRAMEWORK
	public override bool IsProviderPathSupported(string providerName)
	{
		return string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal);
	}

	public override IEnumerable<string> GetProviderAssemblyNames(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal))
		{
			foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
			{
				yield return $"{assemblyName}.dll";
			}
		}
	}

	public override string? TryGetDefaultPath(string providerName)
	{
		if (string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal))
		{
			var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);

			if (!string.IsNullOrEmpty(programFiles))
			{
				foreach (var assemblyName in SapHanaProviderAdapter.UnmanagedAssemblyNames)
				{
					var version = assemblyName.Substring(assemblyName.IndexOf(".v", StringComparison.Ordinal) + 1);

					var path = Path.Combine(programFiles, $"sap\\hdbclient\\dotnetcore\\{version}\\{assemblyName}.dll");

					if (File.Exists(path))
						return path;
				}
			}
		}

		return null;
	}

	private static bool _factoryRegistered;
	public override void RegisterProviderFactory(string providerName, string providerPath)
	{
		if (string.Equals(providerName, ProviderName.SapHanaNative, StringComparison.Ordinal) && !_factoryRegistered)
		{
			if (!File.Exists(providerPath))
				throw new LinqToDBLinqPadException($"Cannot find SAP HANA provider assembly at '{providerPath}'");

			try
			{
				var sapHanaAssembly = Assembly.LoadFrom(providerPath);
				DbProviderFactories.RegisterFactory("Sap.Data.Hana", sapHanaAssembly.GetType("Sap.Data.Hana.HanaFactory")!);
				_factoryRegistered = true;
			}
			catch (Exception ex)
			{
				throw new LinqToDBLinqPadException($"Failed to initialize SAP HANA provider factory: ({ex.GetType().Name}) {ex.Message}");
			}
		}
	}
#endif
}
