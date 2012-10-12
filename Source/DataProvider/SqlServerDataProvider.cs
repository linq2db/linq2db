using System;
using System.Data;
using System.Data.SqlClient;

namespace LinqToDB.DataProvider
{
	public class SqlServerDataProvider : DataProviderBase
	{
		public SqlServerDataProvider()
		{
			Version = SqlServerVersion.v2008;
		}

		public SqlServerDataProvider(SqlServerVersion version)
		{
			Version = version;
		}

		public override string           Name         { get { return LinqToDB.ProviderName.SqlServer; } }
		public override string           ProviderName { get { return typeof(SqlConnection).Namespace; } }
		public          SqlServerVersion Version      { get; set; }

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}
		
		public override void Configure(string name, string value)
		{
			if (name == "version") switch (value)
			{
				case "2005" : Version = SqlServerVersion.v2005; break;
				case "2008" : Version = SqlServerVersion.v2008; break;
				case "2012" : Version = SqlServerVersion.v2012; break;
			}
		}
	}
}
