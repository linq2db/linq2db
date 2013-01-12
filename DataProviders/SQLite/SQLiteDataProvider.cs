using System;
using System.Data;
using System.Data.Linq;
using System.Data.SQLite;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	public class SQLiteDataProvider : DataProviderBase
	{
		public SQLiteDataProvider() : base(new SQLiteMappingSchema())
		{
			SetCharField("char",  (r,i) => r.GetString(i).TrimEnd());
			SetCharField("nchar", (r,i) => r.GetString(i).TrimEnd());
		}

		public override string Name           { get { return ProviderName.SQLite;     } }
		public override Type   ConnectionType { get { return typeof(SQLiteConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new SQLiteConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(SQLiteDataReader));
		}

		#region Overrides

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.UInt32     : dataType = DataType.Int64;   break;
				case DataType.UInt64     : dataType = DataType.Decimal; break;
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Undefined  :
					     if (value is uint)         dataType = DataType.Int64;
					else if (value is ulong)        dataType = DataType.Decimal;
					else if (value is Binary)       value = ((Binary)value).ToArray();
					else if (value is XDocument)    value = value.ToString();
					else if (value is XmlDocument)  value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		#endregion
	}
}
