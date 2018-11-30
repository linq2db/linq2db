using System;
using System.Data.Common;
using System.IO;
using System.Reflection;
using LinqToDB.Data;
using LinqToDB.DataProvider.PostgreSQL;
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
#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		// configure assembly redirect for referenced assemblies to use version from GAC
		// this solves exception from provider-specific tests, when it tries to load version from redist folder
		// but loaded from GAC assembly has other version
		AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
		{
			var requestedAssembly = new AssemblyName(args.Name);
			if (requestedAssembly.Name == "IBM.Data.DB2")
				return DbProviderFactories.GetFactory("IBM.Data.DB2").GetType().Assembly;
			if (requestedAssembly.Name == "IBM.Data.Informix")
				return DbProviderFactories.GetFactory("IBM.Data.Informix").GetType().Assembly;

			return null;
		};
#endif

		// register test provider
		TestNoopProvider.Init();

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
		if (File.Exists("npgsql4/npgsql.dll"))
		{
			DataConnection.AddDataProvider(new EdgePostgreSQLDataProvider());
		}
#endif
	}

	[OneTimeTearDown]
	public void TestAssemblyTeardown()
	{
	}

#if !NETSTANDARD1_6 && !NETSTANDARD2_0
	internal class EdgePostgreSQLDataProvider : PostgreSQLDataProvider
	{
		private Type  _dataReaderType;
		volatile Type _connectionType;

		private readonly Assembly _assembly;

		protected override string ConnectionTypeName { get { return "Npgsql.NpgsqlConnection"; } }
		protected override string DataReaderTypeName { get { return "Npgsql.NpgsqlDataReader"; } }

		public EdgePostgreSQLDataProvider()
			: base(TestProvName.PostgreSQLLatest, PostgreSQLVersion.v95)
		{
			_assembly = Assembly.LoadFrom("npgsql4/npgsql.dll");
		}

		public override Type DataReaderType
		{
			get
			{
				if (_dataReaderType != null)
					return _dataReaderType;

				if (DbFactoryProviderName == null)
					return _dataReaderType = _assembly.GetType(DataReaderTypeName, true);

				_dataReaderType = _assembly.GetType(DataReaderTypeName, false);

				if (_dataReaderType == null)
				{
					var assembly = DbProviderFactories.GetFactory(DbFactoryProviderName).GetType().Assembly;

					var idx = 0;
					var dataReaderTypeName = (idx = DataReaderTypeName.IndexOf(',')) != -1 ? DataReaderTypeName.Substring(0, idx) : DataReaderTypeName;
					_dataReaderType = assembly.GetType(dataReaderTypeName, true);
				}

				return _dataReaderType;
			}
		}

		protected override Type GetConnectionType()
		{
			if (_connectionType == null)
				lock (SyncRoot)
					if (_connectionType == null)
					{
						if (DbFactoryProviderName == null)
							_connectionType = _assembly.GetType(ConnectionTypeName, true);
						else
						{
							_connectionType = _assembly.GetType(ConnectionTypeName, false);

							if (_connectionType == null)
								using (var db = DbProviderFactories.GetFactory(DbFactoryProviderName).CreateConnection())
									_connectionType = db.GetType();
						}

						OnConnectionTypeCreated(_connectionType);
					}

			return _connectionType;
		}
	}
#endif
}
