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
using NUnit.Framework.Internal;
using NUnit.Framework.Internal.Execution;
using NUnit.ParallelByResource;

using Oracle.ManagedDataAccess.Client;

using Tests;

// Mark every fixture/test parallelizable so NUnit assigns the Parallel execution strategy
// and dispatches each case. The actual concurrency is governed by ResourceLaneDispatcher
// (installed in OneTimeSetUp): cases are routed to per-provider lanes so same-database tests
// never overlap. Tests that mutate global state are excluded via [NonParallelizable].
[assembly: Parallelizable(ParallelScope.All)]

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
						throw new InvalidOperationException("Failed to get value of '_SQLiteNativeModuleHandle'.");
					}
				}

				return handle.Value;
			}

			return IntPtr.Zero;
		});

		// DB2/Informix on Linux load their native client (libdb2.so) from the clidriver that
		// Build/Azure/scripts/db2.provider.sh extracts next to the test binaries. CI exports
		// LD_LIBRARY_PATH=clidriver/lib, but that env var doesn't reliably propagate to the
		// testhost subprocess, so the native load intermittently fails. Resolve libdb2.so
		// explicitly from the known clidriver path. See https://github.com/linq2db/linq2db/issues/5538
		if (OperatingSystem.IsLinux())
		{
			IntPtr db2Handle = default;
			System.Runtime.InteropServices.NativeLibrary.SetDllImportResolver(typeof(IBM.Data.Db2.DB2Connection).Assembly, (module, assembly, searchPath) =>
			{
				if (module == "libdb2.so")
				{
					if (db2Handle == default)
					{
						var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "clidriver", "lib", "libdb2.so");
						if (File.Exists(path))
							db2Handle = System.Runtime.InteropServices.NativeLibrary.Load(path);
					}

					if (db2Handle != default)
						return db2Handle;
				}

				return IntPtr.Zero;
			});
		}
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

		// Cap the process-wide query cache to bound test-process memory. The CI NETFX legs run two
		// SQL Server versions' full suites in a single process and were hitting OutOfMemoryException;
		// the default cap is 10000 entries but a full run only produces ~1700 distinct queries, so a
		// small cap trims retained compiled-query memory with no correctness impact. Overridable via
		// the L2DB_TEST_QUERYCACHE env var for tuning.
		{
			var qcMax = Environment.GetEnvironmentVariable("L2DB_TEST_QUERYCACHE") is { } qcMaxStr && int.TryParse(qcMaxStr, out var n) ? n : 100;
			LinqToDB.Internal.Linq.QueryCache.Default.MaxEntriesOverride = qcMax;
		}

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

		// Parallelize tests across database providers: route each provider's tests to a
		// dedicated lane so the same database is never hit concurrently. Only swap when
		// NUnit is actually running in parallel; a serial run keeps the original dispatcher.
		var configuredLanes = TestConfiguration.MaxParallelLanes;
		var maxLanes        = configuredLanes ?? (2 * Environment.ProcessorCount);
		if (maxLanes < 1)
			maxLanes = 1;

		if (ResourceLaneDispatcherInstaller.TryInstall(new DatabaseLaneStrategy(), new DelegateParallelDiagnostics(ParallelDiag.Log), maxLanes, out var workers))
		{
			TestBase.ParallelExecutionEnabled = true;
			TestContext.Progress.WriteLine($"[parallel] installed ResourceLaneDispatcher (maxLanes={maxLanes} [{(configuredLanes.HasValue ? "from config" : "default 2xCPU")}], cpus={Environment.ProcessorCount}, nunitWorkers={workers})");
		}
		else
		{
			TestContext.Progress.WriteLine($"[parallel] not installed; dispatcher is {TestExecutionContext.CurrentContext.Dispatcher?.GetType().Name ?? "null"}");
		}

		// Set up in-memory databases (SQLite/DuckDB) for any provider configured with an in-memory
		// connection string (CI), before a_CreateData seeds them. No-op for the normal file-based setup.
		SetupInMemoryDatabases();
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

	// Route SQLite/DuckDB CI runs to in-memory databases (connection strings in Build/Azure/*/sqlite.json
	// and duckdb.json) to avoid the per-commit filesystem sync that dominates their on-disk runs. Both use
	// a shared-cache in-memory connection string (SQLite ...cache=shared, DuckDB :memory:?cache=shared)
	// that is shared across all connections, so each only needs one keep-alive connection held open for
	// the run (the DB is destroyed once its last connection closes) — registered in TestInMemoryDatabases.
	// Auto-activates per config only when its connection string is in-memory; a complete no-op for the
	// normal file-based (dev) setup.
	static void SetupInMemoryDatabases()
	{
		SetupSqliteInMemory();
#if !NETFRAMEWORK
		SetupDuckDBInMemory();
#endif
	}

	static void SetupSqliteInMemory()
	{
		// The SQLite configs the CI job enables, each with its committed on-disk source file. `classic`
		// picks the ADO provider (System.Data.SQLite vs Microsoft.Data.Sqlite).
		var configs = new (string name, bool classic, string sourceFile)[]
		{
			("SQLite.Classic",      true,  "TestData.sqlite"),
			("SQLite.Classic.MPU",  true,  "TestData.MiniProfiler.Unmapped.sqlite"),
			("SQLite.Classic.MPM",  true,  "TestData.MiniProfiler.Mapped.sqlite"),
			("SQLite.MS",           false, "TestData.MS.sqlite"),
			("Northwind.SQLite",    true,  "Northwind.sqlite"),
			("Northwind.SQLite.MS", false, "Northwind.MS.sqlite"),
		};

		foreach (var (name, classic, sourceFile) in configs)
		{
			string cs;
			try { cs = LinqToDB.Data.DataConnection.GetConnectionString(name); }
			catch { continue; }

			if (cs == null || cs.IndexOf("mode=memory", StringComparison.OrdinalIgnoreCase) < 0)
				continue; // file-based -> nothing to keep alive

			DbConnection keep = classic
				? new System.Data.SQLite.SQLiteConnection(cs)
				: new Microsoft.Data.Sqlite.SqliteConnection(cs);
			keep.Open();
			TestInMemoryDatabases.AddKeepAlive(keep);

			// Preload the in-memory DB from its committed on-disk file via the SQLite online-backup API,
			// so a filtered run (which skips the a_CreateData seeding) still has tables and data. Full
			// runs re-seed TestData on top; Northwind (no SQL seed script) is only ever loaded this way.
			var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Database", sourceFile);
			if (File.Exists(path))
			{
				if (classic)
				{
					using var src = new System.Data.SQLite.SQLiteConnection($"Data Source={path};Read Only=True");
					src.Open();
					src.BackupDatabase((System.Data.SQLite.SQLiteConnection)keep, "main", "main", -1, null, 0);
				}
				else
				{
					using var src = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path};Mode=ReadOnly");
					src.Open();
					src.BackupDatabase((Microsoft.Data.Sqlite.SqliteConnection)keep);
				}
			}

			TestContext.Progress.WriteLine($"[sqlite-inmemory] keep-alive open for {name} ({cs})");
		}
	}

#if !NETFRAMEWORK
	static void SetupDuckDBInMemory()
	{
		string cs;
		try { cs = LinqToDB.Data.DataConnection.GetConnectionString("DuckDB"); }
		catch { return; }

		if (cs == null || cs.IndexOf(":memory:", StringComparison.OrdinalIgnoreCase) < 0)
			return; // file-based -> nothing to do

		// DuckDB in-memory is CI-only (the tracked DataProviders.json default stays file-based). Unlike
		// SQLite, DuckDB can't preload the in-memory DB from its committed on-disk file (COPY FROM DATABASE
		// fails on FK ordering), so the shared in-memory catalog is only ever seeded by a_CreateData. CI
		// always runs the full suite, so a_CreateData seeds it first; a filtered run against an in-memory
		// DuckDB would see an empty DB — which is why the default is left file-based for local/filtered runs.

		// A DuckDB shared-cache in-memory database (Data Source=:memory:?cache=shared) is shared by every
		// connection using the same string — exactly like SQLite — so we only need to hold one master
		// connection open; the database is destroyed once its last connection closes.
		var master = new DuckDB.NET.Data.DuckDBConnection(cs);
		master.Open();
		TestInMemoryDatabases.AddKeepAlive(master);

		TestContext.Progress.WriteLine($"[duckdb-inmemory] keep-alive open ({cs})");
	}
#endif

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
		TestInMemoryDatabases.DisposeAll();

		ParallelDiag.Dump();

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
