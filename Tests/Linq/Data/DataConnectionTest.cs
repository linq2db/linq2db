using System;
using System.Data;

using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.Data
{
	using System.Configuration;

	using Model;

	[TestFixture]
	public class DataConnectionTest : TestBase
	{
		[Test]
		public void Test1([IncludeDataContexts("Northwind")] string context)
		{
			var connectionString = ConfigurationManager.ConnectionStrings["Northwind"].ConnectionString;

			using (var conn = new DataConnection(SqlServerTools.GetDataProvider(), connectionString))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.Null);
			}
		}

		[Test]
		public void Test2()
		{
			using (var conn = new DataConnection())
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(DataConnection.DefaultConfiguration));
			}
		}

		[Test]
		public void Test3([IncludeDataContexts(
			ProviderName.SqlServer,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2008 + ".1",
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2005 + ".1",
			ProviderName.Access
			)] string context)
		{
			using (var conn = new DataConnection(context))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(context));

				if (context.EndsWith(".2005"))
				{
					var sdp = conn.DataProvider;
					Assert.That(sdp.Name, Is.EqualTo("SqlServer.2005"));
				}

				if (context.EndsWith(".2008"))
				{
					var sdp = conn.DataProvider;
					Assert.That(sdp.Name, Is.EqualTo("SqlServer.2008"));
				}
			}
		}

		[Test]
		public void EnumExecuteScalarTest()
		{
			using (var dbm = new DataConnection())
			{
				var gender = dbm.Execute<Gender>("select 'M'");

				Assert.That(gender, Is.EqualTo(Gender.Male));
			}
		}
	}
}
