using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
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
			ProviderName.OracleManaged,
			ProviderName.OracleNative,
			ProviderName.SapHana)]
					string context,
			[Values]
					bool providerSpecific)
		{
			using (new AllowMultipleQuery())
			using (new AvoidSpecificDataProviderAPI(providerSpecific))
			using (var db = new IssueDataConnection(context))
			{
				db.GetTable<Child>().LoadWith(p => p.Parent.Children).First();
			}
		}

		[ActiveIssue("AvoidSpecificDataProviderAPI support missing", Configurations = new[] { ProviderName.OracleManaged, ProviderName.OracleNative })]
		[Test]
		public void TestFactory([DataSources(false)] string context, [Values] bool providerSpecific)
		{
			using (new AllowMultipleQuery())
			using (new AvoidSpecificDataProviderAPI(providerSpecific))
			using (var db = new FactoryDataConnection(context))
			{
				db.GetTable<Child>().LoadWith(p => p.Parent.Children).First();
			}
		}
	}
}
