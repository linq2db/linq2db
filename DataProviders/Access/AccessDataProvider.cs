using System;
using System.Data;
using System.Data.OleDb;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	public class AccessDataProvider : DataProviderBase
	{
		public AccessDataProvider() : base(new AccessMappingSchema())
		{
		}

		public override string Name           { get { return ProviderName.Access;     } }
		public override Type   ConnectionType { get { return typeof(OleDbConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new OleDbConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(OleDbDataReader));
		}
	}
}
