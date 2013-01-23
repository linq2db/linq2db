using System;
using System.Data.SqlTypes;
using System.IO;
using System.Text;
using System.Xml;

using Microsoft.SqlServer.Types;

namespace LinqToDB.DataProvider
{
	using Common;
	using Mapping;

	public class SqlServerMappingSchema : MappingSchema
	{
		public SqlServerMappingSchema()
			: base(ProviderName.SqlServer)
		{
			SetConvertExpression<SqlXml,XmlReader>(
				s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader(),
				s => s.CreateReader());

			SetConvertExpression<string,SqlXml>(s => new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(s))));

			AddScalarType(typeof(SqlBinary),      SqlBinary.     Null, DataType.VarBinary);
			AddScalarType(typeof(SqlBoolean),     SqlBoolean.    Null, DataType.Boolean);
			AddScalarType(typeof(SqlByte),        SqlByte.       Null, DataType.Byte);
			AddScalarType(typeof(SqlDateTime),    SqlDateTime.   Null, DataType.DateTime);
			AddScalarType(typeof(SqlDecimal),     SqlDecimal.    Null, DataType.Decimal);
			AddScalarType(typeof(SqlDouble),      SqlDouble.     Null, DataType.Double);
			AddScalarType(typeof(SqlGuid),        SqlGuid.       Null, DataType.Guid);
			AddScalarType(typeof(SqlInt16),       SqlInt16.      Null, DataType.Int16);
			AddScalarType(typeof(SqlInt32),       SqlInt32.      Null, DataType.Int32);
			AddScalarType(typeof(SqlInt64),       SqlInt64.      Null, DataType.Int64);
			AddScalarType(typeof(SqlMoney),       SqlMoney.      Null, DataType.Money);
			AddScalarType(typeof(SqlSingle),      SqlSingle.     Null, DataType.Single);
			AddScalarType(typeof(SqlString),      SqlString.     Null, DataType.NVarChar);
			AddScalarType(typeof(SqlXml),         SqlXml.        Null, DataType.Xml);
			AddScalarType(typeof(SqlHierarchyId), SqlHierarchyId.Null, DataType.Udt);
			AddScalarType(typeof(SqlGeography),   SqlGeography.  Null, DataType.Udt);
			AddScalarType(typeof(SqlGeometry),    SqlGeometry.   Null, DataType.Udt);
		}

		internal static SqlServerMappingSchema Instance = new SqlServerMappingSchema();
	}
}
