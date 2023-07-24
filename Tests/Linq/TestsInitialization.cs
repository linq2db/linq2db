using System;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;

using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Tools;

using NUnit.Framework;

using Tests;

/// <summary>
/// 1. Don't add namespace to this class! It's intentional
/// 2. This class implements test assembly setup/teardown methods.
/// </summary>
[SetUpFixture]
public class TestsInitialization
{
	[OneTimeSetUp]
	public void TestAssemblySetup()
	{
#if METRICS
		_testMetricWatcher = Metrics.TestTotal.Start();
#endif

		// required for tests expectations
		ClickHouseOptions.Default = ClickHouseOptions.Default with { UseStandardCompatibleAggregates = true };

		// uncomment it to run tests with SeqentialAccess command behavior
		//LinqToDB.Common.Configuration.OptimizeForSequentialAccess = true;
		//DbCommandProcessorExtensions.Instance = new SequentialAccessCommandProcessor();

		// netcoreapp2.1 adds DbProviderFactories support, but providers should be registered by application itself
		// this code allows to load assembly using factory without adding explicit reference to project
		CopySQLiteRuntime();
		RegisterSqlCEFactory();

#if NET472 && !AZURE
		// configure assembly redirect for referenced assemblies to use version from GAC
		// this solves exception from provider-specific tests, when it tries to load version from redist folder
		// but loaded from GAC assembly has other version
		AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
		{
			var requestedAssembly = new AssemblyName(args.Name);

			if (requestedAssembly.Name == "IBM.Data.DB2")
				return DbProviderFactories.GetFactory("IBM.Data.DB2").GetType().Assembly;

			if (requestedAssembly.Name == "IBM.Data.Informix")
				// chose your red or blue pill carefully
				//return DbProviderFactories.GetFactory("IBM.Data.Informix").GetType().Assembly;
				return typeof(IBM.Data.Informix.IfxTimeSpan).Assembly;

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

	// workaround for
	// https://github.com/ericsink/SQLitePCL.raw/issues/389
	// https://github.com/dotnet/efcore/issues/19396
	private void CopySQLiteRuntime()
	{
#if NET472
		const string runtimeFile = "e_sqlite3.dll";
		var destPath             = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, runtimeFile);
		var sourcePath           = Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"runtimes",
			IntPtr.Size == 4 ? "win-x86" : "win-x64",
			"native",
			runtimeFile);

		File.Copy(sourcePath, destPath, true);
#endif
	}

	private void RegisterSqlCEFactory()
	{
#if !NET472
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

#if METRICS
	Metric.Watcher? _testMetricWatcher;
#endif

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
#if METRICS
		_testMetricWatcher?.Dispose();

		var str = Metrics.All.Select(m => new
		{
			m.Name,
			m.Elapsed,
			m.CallCount,
			TimePerCall = m.CallCount switch
			{
				0 => TimeSpan.Zero,
				1 => m.Elapsed,
				_ => new TimeSpan(m.Elapsed.Ticks / m.CallCount)
			}
		})
		.ToDiagnosticString();

		Console.    WriteLine(str);
		Debug.      WriteLine(str);
		TestContext.WriteLine(str);
#else
		var str = "Metrics are off";
		Console.    WriteLine(str);
		Debug.      WriteLine(str);
		TestContext.WriteLine(str);
#endif
	}
}
