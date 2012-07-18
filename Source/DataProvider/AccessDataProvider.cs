using System;
using System.Data;
using System.Data.OleDb;

namespace LinqToDB.DataProvider
{
	public class AccessDataProvider : DataProviderBase
	{
		public override string Name         { get { return LinqToDB.ProviderName.Access;      } }
		public override string ProviderName { get { return typeof(OleDbConnection).Namespace; } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new OleDbConnection(connectionString);
		}
	}
}
