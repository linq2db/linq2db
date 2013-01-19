using System;
using System.Data;
using System.Data.Linq;
using System.Xml;
using System.Xml.Linq;

using FirebirdSql.Data.FirebirdClient;

namespace LinqToDB.DataProvider
{
	using SqlProvider;

	public class FirebirdDataProvider : DataProviderBase
	{
		public FirebirdDataProvider() : base(new FirebirdMappingSchema())
		{
			SetCharField("CHAR", (r,i) => r.GetString(i).TrimEnd());

			//SetProviderField<FbDataReader,string>((r,i) => r.Get(i));
		}

		public override string Name           { get { return ProviderName.Firebird; } }
		public override Type   ConnectionType { get { return typeof(FbConnection);  } }
		public override Type   DataReaderType { get { return typeof(FbDataReader);  } }
		
		public override IDbConnection CreateConnection(string connectionString)
		{
			return new FbConnection(connectionString);
		}

		public override ISqlProvider CreateSqlProvider()
		{
			return new FirebirdSqlProvider();
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			return true;
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
//			if (value is ulong)
//				value = (decimal)(ulong)value;

			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
				case DataType.SByte      : dataType = DataType.Int16;   break;
				case DataType.UInt16     : dataType = DataType.Int32;   break;
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.VarNumeric : dataType = DataType.Decimal; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}
	}
}
