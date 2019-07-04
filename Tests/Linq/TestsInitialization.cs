﻿using System;
using System.Data.Common;
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
#if !NETSTANDARD1_6 && !NETSTANDARD2_0 && !APPVEYOR && !TRAVIS
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

		// disabled for core, as default loader doesn't allow multiple assemblies with same name
		// https://github.com/dotnet/coreclr/blob/master/Documentation/design-docs/assemblyloadcontext.md
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		Npgsql4PostgreSQLDataProvider.Init();
#endif
	}

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
	}
}
