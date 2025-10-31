using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Metrics;
using LinqToDB.Tools.Activity;

using NUnit.Framework;

using Oracle.ManagedDataAccess.Client;

using Tests;

/// <summary>
/// 1. Don't add namespace to this class! It's intentional
/// 2. This class implements test assembly setup/teardown methods.
/// </summary>
[SetUpFixture]
public class TestsInitialization
{
	static bool _doMetrics;

	[OneTimeSetUp]
	public void TestAssemblySetup()
	{
		// temporary, see SQLite.Runtime.props notes
		Environment.SetEnvironmentVariable("PreLoadSQLite_BaseDirectory", Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "sds"));

#if NET8_0_OR_GREATER
		// this API is not available in NETFX, but for some reason it works if SDS test run first (which is true now)
		IntPtr? handle = null;
		System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(System.Data.SQLite.AssemblySourceIdAttribute).Assembly, (module, assembly, searchPath) =>
		{
			if (module == "e_sqlite3")
			{
				if (handle == null)
				{
					// This code targets System.Data.SQLite version 2.0.2
					var type = assembly.GetType("System.Data.SQLite.UnsafeNativeMethods");
					if (type == null)
					{
						throw new InvalidOperationException($"Failed to find type 'System.Data.SQLite.UnsafeNativeMethods' in assembly '{assembly.FullName}'.");
					}

					var field = type.GetField("_SQLiteNativeModuleHandle", BindingFlags.Static | BindingFlags.NonPublic);
					if (field == null)
					{
						throw new InvalidOperationException($"Failed to find field '_SQLiteNativeModuleHandle' in type '{type.FullName}'.");
					}

					handle = field.GetValue(null) as IntPtr?;
					if (handle == null)
					{
						throw new InvalidOperationException($"Failed to get value of '_SQLiteNativeModuleHandle'.");
					}
				}

				return handle.Value;
			}

			return IntPtr.Zero;
		});
#else
		// force load of SDS runtime first as there is no SetDllImportResolver API
		using (var _ = new System.Data.SQLite.SQLiteConnection("", false))
		{ }
#endif

#if DEBUG
		ActivityService.AddFactory(ActivityHierarchyFactory);

		static IActivity? ActivityHierarchyFactory(ActivityID activityID)
		{
			var ctx = CustomTestContext.Get();

			if (ctx.Get<bool>(CustomTestContext.TRACE_DISABLED) != true)
				return new ActivityHierarchy(activityID, s => Debug.WriteLine(s));
			return null;
		}

		_doMetrics = true;
#else
		_doMetrics = TestConfiguration.StoreMetrics == true;
#endif

		if (_doMetrics)
		{
			Configuration.TraceMaterializationActivity = true;
			ActivityService.AddFactory(ActivityStatistics.Factory);
		}

		// required for tests expectations
		ClickHouseOptions.Default = ClickHouseOptions.Default with { UseStandardCompatibleAggregates = true };

		// uncomment it to run tests with SeqentialAccess command behavior
		//LinqToDB.Common.Configuration.OptimizeForSequentialAccess = true;
		//DbCommandProcessorExtensions.Instance = new SequentialAccessCommandProcessor();

		// netcoreapp2.1 adds DbProviderFactories support, but providers should be registered by application itself
		// this code allows to load assembly using factory without adding explicit reference to project
		RegisterSqlCEFactory();

		// enable ora11 protocol with v23 client
		OracleConfiguration.SqlNetAllowedLogonVersionClient = OracleAllowedLogonVersionClient.Version11;

#if NETFRAMEWORK && !AZURE
		// configure assembly redirect for referenced assemblies to use version from GAC
		// this solves exception from provider-specific tests, when it tries to load version from redist folder
		// but loaded from GAC assembly has other version
		AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
		{
			var requestedAssembly = new AssemblyName(args.Name);

			if (requestedAssembly.Name == "IBM.Data.DB2")
				return DbProviderFactories.GetFactory("IBM.Data.DB2").GetType().Assembly;

			if (requestedAssembly.Name == "IBM.Data.Informix")
			{
				// chose your red or blue pill carefully
				//return DbProviderFactories.GetFactory("IBM.Data.Informix").GetType().Assembly;
#pragma warning disable CS0618 // Type or member is obsolete
				return typeof(IBM.Data.Informix.IfxTimeSpan).Assembly;
#pragma warning restore CS0618 // Type or member is obsolete
			}

			return null;
		};
#endif

		// register test providers
		TestNoopProvider.Init();
		SQLiteMiniprofilerProvider.Init();

		// uncomment to run FEC for all tests and comment reset line in TestBase.OnAfterTest
		//LinqToDB.Common.Compilation.SetExpressionCompiler(_ => FastExpressionCompiler.ExpressionCompiler.CompileFast(_, true));

		//custom initialization logic
		CustomizationSupport.Init();
	}

	private void RegisterSqlCEFactory()
	{
#if !NETFRAMEWORK
		try
		{
			// default install pathes. Hardcoded for now as hardly anyone will need other location in near future
			var pathx64 = @"c:\Program Files\Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll";
			var pathx86 = @"c:\Program Files (x86)\Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll";
			var path = IntPtr.Size == 4 ? pathx86 : pathx64;
			var assembly = Assembly.LoadFrom(path);
			DbProviderFactories.RegisterFactory("System.Data.SqlServerCe.4.0", assembly.GetType("System.Data.SqlServerCe.SqlCeProviderFactory")!);
		}
		catch { }
#endif
	}

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
		if (_doMetrics)
		{
			var str = ActivityStatistics.GetReport();

			Debug.WriteLine(str);
			TestContext.Progress.WriteLine(str);

			if (TestConfiguration.StoreMetrics == true)
				BaselinesWriter.WriteMetrics(TestConfiguration.BaselinesPath!, str);
		}
	}
}
