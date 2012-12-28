using System;
using System.Data;
using System.Data.Common;
using System.Data.SqlClient;
using System.Data.SqlTypes;
using System.IO;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Xml;

using Microsoft.SqlServer.Types;

namespace LinqToDB.DataProvider
{
	using Common;
	using Expressions;
	using Mapping;

	public class SqlServerDataProvider : DataProviderBase
	{
		#region SqlServerMappingSchema

		class SqlServerMappingSchema : MappingSchema
		{
			public SqlServerMappingSchema()
				: base(LinqToDB.ProviderName.SqlServer)
			{
				SetConvertExpression<SqlXml,XmlReader>(
					s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader(),
					s => s.CreateReader());

				SetConvertExpression<string,SqlXml>(s => new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(s))));

				SetDefaultValue(SqlBinary.     Null);
				SetDefaultValue(SqlBoolean.    Null);
				SetDefaultValue(SqlByte.       Null);
				SetDefaultValue(SqlDateTime.   Null);
				SetDefaultValue(SqlDecimal.    Null);
				SetDefaultValue(SqlDouble.     Null);
				SetDefaultValue(SqlGuid.       Null);
				SetDefaultValue(SqlInt16.      Null);
				SetDefaultValue(SqlInt32.      Null);
				SetDefaultValue(SqlInt64.      Null);
				SetDefaultValue(SqlMoney.      Null);
				SetDefaultValue(SqlSingle.     Null);
				SetDefaultValue(SqlString.     Null);
				SetDefaultValue(SqlXml.        Null);
				SetDefaultValue(SqlHierarchyId.Null);
				SetDefaultValue(SqlGeography.  Null);
				SetDefaultValue(SqlGeometry.   Null);

				SetScalarType(typeof(SqlBinary));
				SetScalarType(typeof(SqlBoolean));
				SetScalarType(typeof(SqlByte));
				SetScalarType(typeof(SqlDateTime));
				SetScalarType(typeof(SqlDecimal));
				SetScalarType(typeof(SqlDouble));
				SetScalarType(typeof(SqlGuid));
				SetScalarType(typeof(SqlInt16));
				SetScalarType(typeof(SqlInt32));
				SetScalarType(typeof(SqlInt64));
				SetScalarType(typeof(SqlMoney));
				SetScalarType(typeof(SqlSingle));
				SetScalarType(typeof(SqlString));
				SetScalarType(typeof(SqlXml));
				SetScalarType(typeof(SqlHierarchyId));
				SetScalarType(typeof(SqlGeography));
				SetScalarType(typeof(SqlGeometry));
			}
		}

		static readonly MappingSchema _sqlServerMappingSchema     = new SqlServerMappingSchema();
		static readonly MappingSchema _sqlServerMappingSchema2005 = new MappingSchema(LinqToDB.ProviderName.SqlServer2005, _sqlServerMappingSchema);
		static readonly MappingSchema _sqlServerMappingSchema2008 = new MappingSchema(LinqToDB.ProviderName.SqlServer2008, _sqlServerMappingSchema);
		static readonly MappingSchema _sqlServerMappingSchema2012 = new MappingSchema(LinqToDB.ProviderName.SqlServer2012, _sqlServerMappingSchema);

		#endregion

		public SqlServerDataProvider() : this(SqlServerVersion.v2008)
		{
		}

		public SqlServerDataProvider(SqlServerVersion version)
			: base(_sqlServerMappingSchema)
		{
			Version = version;
		}

		public override string Name         { get { return LinqToDB.ProviderName.SqlServer; } }
		public override string ProviderName { get { return typeof(SqlConnection).Namespace; } }

		private         MappingSchema _mappingSchema = _sqlServerMappingSchema;
		public override MappingSchema  MappingSchema
		{
			get { return _mappingSchema; }
		}

		private SqlServerVersion _version;
		public  SqlServerVersion  Version
		{
			get { return _version;  }
			set
			{
				switch (_version = value)
				{
					case SqlServerVersion.v2005 : _mappingSchema = _sqlServerMappingSchema2005; break;
					case SqlServerVersion.v2008 : _mappingSchema = _sqlServerMappingSchema2008; break;
					case SqlServerVersion.v2012 : _mappingSchema = _sqlServerMappingSchema2012; break;
				}
			}
		}

		public override IDbConnection CreateConnection(string connectionString)
		{
			return new SqlConnection(connectionString);
		}

		public override void Configure(string name, string value)
		{
			if (name == "version") switch (value)
			{
				case "2005" : Version = SqlServerVersion.v2005; break;
				case "2008" : Version = SqlServerVersion.v2008; break;
				case "2012" : Version = SqlServerVersion.v2012; break;
			}
		}

		public override Expression ConvertDataReader(Expression reader)
		{
			return Expression.Convert(reader, typeof(SqlDataReader));
		}

		public override Expression GetReaderExpression(MappingSchema mappingSchema, IDataReader reader, int idx, Expression readerExpression, Type toType)
		{
			var expr = base.GetReaderExpression(mappingSchema, reader, idx, readerExpression, toType);

			if (expr.Type == typeof(object))
			{
				var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

				if (type == typeof(SqlHierarchyId))
					expr = Expression.Convert(expr, type);
			}

			var name = ((SqlDataReader)reader).GetDataTypeName(idx);

			if (expr.Type == typeof(string) && (name == "char" || name == "nchar"))
				expr = Expression.Call(expr, MemberHelper.MethodOf<string>(s => s.Trim()));

			return expr;
		}

		protected override MethodInfo GetReaderMethodInfo(IDataRecord reader, int idx, Type toType)
		{
			var type = ((DbDataReader)reader).GetProviderSpecificFieldType(idx);

			//if (toType == type)
			//{
			//	if (type == typeof(SqlBinary))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlBinary  (0));
			//	if (type == typeof(SqlBoolean))  return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlBoolean (0));
			//	if (type == typeof(SqlByte))     return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlByte    (0));
			//	if (type == typeof(SqlDateTime)) return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDateTime(0));
			//	if (type == typeof(SqlDecimal))  return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDecimal (0));
			//	if (type == typeof(SqlDouble))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlDouble  (0));
			//	if (type == typeof(SqlGuid))     return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlGuid    (0));
			//	if (type == typeof(SqlInt16))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt16   (0));
			//	if (type == typeof(SqlInt32))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt32   (0));
			//	if (type == typeof(SqlInt64))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlInt64   (0));
			//	if (type == typeof(SqlMoney))    return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlMoney   (0));
			//	if (type == typeof(SqlSingle))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlSingle  (0));
			//	if (type == typeof(SqlString))   return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlString  (0));
			//	if (type == typeof(SqlXml))      return MemberHelper.MethodOf<SqlDataReader>(r => r.GetSqlXml     (0));
			//}

			var mi = base.GetReaderMethodInfo(reader, idx, toType);

			if (mi != null)
				return mi;

			if (type == typeof(DateTimeOffset)) return MemberHelper.MethodOf<SqlDataReader>(r => r.GetDateTimeOffset(0));
			if (type == typeof(TimeSpan))       return MemberHelper.MethodOf<SqlDataReader>(r => r.GetTimeSpan      (0));

			return null;
		}

		public override bool? IsDBNullAllowed(IDataReader reader, int idx)
		{
			var st = ((SqlDataReader)reader).GetSchemaTable();
			return st == null || (bool)st.Rows[idx]["allowDBNull"];
		}

		public override void SetParameter(IDbDataParameter parameter, string name, DataType dataType, object value)
		{
			switch (dataType)
			{
				case DataType.SByte     : dataType = DataType.Int16;   break;
				case DataType.UInt16    : dataType = DataType.Int32;   break;
				case DataType.UInt32    : dataType = DataType.Int64;   break;
				case DataType.UInt64    : dataType = DataType.Decimal; break;
				case DataType.Undefined :
					if (value is sbyte)
						dataType = DataType.Int16;
					break;
			}

			base.SetParameter(parameter, name, dataType, value);

			switch (dataType)
			{
				case DataType.Text      : ((SqlParameter)parameter).SqlDbType = SqlDbType.Text;      break;
				case DataType.NText     : ((SqlParameter)parameter).SqlDbType = SqlDbType.NText;     break;
				case DataType.Binary    : ((SqlParameter)parameter).SqlDbType = SqlDbType.Binary;    break;
				case DataType.VarBinary : ((SqlParameter)parameter).SqlDbType = SqlDbType.VarBinary; break;
			}
		}
	}
}
