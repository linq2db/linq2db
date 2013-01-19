using System;
using System.Data;
using System.Data.Linq;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	class SqlCeDataProvider : DataProviderBase
	{
		public SqlCeDataProvider() : base(new SqlCeMappingSchema())
		{
			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());
		}

		public override string Name           { get { return ProviderName.SqlCe;      } }
		public override Type   ConnectionType { get { return typeof(SqlCeConnection); } }
		public override Type   DataReaderType { get { return typeof(SqlCeDataReader); } }

		#region Overrides

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlCeConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new SqlCeSqlProvider();
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;    break;
				case DataType.UInt16     : dataType = DataType.Int32;    break;
				case DataType.UInt32     : dataType = DataType.Int64;    break;
				case DataType.UInt64     : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;  break;
				case DataType.Char       : dataType = DataType.NChar;    break;
				case DataType.VarChar    : dataType = DataType.NVarChar; break;
				case DataType.Text       : dataType = DataType.NText;    break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					dataType = DataType.NVarChar;

					if (value is SqlXml)
					{
						var xml = (SqlXml)value;
						value = xml.IsNull ? null : xml.Value;
					}
					else if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;

					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			var p = (SqlCeParameter)parameter;

			switch (dataType)
			{
				case DataType.Text      :
				case DataType.NText     : p.SqlDbType = SqlDbType.NText;     break;
				case DataType.Char      :
				case DataType.NChar     : p.SqlDbType = SqlDbType.NChar;     break;
				case DataType.VarChar   :
				case DataType.NVarChar  : p.SqlDbType = SqlDbType.NVarChar;  break;
				case DataType.Timestamp : p.SqlDbType = SqlDbType.Timestamp; break;
				case DataType.Binary    : p.SqlDbType = SqlDbType.Binary;    break;
				case DataType.VarBinary : p.SqlDbType = SqlDbType.VarBinary; break;
				case DataType.Image     : p.SqlDbType = SqlDbType.Image;     break;
				case DataType.DateTime  :
				case DataType.DateTime2 : p.SqlDbType = SqlDbType.DateTime;  break;
				case DataType.Money     : p.SqlDbType = SqlDbType.Money;     break;
				case DataType.Boolean   : p.SqlDbType = SqlDbType.Money;     break;
				default                 :
					base.SetParameterType(parameter, dataType);
					break;
			}
		}

		#endregion
	}
}
