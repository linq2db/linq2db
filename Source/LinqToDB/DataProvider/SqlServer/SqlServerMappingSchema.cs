using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Xml;

namespace LinqToDB.DataProvider.SqlServer
{
	using System.Globalization;
	using Common;
	using Expressions;
	using LinqToDB.Metadata;
	using Mapping;
	using SqlQuery;

	public class SqlServerMappingSchema : MappingSchema
	{
		private const string DATETIME0_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss}'";
		private const string DATETIME1_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.f}'";
		private const string DATETIME2_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.ff}'";
		private const string DATETIME3_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.fff}'";
		private const string DATETIME4_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.ffff}'";
		private const string DATETIME5_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.fffff}'";
		private const string DATETIME6_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.ffffff}'";
		private const string DATETIME7_FORMAT = "'{0:yyyy-MM-ddTHH:mm:ss.fffffff}'";

		private const string TIME0_FORMAT     = "'{0:hh\\:mm\\:ss}'";
		private const string TIME7_FORMAT     = "'{0:hh\\:mm\\:ss\\.fffffff}'";

		private const string TIMESPAN0_FORMAT = "'{0:d\\.hh\\:mm\\:ss}'";
		private const string TIMESPAN7_FORMAT = "'{0:d\\.hh\\:mm\\:ss\\.fffffff}'";

		private const string DATETIMEOFFSET0_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss zzz}'";
		private const string DATETIMEOFFSET1_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.f zzz}'";
		private const string DATETIMEOFFSET2_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.ff zzz}'";
		private const string DATETIMEOFFSET3_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fff zzz}'";
		private const string DATETIMEOFFSET4_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.ffff zzz}'";
		private const string DATETIMEOFFSET5_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fffff zzz}'";
		private const string DATETIMEOFFSET6_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.ffffff zzz}'";
		private const string DATETIMEOFFSET7_FORMAT = "'{0:yyyy-MM-dd HH:mm:ss.fffffff zzz}'";

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

		internal static SqlServerMappingSchema Instance = new ();

		// TODO: move to SqlServerTypes.Configure?
		public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
		{
			if (@from           != to          &&
				@from.FullName  == to.FullName &&
				@from.Namespace == SqlServerTypes.TypesNamespace)
			{
				var p = Expression.Parameter(@from);

				return Expression.Lambda(
					Expression.Call(to, "Parse", Array<Type>.Empty,
						Expression.New(
							MemberHelper.ConstructorOf(() => new SqlString("")),
							Expression.Call(
								Expression.Convert(p, typeof(object)),
								"ToString",
								Array<Type>.Empty))),
					p);
			}

			return base.TryGetConvertExpression(@from, to);
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
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

			DataTools.ConvertStringToSql(stringBuilder, "+", startPrefix, AppendConversionAction, value, null);
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

			DataTools.ConvertCharToSql(stringBuilder, start, AppendConversionAction, value);
		}

		internal static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType? dt, DateTime value)
		{
			string format;

			if (dt?.Type.DataType == DataType.DateTime2)
				format = dt.Type.Precision switch
				{
					0 => DATETIME0_FORMAT,
					1 => DATETIME1_FORMAT,
					2 => DATETIME2_FORMAT,
					3 => DATETIME3_FORMAT,
					4 => DATETIME4_FORMAT,
					5 => DATETIME5_FORMAT,
					6 => DATETIME6_FORMAT,
					_ => DATETIME7_FORMAT,
				};
			else
				format = value.Millisecond == 0 ? DATETIME0_FORMAT : DATETIME3_FORMAT;

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
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
						? TIMESPAN7_FORMAT
						: TIMESPAN0_FORMAT
					: value.Ticks % 10000000 != 0
						? TIME7_FORMAT
						: TIME0_FORMAT;

				stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
			}
		}

		static void ConvertDateTimeOffsetToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, DateTimeOffset value)
		{
			var format = (sqlDataType.Type.Precision ?? sqlDataType.Type.Scale) switch
			{
				0 => DATETIMEOFFSET0_FORMAT,
				1 => DATETIMEOFFSET1_FORMAT,
				2 => DATETIMEOFFSET2_FORMAT,
				3 => DATETIMEOFFSET3_FORMAT,
				4 => DATETIMEOFFSET4_FORMAT,
				5 => DATETIMEOFFSET5_FORMAT,
				6 => DATETIMEOFFSET6_FORMAT,
				_ => DATETIMEOFFSET7_FORMAT
			};

			stringBuilder.AppendFormat(CultureInfo.InvariantCulture, format, value);
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");
			stringBuilder.AppendByteArrayAsHexViaLookup32(value);
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

	public class SqlServer2016MappingSchema : MappingSchema
	{
		public SqlServer2016MappingSchema()
			: base(ProviderName.SqlServer2016, SqlServerMappingSchema.Instance)
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
