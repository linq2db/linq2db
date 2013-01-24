using System;
using System.Data;
using System.Data.SqlServerCe;
using System.Data.SqlTypes;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using Mapping;
	using SqlProvider;

	public class SqlCeDataProvider : DataProviderBase
	{
		public SqlCeDataProvider(string name)
			: this(name, new SqlCeMappingSchema(name))
		{
		}

		public SqlCeDataProvider(string name, MappingSchema mappingSchema)
			: base(name, mappingSchema)
		{
			SetCharField("NChar", (r,i) => r.GetString(i).TrimEnd());
		}

		public override Type ConnectionType { get { return typeof(SqlCeConnection); } }
		public override Type DataReaderType { get { return typeof(SqlCeDataReader); } }

		#region Overrides

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlCeConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new SqlCeSqlProvider(SqlProviderFlags);
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Xml :
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

		protected override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			var p = (SqlCeParameter)parameter;

			switch (dataType)
			{
				case DataType.SByte      : p.DbType    = DbType.Int16;        break;
				case DataType.UInt16     : p.DbType    = DbType.Int32;        break;
				case DataType.UInt32     : p.DbType    = DbType.Int64;        break;
				case DataType.UInt64     : p.DbType    = DbType.Decimal;      break;
				case DataType.VarNumeric : p.DbType    = DbType.Decimal;      break;
				case DataType.Text       :
				case DataType.NText      : p.SqlDbType = SqlDbType.NText;     break;
				case DataType.Char       :
				case DataType.NChar      : p.SqlDbType = SqlDbType.NChar;     break;
				case DataType.VarChar    :
				case DataType.NVarChar   : p.SqlDbType = SqlDbType.NVarChar;  break;
				case DataType.Timestamp  : p.SqlDbType = SqlDbType.Timestamp; break;
				case DataType.Binary     : p.SqlDbType = SqlDbType.Binary;    break;
				case DataType.VarBinary  : p.SqlDbType = SqlDbType.VarBinary; break;
				case DataType.Image      : p.SqlDbType = SqlDbType.Image;     break;
				case DataType.DateTime   :
				case DataType.DateTime2  : p.SqlDbType = SqlDbType.DateTime;  break;
				case DataType.Money      : p.SqlDbType = SqlDbType.Money;     break;
				case DataType.Boolean    : p.SqlDbType = SqlDbType.Money;     break;
				default                  :
					base.SetParameterType(parameter, dataType);
					break;
			}
		}

		#endregion
	}
}
