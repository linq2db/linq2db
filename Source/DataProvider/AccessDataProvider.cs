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

		static readonly MappingSchema _accessMappingSchema = new AccessMappingSchema();

		#endregion

		public AccessDataProvider() : base(_accessMappingSchema)
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
