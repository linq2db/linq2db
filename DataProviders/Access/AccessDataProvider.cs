using System;
using System.Data;
using System.Data.Linq;
using System.Data.OleDb;
using System.Xml;
using System.Xml.Linq;

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

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			if (dataType == DataType.Undefined && value != null)
				dataType = MappingSchema.GetDataType(value.GetType());

			switch (dataType)
			{
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
