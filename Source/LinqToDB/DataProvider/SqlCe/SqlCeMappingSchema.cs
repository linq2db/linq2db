using System;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Xml;

namespace LinqToDB.DataProvider.SqlCe
{
	using Common;
	using Mapping;
	using SqlQuery;

	public class SqlCeMappingSchema : MappingSchema
	{
		public SqlCeMappingSchema() : this(ProviderName.SqlCe)
		{
		}

		protected SqlCeMappingSchema(string configuration) : base(configuration)
		{
			SetConvertExpression<SqlXml,XmlReader>(
				s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader(),
				s => s.CreateReader());

			SetConvertExpression<string,SqlXml>(s => new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(s))));

			AddScalarType(typeof(SqlBinary),   SqlBinary.  Null, true, DataType.VarBinary);
			AddScalarType(typeof(SqlBoolean),  SqlBoolean. Null, true, DataType.Boolean);
			AddScalarType(typeof(SqlByte),     SqlByte.    Null, true, DataType.Byte);
			AddScalarType(typeof(SqlDateTime), SqlDateTime.Null, true, DataType.DateTime);
			AddScalarType(typeof(SqlDecimal),  SqlDecimal. Null, true, DataType.Decimal);
			AddScalarType(typeof(SqlDouble),   SqlDouble.  Null, true, DataType.Double);
			AddScalarType(typeof(SqlGuid),     SqlGuid.    Null, true, DataType.Guid);
			AddScalarType(typeof(SqlInt16),    SqlInt16.   Null, true, DataType.Int16);
			AddScalarType(typeof(SqlInt32),    SqlInt32.   Null, true, DataType.Int32);
			AddScalarType(typeof(SqlInt64),    SqlInt64.   Null, true, DataType.Int64);
			AddScalarType(typeof(SqlMoney),    SqlMoney.   Null, true, DataType.Money);
			AddScalarType(typeof(SqlSingle),   SqlSingle.  Null, true, DataType.Single);
			AddScalarType(typeof(SqlString),   SqlString.  Null, true, DataType.NVarChar);
			AddScalarType(typeof(SqlXml),      SqlXml.     Null, true, DataType.Xml);

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string), 255));

			SetValueToSqlConverter(typeof(String), (sb,dt,v) => ConvertStringToSql(sb, dt, v.ToString()));
			SetValueToSqlConverter(typeof(Char),   (sb,dt,v) => ConvertCharToSql  (sb, dt, (char)v));
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("nchar(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, string value)
		{
			string startPrefix;

			switch (sqlDataType.DataType)
			{
				case DataType.Char    :
				case DataType.VarChar :
				case DataType.Text    :
					startPrefix = null;
					break;
				default               :
					startPrefix = "N";
					break;
			}

			DataTools.ConvertStringToSql(stringBuilder, "+", startPrefix, AppendConversion, value, null);
		}

		static void ConvertCharToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, char value)
		{
			string start;

			switch (sqlDataType.DataType)
			{
				case DataType.Char    :
				case DataType.VarChar :
				case DataType.Text    :
					start = "'";
					break;
				default               :
					start = "N'";
					break;
			}

			DataTools.ConvertCharToSql(stringBuilder, start, AppendConversion, value);
		}
	}
}
