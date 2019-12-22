using System;
using System.Data.Common;
using System.IO;
using System.Reflection;

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
#if NET46
		// recent SAP HANA provider uses Assembly.GetEntryAssembly() calls during native dlls discovery, which
		// leads to NRE as it returns null under NETFX, so we need to fake this method result to unblock HANA testing
		// https://github.com/microsoft/vstest/issues/1834
		// https://dejanstojanovic.net/aspnet/2015/january/set-entry-assembly-in-unit-testing-methods/
		var assembly = Assembly.GetCallingAssembly();

		var manager = new AppDomainManager();
		var entryAssemblyfield = manager.GetType().GetField("m_entryAssembly", BindingFlags.Instance | BindingFlags.NonPublic);
		entryAssemblyfield.SetValue(manager, assembly);

		var domain = AppDomain.CurrentDomain;
		var domainManagerField = domain.GetType().GetField("_domainManager", BindingFlags.Instance | BindingFlags.NonPublic);
		domainManagerField.SetValue(domain, manager);
#endif

		// netcoreapp2.1 adds DbProviderFactories support, but providers should be registered by application itself
		// this code allows to load assembly using factory without adding explicit reference to project
		RegisterSapHanaFactory();
		RegisterSqlCEFactory();

#if !NETCOREAPP2_1 && !AZURE
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
	}

	private void RegisterSapHanaFactory()
	{
#if NETCOREAPP2_1
		try
		{
			// woo-hoo, hardcoded pathes! default install location on x64 system
			var srcPath = @"c:\Program Files (x86)\sap\hdbclient\dotnetcore\v2.1\Sap.Data.Hana.Core.v2.1.dll";
			var targetPath = Path.Combine(System.AppDomain.CurrentDomain.BaseDirectory, Path.GetFileName(srcPath));
			if (File.Exists(srcPath))
			{
				// original path contains spaces which breaks broken native dlls discovery logic in SAP provider
				// if you run tests from path with spaces - it will not help you
				File.Copy(srcPath, targetPath, true);
				var sapHanaAssembly = Assembly.LoadFrom(targetPath);
				DbProviderFactories.RegisterFactory("Sap.Data.Hana", sapHanaAssembly.GetType("Sap.Data.Hana.HanaFactory"));
			}
		}
		catch { }
#endif
	}

	private void RegisterSqlCEFactory()
	{
#if NETCOREAPP2_1
		try
		{
			// default install pathes. Hardcoded for now as hardly anyone will need other location in near future
			var pathx64 = @"c:\Program Files\Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll";
			var pathx86 = @"c:\Program Files (x86)\Microsoft SQL Server Compact Edition\v4.0\Private\System.Data.SqlServerCe.dll";
			var path = IntPtr.Size == 4 ? pathx86 : pathx64;
			var assembly = Assembly.LoadFrom(path);
			DbProviderFactories.RegisterFactory("System.Data.SqlServerCe.4.0", assembly.GetType("System.Data.SqlServerCe.SqlCeProviderFactory"));
		}
		catch { }
#endif
	}

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
	}
}
