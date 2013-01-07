using System;
using System.Data;
using System.Data.Linq;
using System.Data.OleDb;
using System.Linq.Expressions;
using System.Xml;
using System.Xml.Linq;

namespace LinqToDB.DataProvider
{
	using Expressions;
	using Mapping;

	public class AccessDataProvider : DataProviderBase
	{
		public AccessDataProvider() : base(new AccessMappingSchema())
		{
		}

		public override string Name           { get { return ProviderName.Access;     } }
		public override Type   ConnectionType { get { return typeof(OleDbConnection); } }
		
		public override IDbConnection CreateConnection(string connectionString )
		{
			return new OleDbConnection(connectionString);
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(OleDbDataReader));
		}

		#region Overrides

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var expr = base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);
			var name = ((OleDbDataReader)reader).GetDataTypeName(idx);

			if (expr.Type == typeof(string) && (name == "DBTYPE_WCHAR"))
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((OleDbDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["AllowDBNull"];
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.Binary     :
				case DataType.VarBinary  :
					if (value is Binary) value = ((Binary)value).ToArray();
					break;
				case DataType.Xml        :
					     if (value is XDocument)   value = value.ToString();
					else if (value is XmlDocument) value = ((XmlDocument)value).InnerXml;
					break;
				case DataType.Undefined  :
					     if (value is Binary)       value = ((Binary)value).ToArray();
					else if (value is XDocument)    value = value.ToString();
					else if (value is XmlDocument)  value = ((XmlDocument)value).InnerXml;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);
		}

		#endregion
	}
}
