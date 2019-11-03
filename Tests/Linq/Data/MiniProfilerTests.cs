using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.Access;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;
using StackExchange.Profiling;
using StackExchange.Profiling.Data;
using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class MiniProfilerTests : TestBase
	{
		public class MiniProfilerDataContext : DataConnection
		{
			public MiniProfilerDataContext(string configurationString)
				: base(GetDataProvider(), GetConnection(configurationString)) { }

			private static IDataProvider GetDataProvider()
			{
				return new SqlServerDataProvider("MiniProfiler." + ProviderName.SqlServer2000, SqlServerVersion.v2012);
			}

			private static IDbConnection GetConnection(string configurationString)
			{
				LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = true;
				var dbConnection = new SqlConnection(GetConnectionString(configurationString));
				return new ProfiledDbConnection(dbConnection, MiniProfiler.Current);
			}
		}

		[Test]
		public void Test1([IncludeDataSources(TestProvName.Northwind)] string context)
		{
			try
			{
				using (var mpcon = new MiniProfilerDataContext(context))
				{
					mpcon.GetTable<Northwind.Category>().ToList();
				}
			}
			finally
			{
				LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = false;
			}
		}

		// provider-specific tests
		[Test]
		public void TestAccess([IncludeDataSources(ProviderName.Access)] string context, [Values] ConnectionType type, [Values] bool avoidApi)
		{
			using (new AvoidSpecificDataProviderAPI(avoidApi))
			{
				using (var db = CreateDataConnection(AccessTools.GetDataProvider(), type))
				{
				}
			}
		}

		public enum ConnectionType
		{
			Raw,
			MiniProfiler,
			SimpleMiniProfiler
		}


		private DataConnection CreateDataConnection(IDataProvider provider, ConnectionType type)
		{
			DataConnection db = null;
			return db = new DataConnection(provider, () =>
			{
				var cn = provider.CreateConnection(db.ConfigurationString);

				switch (type)
				{
					case ConnectionType.MiniProfiler      : return new ProfiledDbConnection((DbConnection)cn, MiniProfiler.Current);
					case ConnectionType.SimpleMiniProfiler: return new SimpleProfiledConnection(cn, MiniProfiler.Current);
				}

				return cn;
			});
		}
	}
}
