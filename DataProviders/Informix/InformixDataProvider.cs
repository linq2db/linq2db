using System;
using System.Data;
using System.Data.Linq;
using System.Xml;
using System.Xml.Linq;

using IBM.Data.Informix;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class InformixDataProvider : DataProviderBase
	{
		public InformixDataProvider() : base(new InformixMappingSchema())
		{
			SetCharField("CHAR",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("NCHAR", (r,i) => r.GetString(i).TrimEnd());

			SetField<IfxDataReader,Int64>("BIGINT", (r,i) => r.GetBigInt(i));

			SetProviderField<IfxDataReader,IfxDecimal, decimal> ((r,i) => r.GetIfxDecimal (i));
			SetProviderField<IfxDataReader,IfxDateTime,DateTime>((r,i) => r.GetIfxDateTime(i));
			SetProviderField<IfxDataReader,IfxTimeSpan,TimeSpan>((r,i) => r.GetIfxTimeSpan(i));
		}

		public override string Name           { get { return ProviderName.DB2;      } }
		public override Type   ConnectionType { get { return typeof(IfxConnection); } }
		public override Type   DataReaderType { get { return typeof(IfxDataReader); } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new IfxConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new InformixSqlProvider();
		}

		static readonly SqlProviderFlags _sqlProviderFlags = new SqlProviderFlags
		{
			IsParameterOrderDependent = true
		};

		public override SqlProviderFlags GetSqlProviderFlags()
		{
			return _sqlProviderFlags;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
//			if (value is sbyte)
//			{
//				value    = (short)(sbyte)value;
//				dataType = DataType.Int16;
//			}
//			else if (value is byte)
//			{
//				value    = (short)(byte)value;
//				dataType = DataType.Int16;
//			}

			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				case DataType.UInt16     : dataType = DataType.Int32;    break;
				case DataType.UInt32     : dataType = DataType.Int64;    break;
				case DataType.UInt64     : dataType = DataType.Decimal;  break;
				case DataType.VarNumeric : dataType = DataType.Decimal;  break;
				case DataType.DateTime2  : dataType = DataType.DateTime; break;
				case DataType.Time       :
					if (value is TimeSpan)
						value = new IfxTimeSpan((TimeSpan)value);
					break;
//				case DataType.Char       :
//				case DataType.VarChar    :
//				case DataType.NChar      :
//				case DataType.NVarChar   :
//					if (value is Guid) value = ((Guid)value).ToString();
//					break;
//				case DataType.Guid       :
//					if (value is Guid)
//					{
//						value    = ((Guid)value).ToByteArray();
//						dataType = DataType.VarBinary;
//					}
//					break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					if (value is Guid)   value = ((Guid)value).ToByteArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		public override void SetParameterType(IDbDataParameter parameter, DataType dataType)
		{
			switch (dataType)
			{
				case DataType.Text  : ((IfxParameter)parameter).IfxType = IfxType.Clob; break;
				case DataType.NText : ((IfxParameter)parameter).IfxType = IfxType.Clob; break;
				default             : base.SetParameterType(parameter, dataType);       break;
			}
		}
	}
}
