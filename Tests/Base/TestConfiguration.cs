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
		public   static bool            DisableRemoteContext { get; }
		public   static HashSet<string> UserProviders        { get; }
		internal static string?         DefaultProvider      { get; }
		internal static HashSet<string> SkipCategories       { get; }

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
				Console.WriteLine("UserDataProviders.json:");
				Console.WriteLine(userDataProvidersJson);
#endif

				var testSettings = SettingsReader.Deserialize(configName, dataProvidersJson, userDataProvidersJson);

				testSettings.Connections ??= new();

				DisableRemoteContext = testSettings.DisableRemoteContext == true;
				UserProviders = new HashSet<string>(testSettings.Providers ?? [], StringComparer.OrdinalIgnoreCase);
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
			TestProvName.AllClickHouse
		}.SplitAll()).ToList();

		public static readonly IReadOnlyList<string> EFProviders = CustomizationSupport.Interceptor.GetSupportedProviders(new List<string>
		{
			ProviderName.SQLiteMS,
			// latest tested ef.core doesn't support older versions, leading to too many failing tests to disable
			TestProvName.AllSqlServer2016PlusMS,
			// latest tested ef.core doesn't support older versions, leading to too many failing tests to disable
			TestProvName.AllPostgreSQL13Plus,
			TestProvName.AllMySqlConnector,

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
		}.SplitAll()).ToList();
	}
}
