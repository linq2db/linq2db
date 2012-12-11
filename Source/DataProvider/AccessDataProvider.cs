using System;
using System.Data;
using System.Data.OleDb;
using System.Linq.Expressions;

namespace LinqToDB.DataProvider
{
	using Mapping;

	public class AccessDataProvider : DataProviderBase
	{
		#region AccessMappingSchema

		class AccessMappingSchema : MappingSchema
		{
			public AccessMappingSchema() : base(LinqToDB.ProviderName.Access)
			{
			}
		}

		#endregion

		public AccessDataProvider() : base(new AccessMappingSchema())
		{
		}

		public override string Name         { get { return LinqToDB.ProviderName.Access;      } }
		public override string ProviderName { get { return typeof(OleDbConnection).Namespace; } }
		
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
