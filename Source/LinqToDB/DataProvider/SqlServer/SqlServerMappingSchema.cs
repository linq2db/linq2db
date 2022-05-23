using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using System.Xml;

namespace LinqToDB.DataProvider.SqlServer
{
	using Common;
	using Expressions;
	using Metadata;
	using Mapping;
	using SqlQuery;
	using System.Runtime.CompilerServices;

	sealed class SqlServerMappingSchema : LockedMappingSchema
	{
		private static readonly int[] TICKS_DIVIDERS = new[]
		{
			10000000,
			1000000,
			100000,
			10000,
			1000,
			100,
			10,
			1
		};

		// TIME(p)
		private const string TIME_FROMPARTS_FORMAT                    = "TIMEFROMPARTS({0}, {1}, {2}, {3}, {4})";
		private static readonly string[] TIME_TYPED_FORMATS           = new[]
		{
			"CAST('{0:hh\\:mm\\:ss}' AS TIME(0))",
			"CAST('{0:hh\\:mm\\:ss\\.f}' AS TIME(1))",
			"CAST('{0:hh\\:mm\\:ss\\.ff}' AS TIME(2))",
			"CAST('{0:hh\\:mm\\:ss\\.fff}' AS TIME(3))",
			"CAST('{0:hh\\:mm\\:ss\\.ffff}' AS TIME(4))",
			"CAST('{0:hh\\:mm\\:ss\\.fffff}' AS TIME(5))",
			"CAST('{0:hh\\:mm\\:ss\\.ffffff}' AS TIME(6))",
			"CAST('{0:hh\\:mm\\:ss\\.fffffff}' AS TIME)"
		};
		// DATE
		private const string DATE_FROMPARTS_FORMAT                     = "DATEFROMPARTS({0}, {1}, {2})";
		private const string DATE_FORMAT                               = "'{0:yyyy-MM-dd}'";
		private const string DATE_TYPED_FORMAT                         = "CAST('{0:yyyy-MM-dd}' AS DATE)";
		// SMALLDATETIME
		private const string SMALLDATETIME_FROMPARTS_FORMAT            = "SMALLDATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4})";
		private const string SMALLDATETIME_TYPED_FORMAT                = "CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS SMALLDATETIME)";
		// DATETIME
		private const string DATETIME_FROMPARTS_FORMAT                 = "DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})";
		private const string DATETIME_FORMAT                           = "'{0:yyyy-MM-ddTHH:mm:ss.fffffff}'";
		private const string DATETIME_TYPED_FORMAT                     = "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)";
		// DATETIME2(p)
		private const string DaTETIME2_FROMPARTS_FORMAT                = "DATETIME2FROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})";
		private static readonly string[] DATETIME2_TYPED_FORMATS       = new[]
		{
			"CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME2(0))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME2(1))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME2(2))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME2(3))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ffff}' AS DATETIME2(4))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffff}' AS DATETIME2(5))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffff}' AS DATETIME2(6))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffff}' AS DATETIME2)"
		};
		// DATETIMEOFFSET(p)
		private const string DaTETIMEOFFSET_FROMPARTS_FORMAT          = "DATETIMEOFFSETFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})";
		private static readonly string[] DATETIMEOFFSET_FORMATS       = new[]
		{
			"'{0:yyyy-MM-ddTHH:mm:sszzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.fzzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.ffzzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.fffzzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}'",
			"'{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}'",
		};
		private static readonly string[] DATETIMEOFFSET_TYPED_FORMATS = new[]
		{
			"CAST('{0:yyyy-MM-ddTHH:mm:sszzz}' AS DATETIMEOFFSET(0))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fzzz}' AS DATETIMEOFFSET(1))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ffzzz}' AS DATETIMEOFFSET(2))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffzzz}' AS DATETIMEOFFSET(3))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}' AS DATETIMEOFFSET(4))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}'} AS DATETIMEOFFSET(5))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}' AS DATETIMEOFFSET(6))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}' AS DATETIMEOFFSET)"
		};

		SqlServerMappingSchema() : base(ProviderName.SqlServer)
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

			SetValueToSqlConverter(typeof(string),         (sb,dt,v) => ConvertStringToSql(sb, dt.Type.DataType, v.ToString()!));
			SetValueToSqlConverter(typeof(char),           (sb,dt,v) => ConvertCharToSql  (sb, dt, (char)v));
			SetValueToSqlConverter(typeof(byte[]),         (sb,dt,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary),         (sb,dt,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string)));

			AddMetadataReader(new SystemDataSqlServerAttributeReader());
		}

		static SqlServerMappingSchema Instance = new ();

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

		static void ConvertStringToSql(StringBuilder stringBuilder, DataType dataType, string value)
		{
			string? startPrefix;

			switch (dataType)
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

		static void ConvertDateTimeToSql(StringBuilder stringBuilder, SqlDataType dt, DateTime value, bool v2008plus, bool supportsFromParts)
		{
			switch (dt.Type.DataType, v2008plus, supportsFromParts)
			{
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
					break;
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _):
					stringBuilder.Append('N');
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
					break;

				case (DataType.SmallDateTime, _, true):
					// SMALLDATETIMEFROMPARTS ( year, month, day, hour, minute )
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, SMALLDATETIME_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second);
					break;
				case (DataType.SmallDateTime, _, false):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, SMALLDATETIME_TYPED_FORMAT, value);
					break;

				case (DataType.Date, true, true):
					// DATEFROMPARTS ( year, month, day )
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FROMPARTS_FORMAT, value.Year, value.Month, value.Day);
					break;
				case (DataType.Date, true, false):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_TYPED_FORMAT, value);
					break;

				case (DataType.DateTime2, true, true):
				{
					var precision = dt.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException($"DATETIME2 type precision is out-of-bounds: {precision}");

					// DATETIME2FROMPARTS ( year, month, day, hour, minute, seconds, fractions, precision )
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DaTETIME2_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, GetFractionalSecondFromTicks(value.Ticks, precision), precision);
					break;
				}
				case (DataType.DateTime2, true, false):
				{
					var precision = dt.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException($"DATETIME2 type precision is out-of-bounds: {precision}");

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME2_TYPED_FORMATS[precision], value);
					break;
				}

				default:
					// default: DATETIME
					if (supportsFromParts)
						// DATETIMEFROMPARTS ( year, month, day, hour, minute, seconds, milliseconds )
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, value.Millisecond);
					else
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_TYPED_FORMAT, value);
					break;
			}
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, TimeSpan value, bool supportsTime, bool supportsFromParts)
		{
			switch (sqlDataType.Type.DataType, supportsTime, supportsFromParts)
			{
				case (DataType.Int64, _, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "{0}", value.Ticks);
					break;
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0:c}'", value);
					break;
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _) or (_, false, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "N'{0:c}'", value);
					break;
				default:
				{
					if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
						throw new InvalidOperationException($"TIME value is out-of-bounds: {value:c}");

					var precision = sqlDataType.Type.Precision ?? 7;

					if (precision < 0 || precision > 7)
						throw new InvalidOperationException($"TIME type precision is out-of-bounds: {precision}");

					if (supportsFromParts)
						// TIMEFROMPARTS ( hour, minute, seconds, fractions, precision )
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME_FROMPARTS_FORMAT, value.Hours, value.Minutes, value.Seconds, GetFractionalSecondFromTicks(value.Ticks, precision), precision);
					else
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME_TYPED_FORMATS[precision], value);
					break;
				}
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static long GetFractionalSecondFromTicks(long ticks, int precision) => (ticks % TICKS_DIVIDERS[0]) / TICKS_DIVIDERS[precision];

#if NET6_0_OR_GREATER
		static void ConvertDateToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, DateOnly value, bool v2008plus, bool supportsFromParts)
		{
			switch (sqlDataType.Type.DataType, v2008plus, supportsFromParts)
			{
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _):
					stringBuilder.Append('N');
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
					break;
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
					break;

				case (_, false, _):
					ConvertDateTimeToSql(stringBuilder, sqlDataType, value.ToDateTime(default), v2008plus, supportsFromParts);
					break;

				default:
				{
					if (supportsFromParts)
						// DATEFROMPARTS ( year, month, day )
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FROMPARTS_FORMAT, value.Year, value.Month, value.Day);
					else
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_TYPED_FORMAT, value);
					break;
				}
			}
		}
#endif

		static void ConvertDateTimeOffsetToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, DateTimeOffset value, bool v2008plus, bool supportsFromParts)
		{
			switch (sqlDataType.Type.DataType, v2008plus, supportsFromParts)
			{
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_FORMATS[precision], value);
					break;
				}
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

					stringBuilder.Append('N');
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_FORMATS[precision], value);
					break;
				}

				case (_, false, _) or (DataType.Date, _, _) or (DataType.DateTime, _, _) or (DataType.DateTime2, _, _) or (DataType.SmallDateTime, _, _):
					ConvertDateTimeToSql(stringBuilder, sqlDataType, value.DateTime, v2008plus, supportsFromParts);
					return;

				default:
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException($"DATETIMEOFFSET type precision is out-of-bounds: {precision}");

					if (supportsFromParts)
						// DATETIMEOFFSETFROMPARTS ( year, month, day, hour, minute, seconds, fractions, hour_offset, minute_offset, precision )
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DaTETIMEOFFSET_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, GetFractionalSecondFromTicks(value.Ticks, precision), value.Offset.Hours, value.Offset.Minutes, precision);
					else
						stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_TYPED_FORMATS[precision], value);
					break;
				}
			}
		}

		static void ConvertBinaryToSql(StringBuilder stringBuilder, byte[] value)
		{
			stringBuilder.Append("0x");
			stringBuilder.AppendByteArrayAsHexViaLookup32(value);
		}

		public sealed class SqlServer2005MappingSchema : LockedMappingSchema
		{
			public SqlServer2005MappingSchema() : base(ProviderName.SqlServer2005, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , false, false));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, false, false));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , false, false));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , false, false));
#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , false, false));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type from, Type to)
			{
				return Instance.TryGetConvertExpression(from, to);
			}
		}

		public sealed class SqlServer2008MappingSchema : LockedMappingSchema
		{
			public SqlServer2008MappingSchema() : base(ProviderName.SqlServer2008, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, false));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, false));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, false));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, false));

#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, false));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type from, Type to)
			{
				return Instance.TryGetConvertExpression(from, to);
			}
		}

		public sealed class SqlServer2012MappingSchema : LockedMappingSchema
		{
			public SqlServer2012MappingSchema() : base(ProviderName.SqlServer2012, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		public sealed class SqlServer2014MappingSchema : LockedMappingSchema
		{
			public SqlServer2014MappingSchema() : base(ProviderName.SqlServer2014, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		public sealed class SqlServer2016MappingSchema : LockedMappingSchema
		{
			public SqlServer2016MappingSchema() : base(ProviderName.SqlServer2016, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		public sealed class SqlServer2017MappingSchema : LockedMappingSchema
		{
			public SqlServer2017MappingSchema() : base(ProviderName.SqlServer2017, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		public sealed class SqlServer2019MappingSchema : LockedMappingSchema
		{
			public SqlServer2019MappingSchema() : base(ProviderName.SqlServer2019, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb, dt, v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb, dt, v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb, dt, v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET6_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb, dt, v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}
	}
}
