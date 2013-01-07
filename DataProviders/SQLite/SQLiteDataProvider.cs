using System;
using System.Data;
using System.Data.Linq;
using System.Data.SQLite;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Mapping;

	public class SQLiteDataProvider : DataProviderBase
	{
		public SQLiteDataProvider() : base(new SQLiteMappingSchema())
		{
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

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var expr = base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
			var name = ((SQLiteDataReader)reader).GetDataTypeName(idx);

			if (expr.Type == typeof(string) &&
				(string.Compare(name, "char",  StringComparison.OrdinalIgnoreCase) == 0 ||
				 string.Compare(name, "nchar", StringComparison.OrdinalIgnoreCase) == 0))
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((SQLiteDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["AllowDBNull"];
		}

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
