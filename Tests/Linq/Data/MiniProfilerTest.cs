using System;
using System.Data;
using System.Data.SqlClient;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider;
using LinqToDB.DataProvider.SqlServer;

using NUnit.Framework;

using StackExchange.Profiling;

using Tests.Model;

namespace Tests.Data
{
	[TestFixture]
	public class MiniProfilerTest : TestBase
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
				return new StackExchange.Profiling.Data.ProfiledDbConnection(dbConnection, MiniProfiler.Current);
			}
		}

		[Test, NorthwindDataContext]
		public void Test1(string context)
		{
			using (var mpcon = new MiniProfilerDataContext(context))
			{
				mpcon.GetTable<Northwind.Category>().ToList();
			}
		}
	}
}
