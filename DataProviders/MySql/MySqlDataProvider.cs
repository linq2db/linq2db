using System;
using System.Data;

using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class MySqlDataProvider : DataProviderBase
	{
		public MySqlDataProvider() : base(new MySqlMappingSchema())
		{
			SetProviderField<MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
			SetProviderField<MySqlDataReader,MySqlDateTime>((r,i) => r.GetMySqlDateTime(i));
			SetToTypeField  <MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
			SetToTypeField  <MySqlDataReader,MySqlDateTime>((r,i) => r.GetMySqlDateTime(i));
		}

		public override string Name           { get { return ProviderName.MySql;      } }
		public override Type   ConnectionType { get { return typeof(MySqlConnection); } }
		public override Type   DataReaderType { get { return typeof(MySqlDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new MySqlConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MySqlSqlProvider();
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (value is MySqlDecimal)
				value = ((MySqlDecimal)value).Value;

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.DateTime2 : dataType = DataType.DateTime; break;
			}

			base.SetParameterType(parameter, dataType);
		}
	}
}
