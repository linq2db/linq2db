using System;
using System.Data;

using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	public class MySqlDataProvider : DataProviderBase
	{
		public MySqlDataProvider()
			: this(ProviderName.MySql, new MySqlMappingSchema())
		{
		}

		protected MySqlDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SetProviderField<MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
			SetProviderField<MySqlDataReader,MySqlDateTime>((r,i) => r.GetMySqlDateTime(i));
			SetToTypeField  <MySqlDataReader,MySqlDecimal> ((r,i) => r.GetMySqlDecimal (i));
			SetToTypeField  <MySqlDataReader,MySqlDateTime>((r,i) => r.GetMySqlDateTime(i));
		}

		public override Type ConnectionType { get { return typeof(MySqlConnection); } }
		public override Type DataReaderType { get { return typeof(MySqlDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new MySqlConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new MySqlSqlProvider(SqlProviderFlags);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Decimal    :
				case DataType.VarNumeric :
					if (value is MySqlDecimal)
						value = ((MySqlDecimal)value).Value;
					break;
				case DataType.Date       :
				case DataType.DateTime   :
				case DataType.DateTime2  :
					if (value is MySqlDateTime)
						value = ((MySqlDateTime)value).Value;
					break;
				case DataType.Char       :
				case DataType.NChar      :
					if (value is char)
						value = value.ToString();
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
		}
	}
}
