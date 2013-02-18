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

			AddScalarType(typeof(SqlBinary),      SqlBinary.     Null, true, DataType.VarBinary);
			AddScalarType(typeof(SqlBoolean),     SqlBoolean.    Null, true, DataType.Boolean);
			AddScalarType(typeof(SqlByte),        SqlByte.       Null, true, DataType.Byte);
			AddScalarType(typeof(SqlDateTime),    SqlDateTime.   Null, true, DataType.DateTime);
			AddScalarType(typeof(SqlDecimal),     SqlDecimal.    Null, true, DataType.Decimal);
			AddScalarType(typeof(SqlDouble),      SqlDouble.     Null, true, DataType.Double);
			AddScalarType(typeof(SqlGuid),        SqlGuid.       Null, true, DataType.Guid);
			AddScalarType(typeof(SqlInt16),       SqlInt16.      Null, true, DataType.Int16);
			AddScalarType(typeof(SqlInt32),       SqlInt32.      Null, true, DataType.Int32);
			AddScalarType(typeof(SqlInt64),       SqlInt64.      Null, true, DataType.Int64);
			AddScalarType(typeof(SqlMoney),       SqlMoney.      Null, true, DataType.Money);
			AddScalarType(typeof(SqlSingle),      SqlSingle.     Null, true, DataType.Single);
			AddScalarType(typeof(SqlString),      SqlString.     Null, true, DataType.NVarChar);
			AddScalarType(typeof(SqlXml),         SqlXml.        Null, true, DataType.Xml);
			AddScalarType(typeof(SqlHierarchyId), SqlHierarchyId.Null, true, DataType.Udt);
			AddScalarType(typeof(SqlGeography),   SqlGeography.  Null, true, DataType.Udt);
			AddScalarType(typeof(SqlGeometry),    SqlGeometry.   Null, true, DataType.Udt);
		}

		internal static SqlServerMappingSchema Instance = new SqlServerMappingSchema();
	}
}
