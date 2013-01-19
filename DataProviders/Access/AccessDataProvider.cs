using System;
using System.Data;
using System.Data.OleDb;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class AccessDataProvider : DataProviderBase
	{
		public AccessDataProvider() : base(new AccessMappingSchema())
		{
			SetCharField("DBTYPE_WCHAR", (r,i) => r.GetString(i).TrimEnd());
		}

		public override string Name           { get { return ProviderName.Access;     } }
		public override Type   ConnectionType { get { return typeof(OleDbConnection); } }
		public override Type   DataReaderType { get { return typeof(OleDbDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new OleDbConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new AccessSqlProvider();
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
