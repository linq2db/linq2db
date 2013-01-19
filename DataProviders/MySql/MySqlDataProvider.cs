using System;
using System.Data;
using System.Data.Linq;
using System.Xml;
using System.Xml.Linq;

using MySql.Data.MySqlClient;
using MySql.Data.Types;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class MySqlDataProvider : DataProviderBase
	{
		public MySqlDataProvider() : base(new MySqlMappingSchema())
		{
			//SetCharField("DBTYPE_WCHAR", (r,i) => r.GetString(i).TrimEnd());

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
			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				//case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.DateTime2  : dataType = DataType.DateTime; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			if (value is MySqlDecimal)
				value = ((MySqlDecimal)value).Value;

			base.SetParameter(parameter, name, dataType, value);
		}
	}
}
