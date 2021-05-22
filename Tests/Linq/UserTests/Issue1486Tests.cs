﻿using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.Mapping;
using NUnit.Framework;
using System.Data;
using System.Linq;
using Tests.Model;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1486Tests : TestBase
	{
		public class IssueDataConnection : DataConnection
		{
			public IssueDataConnection(string configuration)
				: base(GetDataProvider(configuration), GetConnection(configuration), true)
			{
				// to avoid mapper conflict with SequentialAccess test provider
				AddMappingSchema(new MappingSchema());
			}

			private new static IDataProvider GetDataProvider(string configuration)
			{
				return DataConnection.GetDataProvider(configuration);
			}

			private static IDbConnection GetConnection(string configuration)
			{
				string connStr = GetConnectionString(configuration);

				return DataConnection.GetDataProvider(configuration).CreateConnection(connStr);
			}
		}

		public class FactoryDataConnection : DataConnection
		{
			public FactoryDataConnection(string configuration)
				: base(GetDataProvider(configuration), () => GetConnection(configuration))
			{
				// to avoid mapper conflict with SequentialAccess test provider
				AddMappingSchema(new MappingSchema());
			}

			private new static IDataProvider GetDataProvider(string configuration)
			{
				return DataConnection.GetDataProvider(configuration);
			}

			private static IDbConnection GetConnection(string configuration)
			{
				string connStr = GetConnectionString(configuration);

				return DataConnection.GetDataProvider(configuration).CreateConnection(connStr);
			}
		}

		// excluded providers don't support cloning and remove credentials from connection string
		[Test]
		public void TestConnectionStringCopy(
			[DataSources(
				false,
				ProviderName.MySqlConnector,
				TestProvName.AllOracle,
				TestProvName.AllSapHana)]
					string context)
		{
			using (var db = new IssueDataConnection(context))
			{
				db.GetTable<Child>().LoadWith(p => p.Parent!.Children).First();
			}
		}

		[Test]
		public void TestFactory([DataSources(false)] string context)
		{
			using (var db = new FactoryDataConnection(context))
			{
				db.GetTable<Child>().LoadWith(p => p.Parent!.Children).First();
			}
		}
	}
}
