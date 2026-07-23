using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using LinqToDB;
using LinqToDB.Common;
using LinqToDB.Data;

using NUnit.Framework;

using Tests.Tools;

namespace Tests
{
	public static class TestConfiguration
	{
		public   static string?         BaselinesPath        { get; }
		public   static bool?           StoreMetrics         { get; }
		// Configured cap on concurrent provider lanes for the parallel dispatcher; null => use the default.
		public   static int?            MaxParallelLanes     { get; }
		public   static bool            DisableRemoteContext { get; }
		public   static HashSet<string> UserProviders        { get; }
		internal static string?         DefaultProvider      { get; }
		internal static HashSet<string> SkipCategories       { get; }

		// Provider names passed via the --provider command-line option (see TestCommandLine), or null when absent.
		// For the main test set (UserProviders) they REPLACE the providers configured in UserDataProviders.json, so
		// any provider with a defined connection string can run without editing the file. For EFProviders they
		// INTERSECT the curated EF-Core-supported list instead (a provider with no EF context can't run EF tests,
		// so replacing there would just break). Declared above Providers/EFProviders so its initializer runs before
		// theirs: static field initializers run in textual order, ahead of the static constructor body.
		static readonly HashSet<string>? ProviderOverride =
			TestCommandLine.Providers.Count > 0
				? new HashSet<string>(TestCommandLine.Providers, StringComparer.OrdinalIgnoreCase)
				: null;

		// EFProviders keeps the intersection of its curated list and --provider (never an unsupported provider).
		static IEnumerable<string> ApplyEFProviderOverride(IEnumerable<string> providers) =>
			ProviderOverride is null ? providers : providers.Where(ProviderOverride.Contains);

		static TestConfiguration()
		{
			try
			{
				var assemblyPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;

				var dataProvidersJsonFile     = GetFilePath(assemblyPath, @"DataProviders.json")!;
				var userDataProvidersJsonFile = GetFilePath(assemblyPath, @"UserDataProviders.json")!;

				TestExternals.Log($"dataProvidersJsonFile     : {dataProvidersJsonFile}");
				TestExternals.Log($"userDataProvidersJsonFile : {userDataProvidersJsonFile}");

				var dataProvidersJson = File.ReadAllText(dataProvidersJsonFile);
				var userDataProvidersJson =
					File.Exists(userDataProvidersJsonFile) ? File.ReadAllText(userDataProvidersJsonFile) : null;

				var configName = TestUtils.GetConfigName();

#if AZURE
			TestContext.WriteLine("Azure configuration detected.");
			configName += ".Azure";
#endif

#if !DEBUG
#pragma warning disable RS0030 // Do not use banned APIs
				Console.WriteLine("UserDataProviders.json:");
				Console.WriteLine(userDataProvidersJson);
#pragma warning restore RS0030 // Do not use banned APIs
#endif

				var testSettings = SettingsReader.Deserialize(configName, dataProvidersJson, userDataProvidersJson);

				testSettings.Connections ??= new();

				DisableRemoteContext = testSettings.DisableRemoteContext == true;
				// --provider, when supplied, REPLACES the providers from UserDataProviders.json (see ProviderOverride):
				// any provider with a defined connection string can be run without editing the file.
				UserProviders = ProviderOverride is not null
					? new HashSet<string>(ProviderOverride, StringComparer.OrdinalIgnoreCase)
					: new HashSet<string>(testSettings.Providers ?? [], StringComparer.OrdinalIgnoreCase);

				if (ProviderOverride is not null)
				{
					TestContext.Out.WriteLine($"Provider override (--provider): {string.Join(", ", UserProviders)}");

					foreach (var p in UserProviders)
						if (!testSettings.Connections.Keys.Contains(p, StringComparer.OrdinalIgnoreCase))
							TestContext.Out.WriteLine($"WARNING: --provider '{p}' has no connection string defined; it will fail to connect.");
				}

				SkipCategories = new HashSet<string>(testSettings.Skip ?? [], StringComparer.OrdinalIgnoreCase);

				var logLevel   = testSettings.TraceLevel;
				var traceLevel = TraceLevel.Info;

				if (!string.IsNullOrEmpty(logLevel))
					if (!Enum.TryParse(logLevel, true, out traceLevel))
						traceLevel = TraceLevel.Info;

				if (!string.IsNullOrEmpty(testSettings.NoLinqService))
					DataSourcesBaseAttribute.NoLinqService = ConvertTo<bool>.From(testSettings.NoLinqService);

				DataConnection.TurnTraceSwitchOn(traceLevel);

				TestContext.Out.WriteLine("Connection strings:");
				TestExternals.Log("Connection strings:");

#if !NETFRAMEWORK
				TxtSettings.Instance.DefaultConfiguration = "SQLiteMs";

				foreach (var provider in testSettings.Connections/*.Where(c => UserProviders.Contains(c.Key))*/)
				{
					if (string.IsNullOrWhiteSpace(provider.Value.ConnectionString))
						throw new InvalidOperationException($"Provider: {provider.Key}. ConnectionString should be provided.");

					TestContext.Out.WriteLine($"\tName=\"{provider.Key}\", Provider=\"{provider.Value.Provider}\", ConnectionString=\"{provider.Value.ConnectionString}\"");

					TxtSettings.Instance.AddConnectionString(
						provider.Key, provider.Value.Provider ?? "", provider.Value.ConnectionString);
				}

				DataConnection.DefaultSettings = TxtSettings.Instance;
#else
				foreach (var provider in testSettings.Connections)
				{
					var str = $"\tName=\"{provider.Key}\", Provider=\"{provider.Value.Provider}\", ConnectionString=\"{provider.Value.ConnectionString}\"";

					TestContext.Out.WriteLine(str);
					TestExternals.Log(str);

					if (provider.Value.ConnectionString != null)
					{
						DataConnection.AddOrSetConfiguration(
							provider.Key,
							provider.Value.ConnectionString,
							provider.Value.Provider ?? "");
					}
				}
#endif

				TestContext.Out.WriteLine("Providers:");
				TestExternals.Log("Providers:");

				foreach (var userProvider in UserProviders)
				{
					TestContext.Out.WriteLine($"\t{userProvider}");
					TestExternals.Log($"\t{userProvider}");
				}

				DefaultProvider = testSettings.DefaultConfiguration;

				if (!string.IsNullOrEmpty(DefaultProvider))
				{
					DataConnection.DefaultConfiguration = DefaultProvider;
#if !NETFRAMEWORK
					TxtSettings.Instance.DefaultConfiguration = DefaultProvider;
#endif
				}

				// parallel lane cap (null => dispatcher uses its default)
				MaxParallelLanes = testSettings.MaxParallelLanes;

				// baselines
				if (!string.IsNullOrWhiteSpace(testSettings.BaselinesPath))
				{
					var baselinesPath = Path.GetFullPath(testSettings.BaselinesPath);

					if (Directory.Exists(baselinesPath))
					{
						BaselinesPath = baselinesPath;
						StoreMetrics = testSettings.StoreMetrics;
					}
				}
			}
			catch (Exception ex)
			{
				TestUtils.Log(ex);
				throw;
			}
		}

		static string? GetFilePath(string basePath, string findFileName)
		{
			var fileName = Path.GetFullPath(Path.Combine(basePath, findFileName));

			string? path = basePath;

			while (!File.Exists(fileName))
			{
				TestContext.Out.WriteLine($"File not found: {fileName}");

				path = Path.GetDirectoryName(path);

				if (path == null)
					return null;

				fileName = Path.GetFullPath(Path.Combine(path, findFileName));
			}

			TestContext.Out.WriteLine($"Base path found: {fileName}");

			return fileName;
		}

		internal static readonly IReadOnlyList<string> Providers = CustomizationSupport.Interceptor.GetSupportedProviders(new List<string>
		{
#if NETFRAMEWORK
			// test providers with .net framework provider only
			ProviderName.Sybase,
			TestProvName.AllOracleNative,
			ProviderName.Informix,
#endif
			// multi-tfm providers
			ProviderName.SqlCe,
			TestProvName.AllAccess,
			ProviderName.DB2,
			ProviderName.InformixDB2,
			TestProvName.AllSQLite,
			TestProvName.AllOracleManaged,
			TestProvName.AllOracleDevart,
			ProviderName.SybaseManaged,
			TestProvName.AllFirebird,
			TestProvName.AllSqlServer,
			TestProvName.AllPostgreSQL,
			TestProvName.AllMySql,
			TestProvName.AllSapHana,
			TestProvName.AllClickHouse,
			TestProvName.AllYdb,
			TestProvName.AllDuckDB,
		}.SplitAll()).ToList();

		public static readonly IReadOnlyList<string> EFProviders = ApplyEFProviderOverride(CustomizationSupport.Interceptor.GetSupportedProviders(new List<string>
		{
			ProviderName.SQLiteMS,
			// latest tested ef.core doesn't support older versions, leading to too many failing tests to disable
			TestProvName.AllSqlServer2016PlusMS,
			// latest tested ef.core doesn't support older versions, leading to too many failing tests to disable
			TestProvName.AllPostgreSQL13Plus,
#if !NET10_0 // provider need update for v10
			TestProvName.AllMySqlConnector,
#endif

#if NETFRAMEWORK
			// test providers with .net framework provider only
			//ProviderName.Sybase,
			//TestProvName.AllOracleNative,
			//ProviderName.Informix,
#endif
			// multi-tfm providers
			//ProviderName.SqlCe,
			//TestProvName.AllAccess,
			//ProviderName.DB2,
			//ProviderName.InformixDB2,
			//TestProvName.AllOracleManaged,
			//TestProvName.AllOracleDevart,
			//ProviderName.SybaseManaged,
			//TestProvName.AllFirebird,
			//TestProvName.AllSqlServer,
			//TestProvName.AllMySql,
			//TestProvName.AllSapHana,
			//TestProvName.AllClickHouse
		}.SplitAll())).ToList();
	}
}
