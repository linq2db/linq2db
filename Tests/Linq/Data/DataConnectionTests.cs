using System;
using System.Data;
using System.Threading.Tasks;

using NUnit.Framework;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.DB2;
using LinqToDB.DataProvider.SqlServer;

namespace Tests.Data
{
#if !NETSTANDARD1_6
	using System.Configuration;
#endif

	using Model;

	[TestFixture]
	public class DataConnectionTests : TestBase
	{
		[Test]
		public void Test1([NorthwindDataContext] string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);
			var dataProvider = DataConnection.GetDataProvider(context);

			using (var conn = new DataConnection(dataProvider, connectionString))
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
		public void Test3([IncludeDataSources(
			ProviderName.SqlServer,
			ProviderName.SqlServer2008,
			ProviderName.SqlServer2008 + ".1",
			ProviderName.SqlServer2005,
			ProviderName.SqlServer2005 + ".1",
			ProviderName.Access)]
			string context)
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

		[Test, DataContextSource(false)]
		public void CloneTest(string context)
		{
			using (var con = new DataConnection(context))
			{
				var dbName = con.Connection.Database;

				for (var i = 0; i < 150; i++)
					using (var clone = (DataConnection)con.Clone())
						dbName = clone.Connection.Database;
			}
		}

		[Test]
		public void GetDataProviderTest([IncludeDataSources(false,
			ProviderName.DB2, ProviderName.SqlServer2005, ProviderName.SqlServer2008,
			ProviderName.SqlServer2012, ProviderName.SqlServer2014)]
			string context)
		{
			var connectionString = DataConnection.GetConnectionString(context);

			IDataProvider dataProvider;

			switch (context)
			{
				case ProviderName.DB2:
				{
					dataProvider = DataConnection.GetDataProvider("DB2", connectionString);

					Assert.That(dataProvider, Is.TypeOf<DB2DataProvider>());

					var sqlServerDataProvider = (DB2DataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(DB2Version.LUW));

					break;
				}

				case ProviderName.SqlServer2005:
				{
					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", "MyConfig.2005", connectionString);

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2005));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString);
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2005));

					break;
				}

				case ProviderName.SqlServer2008:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer", connectionString);

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2008));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString);
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2008));

					break;
				}

				case ProviderName.SqlServer2012:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer.2012", connectionString);

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString);
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					break;
				}

				case ProviderName.SqlServer2014:
				{
					dataProvider = DataConnection.GetDataProvider("SqlServer", "SqlServer.2012", connectionString);

					Assert.That(dataProvider, Is.TypeOf<SqlServerDataProvider>());

					var sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					dataProvider = DataConnection.GetDataProvider("System.Data.SqlClient", connectionString);
					sqlServerDataProvider = (SqlServerDataProvider)dataProvider;

					Assert.That(sqlServerDataProvider.Version, Is.EqualTo(SqlServerVersion.v2012));

					break;
				}
			}
		}

		[Test]
		public void TestOpenEvent()
		{
			var opened = false;
			var openedAsync = false;
			using (var conn = new DataConnection())
			{
				conn.OnConnectionOpened += (dc, cn) => opened = true;
				conn.OnConnectionOpenedAsync += async (dc, cn, token) => await Task.Run(() => openedAsync = true);
				Assert.False(opened);
				Assert.False(openedAsync);
				Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
				Assert.True(opened);
				Assert.False(openedAsync);
			}
		}

		[Test]
		public async Task TestAsyncOpenEvent()
		{
			var opened = false;
			var openedAsync = false;
			using (var conn = new DataConnection())
			{
				conn.OnConnectionOpened += (dc, cn) => opened = true;
				conn.OnConnectionOpenedAsync += async (dc, cn, token) => await Task.Run(() => openedAsync = true);
				Assert.False(opened);
				Assert.False(openedAsync);
				await conn.SelectAsync(() => 1);
				Assert.False(opened);
				Assert.True(openedAsync);
			}
		}

		[Test]
		public void TestOpenEventWithoutHandlers()
		{
			using (var conn = new DataConnection())
			{
				Assert.That(conn.Connection.State, Is.EqualTo(ConnectionState.Open));
			}
		}

		[Test]
		public async Task TestAsyncOpenEventWithoutHandlers()
		{
			using (var conn = new DataConnection())
			{
				await conn.SelectAsync(() => 1);
			}
		}
	}
}
