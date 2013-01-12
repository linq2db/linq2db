using System;
using System.Data;
using System.Data.Common;
using System.Data.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Xml;
using System.Xml.Linq;

using Npgsql;
using NpgsqlTypes;

namespace LinqToDB.DataProvider
{
	public class PostgreSQLDataProvider : DataProviderBase
	{
		public PostgreSQLDataProvider() : base(new PostgreSQLMappingSchema())
		{
			SetCharField("bpchar", (r,i) => r.GetString(i).TrimEnd());

			SetProviderField<NpgsqlDataReader,BitString>        ((r,i) => r.GetBitString  (i));
			SetProviderField<NpgsqlDataReader,NpgsqlInterval>   ((r,i) => r.GetInterval   (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTime>       ((r,i) => r.GetTime       (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTimeTZ>     ((r,i) => r.GetTimeTZ     (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTimeStamp>  ((r,i) => r.GetTimeStamp  (i));
			SetProviderField<NpgsqlDataReader,NpgsqlTimeStampTZ>((r,i) => r.GetTimeStampTZ(i));
			SetProviderField<NpgsqlDataReader,NpgsqlDate>       ((r,i) => r.GetDate       (i));
			SetProviderField<NpgsqlDataReader,NpgsqlInet>       ((r,i) => (NpgsqlInet)      r.GetProviderSpecificValue(i));
			SetProviderField<NpgsqlDataReader,NpgsqlMacAddress> ((r,i) => (NpgsqlMacAddress)r.GetProviderSpecificValue(i));

			SetProviderField2<NpgsqlDataReader,DateTimeOffset,NpgsqlTimeStampTZ>((r,i) => (NpgsqlTimeStampTZ)r.GetProviderSpecificValue(i));
		}

		public override string Name           { get { return ProviderName.PostgreSQL; } }
		public override Type   ConnectionType { get { return typeof(NpgsqlConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new NpgsqlConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(NpgsqlDataReader));
		}

		#region Overrides

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (dataType == DataType.Undefined && value != null && !(value is string))
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;     break;
				case DataType.UInt16     : dataType = DataType.Int32;     break;
				case DataType.UInt32     : dataType = DataType.Int64;     break;
				case DataType.UInt64     : dataType = DataType.Decimal;   break;
				case DataType.DateTime2  : dataType = DataType.DateTime;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;   break;
				case DataType.Decimal    :
				case DataType.Money      : dataType = DataType.Undefined; break;
				case DataType.Image      : dataType = DataType.VarBinary; goto case DataType.VarBinary;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Undefined  :
					     if (value is Binary)      value = ((Binary)value).ToArray();
					else if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Binary    :
				case DataType.VarBinary : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Bytea;   break;
				case DataType.Boolean   : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Boolean; break;
				case DataType.Xml       : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Xml;     break;
				case DataType.Text      :
				case DataType.NText     : ((NpgsqlParameter)parameter).NpgsqlDbType = NpgsqlDbType.Text;    break;
				default                 : base.SetParameterType(parameter, dataType);                       break;
			}
		}

		#endregion
	}
}
