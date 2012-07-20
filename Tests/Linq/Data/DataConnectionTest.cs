using System;
using System.Data;

using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;

namespace Tests.Data
{
	[TestFixture]
	public class DataConnectionTest
	{
		[Test]
		public void Test1()
		{
			using (var conn = new DataConnection(new SqlServerDataProvider(), "Server=.;Database=BLToolkitData;Integrated Security=SSPI"))
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
		public void Test3([Values(
			ProviderName.SqlServer,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2008 + ".1",
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2005 + ".1",
			ProviderName.Access
			)] string config)
		{
			using (var conn = new DataConnection(config))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(config));

				if (config.EndsWith(".2005"))
				{
					var sdp = (SqlServerDataProvider)conn.DataProvider;
					Assert.That(sdp.Version, Is.EqualTo(SqlServerVersion.v2005));
				}

				if (config.EndsWith(".2008"))
				{
					var sdp = (SqlServerDataProvider)conn.DataProvider;
					Assert.That(sdp.Version, Is.EqualTo(SqlServerVersion.v2008));
				}
			}
		}
	}
}
