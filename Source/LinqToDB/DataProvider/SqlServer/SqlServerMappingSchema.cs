using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Xml;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Expressions;
	using LinqToDB.Metadata;
	using Mapping;
	using SqlQuery;

	public class SqlServerMappingSchema : MappingSchema
	{
		public SqlServerMappingSchema()
			: base(ProviderName.SqlServer)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetConvertExpression<SqlXml, XmlReader>(
				s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader(),
				s => s.CreateReader());

			SetConvertExpression<string,SqlXml>(s => new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(s))));

			AddScalarType(typeof(SqlBinary),    SqlBinary.  Null, true, DataType.VarBinary);
			AddScalarType(typeof(SqlBinary?),   SqlBinary.  Null, true, DataType.VarBinary);
			AddScalarType(typeof(SqlBoolean),   SqlBoolean. Null, true, DataType.Boolean);
			AddScalarType(typeof(SqlBoolean?),  SqlBoolean. Null, true, DataType.Boolean);
			AddScalarType(typeof(SqlByte),      SqlByte.    Null, true, DataType.Byte);
			AddScalarType(typeof(SqlByte?),     SqlByte.    Null, true, DataType.Byte);
			AddScalarType(typeof(SqlDateTime),  SqlDateTime.Null, true, DataType.DateTime);
			AddScalarType(typeof(SqlDateTime?), SqlDateTime.Null, true, DataType.DateTime);
			AddScalarType(typeof(SqlDecimal),   SqlDecimal. Null, true, DataType.Decimal);
			AddScalarType(typeof(SqlDecimal?),  SqlDecimal. Null, true, DataType.Decimal);
			AddScalarType(typeof(SqlDouble),    SqlDouble.  Null, true, DataType.Double);
			AddScalarType(typeof(SqlDouble?),   SqlDouble.  Null, true, DataType.Double);
			AddScalarType(typeof(SqlGuid),      SqlGuid.    Null, true, DataType.Guid);
			AddScalarType(typeof(SqlGuid?),     SqlGuid.    Null, true, DataType.Guid);
			AddScalarType(typeof(SqlInt16),     SqlInt16.   Null, true, DataType.Int16);
			AddScalarType(typeof(SqlInt16?),    SqlInt16.   Null, true, DataType.Int16);
			AddScalarType(typeof(SqlInt32),     SqlInt32.   Null, true, DataType.Int32);
			AddScalarType(typeof(SqlInt32?),    SqlInt32.   Null, true, DataType.Int32);
			AddScalarType(typeof(SqlInt64),     SqlInt64.   Null, true, DataType.Int64);
			AddScalarType(typeof(SqlInt64?),    SqlInt64.   Null, true, DataType.Int64);
			AddScalarType(typeof(SqlMoney),     SqlMoney.   Null, true, DataType.Money);
			AddScalarType(typeof(SqlMoney?),    SqlMoney.   Null, true, DataType.Money);
			AddScalarType(typeof(SqlSingle),    SqlSingle.  Null, true, DataType.Single);
			AddScalarType(typeof(SqlSingle?),   SqlSingle.  Null, true, DataType.Single);
			AddScalarType(typeof(SqlString),    SqlString.  Null, true, DataType.NVarChar);
			AddScalarType(typeof(SqlString?),   SqlString.  Null, true, DataType.NVarChar);
			AddScalarType(typeof(SqlXml),       SqlXml.     Null, true, DataType.Xml);

			AddScalarType(typeof(DateTime),  DataType.DateTime);
			AddScalarType(typeof(DateTime?), DataType.DateTime);

			SqlServerTypes.Configure(this);

			SetValueToSqlConverter(typeof(string),         (sb,dt,v) => ConvertStringToSql        (sb, dt, v.ToString()!));
			SetValueToSqlConverter(typeof(char),           (sb,dt,v) => ConvertCharToSql          (sb, dt, (char)v));
			SetValueToSqlConverter(typeof(DateTime),       (sb,dt,v) => ConvertDateTimeToSql      (sb, null, (DateTime)v));
			SetValueToSqlConverter(typeof(TimeSpan),       (sb,dt,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v));
			SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb,dt,v) => ConvertBinaryToSql        (sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb,dt,v) => ConvertBinaryToSql        (sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string)));

			AddMetadataReader(new SystemDataSqlServerAttributeReader());
		}

		internal static SqlServerMappingSchema Instance = new SqlServerMappingSchema();

		// TODO: move to SqlServerTypes.Configure?
		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			if (@from           != to          &&
				@from.FullName  == to.FullName &&
				@from.Namespace == SqlServerTypes.TypesNamespace)
			{
				var p = Expression.Parameter(@from);

				return Expression.Lambda(
					Expression.Call(to, "Parse", new Type[0],
						Expression.New(
							MemberHelper.ConstructorOf(() => new SqlString("")),
							Expression.Call(
								Expression.Convert(p, typeof(object)),
								"ToString",
								new Type[0]))),
					p);
			}

			return base.TryGetConvertExpression(@from, to);
		}

		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder
				.Append("char(")
				.Append(value)
				.Append(')')
				;
		}

		static void ConvertStringToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, string value)
		{
			string? startPrefix;

			switch (sqlDataType.Type.DataType)
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

			switch (sqlDataType.Type.DataType)
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

		internal static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType? dt, DateTime value)
		{
			var format =
				value.Millisecond == 0
					? "yyyy-MM-ddTHH:mm:ss"
					: dt == null || dt.Type.DataType != DataType.DateTime2
						? "yyyy-MM-ddTHH:mm:ss.fff"
						: dt.Type.Precision == 0
							? "yyyy-MM-ddTHH:mm:ss"
							: "yyyy-MM-ddTHH:mm:ss." + new string('f', dt.Type.Precision ?? 7);

			stringBuilder
				.Append('\'')
				.Append(value.ToString(format))
				.Append('\'')
				;
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, TimeSpan value)
		{
			if (sqlDataType.Type.DataType == DataType.Int64)
			{
				stringBuilder.Append(value.Ticks);
			}
			else
			{
				var format = value.Days > 0
					? value.Ticks % 10000000 != 0
						? "d\\.hh\\:mm\\:ss\\.fffffff"
						: "d\\.hh\\:mm\\:ss"
					: value.Ticks % 10000000 != 0
						? "hh\\:mm\\:ss\\.fffffff"
						: "hh\\:mm\\:ss";

				stringBuilder
					.Append('\'')
					.Append(value.ToString(format))
					.Append('\'')
					;
			}
		}

		static void ConvertDateTimeOffsetToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, DateTimeOffset value)
		{
			var format = "'{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}'";

			switch (sqlDataType.Type.Precision ?? sqlDataType.Type.Scale)
			{
				case 0 : format = "'{0:yyyy-MM-dd HH:mm:ss zzz}'"; break;
				case 1 : format = "'{0:yyyy-MM-dd HH:mm:ss.f zzz}'"; break;
				case 2 : format = "'{0:yyyy-MM-dd HH:mm:ss.ff zzz}'"; break;
				case 3 : format = "'{0:yyyy-MM-dd HH:mm:ss.fff zzz}'"; break;
				case 4 : format = "'{0:yyyy-MM-dd HH:mm:ss.ffff zzz}'"; break;
				case 5 : format = "'{0:yyyy-MM-dd HH:mm:ss.fffff zzz}'"; break;
				case 6 : format = "'{0:yyyy-MM-dd HH:mm:ss.ffffff zzz}'"; break;
				case 7 : format = "'{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}'"; break;
			}

			stringBuilder.AppendFormat(format, value);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");

			foreach (var b in value)
				stringBuilder.Append(b.ToString("X2"));
		}
	}

	public class SqlServer2000MappingSchema : MappingSchema
	{
		public SqlServer2000MappingSchema()
			: base(ProviderName.SqlServer2000, SqlServerMappingSchema.Instance)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;
		}

		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2005MappingSchema : MappingSchema
	{
		public SqlServer2005MappingSchema()
			: base(ProviderName.SqlServer2005, SqlServerMappingSchema.Instance)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;
		}

		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2008MappingSchema : MappingSchema
	{
		public SqlServer2008MappingSchema()
			: base(ProviderName.SqlServer2008, SqlServerMappingSchema.Instance)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, v) => SqlServerMappingSchema.ConvertDateTimeToSql(sb, dt, (DateTime)v));
		}

		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2012MappingSchema : MappingSchema
	{
		public SqlServer2012MappingSchema()
			: base(ProviderName.SqlServer2012, SqlServerMappingSchema.Instance)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, v) => SqlServerMappingSchema.ConvertDateTimeToSql(sb, dt, (DateTime)v));
		}

		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}

	public class SqlServer2017MappingSchema : MappingSchema
	{
		public SqlServer2017MappingSchema()
			: base(ProviderName.SqlServer2017, SqlServerMappingSchema.Instance)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;
			SetValueToSqlConverter(typeof(DateTime), (sb, dt, v) => SqlServerMappingSchema.ConvertDateTimeToSql(sb, dt, (DateTime)v));
		}

		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			return SqlServerMappingSchema.Instance.TryGetConvertExpression(@from, to);
		}
	}
}
