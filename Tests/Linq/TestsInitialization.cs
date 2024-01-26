﻿using System;
using System.Data.Common;
using System.Diagnostics;
using System.Reflection;

using LinqToDB.Common;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.Tools;
using LinqToDB.Tools.Activity;

using NUnit.Framework;

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
		_doMetrics = TestBase.StoreMetrics == true;
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
		var destPath             = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, runtimeFile);
		var sourcePath           = System.IO.Path.Combine(
			AppDomain.CurrentDomain.BaseDirectory,
			"runtimes",
			IntPtr.Size == 4 ? "win-x86" : "win-x64",
			"native",
			runtimeFile);

		System.IO.File.Copy(sourcePath, destPath, true);
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

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
		if (_doMetrics)
		{
			var str = ActivityStatistics.GetReport();

			Debug.WriteLine(str);
			TestContext.Progress.WriteLine(str);

			if (TestBase.StoreMetrics == true)
				BaselinesWriter.WriteMetrics(TestBase.BaselinesPath!, str);
		}
	}
}
