using System;
using System.Data;

using NUnit.Framework;

using LinqToDB_Temp;
using LinqToDB_Temp.Data;
using LinqToDB_Temp.DataProvider;

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
		public void Test3()
		{
			using (var conn = new DataConnection(ProviderName.SqlServer))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(ProviderName.SqlServer));
			}
		}

		[Test]
		public void Test4()
		{
			using (var conn = new DataConnection(ProviderName.Access))
			{
				Assert.That(conn.Connection.State,    Is.EqualTo(ConnectionState.Open));
				Assert.That(conn.ConfigurationString, Is.EqualTo(ProviderName.Access));
			}
		}
	}
}
