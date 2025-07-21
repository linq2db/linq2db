using System;
using System.Data.Linq;
using System.Data.SqlTypes;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text;
using System.Xml;

using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Expressions;
using LinqToDB.Internal.Common;
using LinqToDB.Internal.Extensions;
using LinqToDB.Internal.Mapping;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

namespace LinqToDB.Internal.DataProvider.SqlServer
{
	sealed class SqlServerMappingSchema : LockedMappingSchema
	{
#if SUPPORTS_COMPOSITE_FORMAT
		// TIME(p)
		private static readonly CompositeFormat TIME_TICKS_FORMAT     = CompositeFormat.Parse("CAST({0} AS BIGINT)");
		private static readonly CompositeFormat TIME_FROMPARTS_FORMAT = CompositeFormat.Parse("TIMEFROMPARTS({0}, {1}, {2}, {3}, {4})");
		private static readonly CompositeFormat[] TIME_TYPED_FORMATS  = new[]
		{
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss}' AS TIME(0))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.f}' AS TIME(1))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.ff}' AS TIME(2))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.fff}' AS TIME(3))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.ffff}' AS TIME(4))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.fffff}' AS TIME(5))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.ffffff}' AS TIME(6))"),
			CompositeFormat.Parse("CAST('{0:hh\\:mm\\:ss\\.fffffff}' AS TIME)")
		};

		// DATE
		private static readonly CompositeFormat DATE_FROMPARTS_FORMAT             = CompositeFormat.Parse("DATEFROMPARTS({0}, {1}, {2})");
		private static readonly CompositeFormat DATE_FORMAT                       = CompositeFormat.Parse("'{0:yyyy-MM-dd}'");
		private static readonly CompositeFormat DATE_TYPED_FORMAT                 = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd}' AS DATE)");
		private static readonly CompositeFormat DATE_AS_DATETIME_TYPED_FORMAT     = CompositeFormat.Parse("CAST('{0:yyyy-MM-dd}' AS DATETIME)");
		// SMALLDATETIME
		private static readonly CompositeFormat SMALLDATETIME_TYPED_FORMAT        = CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS SMALLDATETIME)");
		// DATETIME
		private static readonly CompositeFormat DATETIME_FROMPARTS_FORMAT         = CompositeFormat.Parse("DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})");
		// precision=3 to match SqlClient behavior for parameters
		// alternative option will be to generate parameter value explicitly
		private static readonly CompositeFormat DATETIME_FORMAT                   = CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fff}'");
		private static readonly CompositeFormat DATETIME_TYPED_FORMAT             = CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)");
		private static readonly CompositeFormat[] DATETIME_WITH_PRECISION_FORMATS = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)")
		};
		// DATETIME2(p)
		private static readonly CompositeFormat DaTETIME2_FROMPARTS_FORMAT      = CompositeFormat.Parse("DATETIME2FROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7})");
		private static readonly CompositeFormat[] DATETIME2_TYPED_FORMATS       = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME2(0))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME2(1))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME2(2))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME2(3))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffff}' AS DATETIME2(4))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffff}' AS DATETIME2(5))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffff}' AS DATETIME2(6))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffff}' AS DATETIME2)")
		};
		// DATETIMEOFFSET(p)
		private static readonly CompositeFormat DaTETIMEOFFSET_FROMPARTS_FORMAT = CompositeFormat.Parse("DATETIMEOFFSETFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9})");
		private static readonly CompositeFormat[] DATETIMEOFFSET_FORMATS        = new[]
		{
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:sszzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.ffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}'"),
			CompositeFormat.Parse("'{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}'")
		};

		private static readonly CompositeFormat[] DATETIMEOFFSET_TYPED_FORMATS = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:sszzz}' AS DATETIMEOFFSET(0))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fzzz}' AS DATETIMEOFFSET(1))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffzzz}' AS DATETIMEOFFSET(2))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffzzz}' AS DATETIMEOFFSET(3))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffzzz}' AS DATETIMEOFFSET(4))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}' AS DATETIMEOFFSET(5))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}' AS DATETIMEOFFSET(6))"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}' AS DATETIMEOFFSET)")
		};
		private static readonly CompositeFormat[] DATETIMEOFFSET_AS_DATETIME_TYPED_FORMATS = new[]
		{
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
			CompositeFormat.Parse("CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"),
		};
#else
		// TIME(p)
		private const string TIME_TICKS_FORMAT                        = "CAST({0} AS BIGINT)";
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
		private const string DATE_FROMPARTS_FORMAT                       = "DATEFROMPARTS({0}, {1}, {2})";
		private const string DATE_FORMAT                                 = "'{0:yyyy-MM-dd}'";
		private const string DATE_TYPED_FORMAT                           = "CAST('{0:yyyy-MM-dd}' AS DATE)";
		private const string DATE_AS_DATETIME_TYPED_FORMAT               = "CAST('{0:yyyy-MM-dd}' AS DATETIME)";
		// SMALLDATETIME
		private const string SMALLDATETIME_TYPED_FORMAT                  = "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS SMALLDATETIME)";
		// DATETIME
		private const string DATETIME_FROMPARTS_FORMAT                   = "DATETIMEFROMPARTS({0}, {1}, {2}, {3}, {4}, {5}, {6})";
		// precision=3 to match SqlClient behavior for parameters
		// alternative option will be to generate parameter value explicitly
		private const string DATETIME_FORMAT                             = "'{0:yyyy-MM-ddTHH:mm:ss.fff}'";
		private const string DATETIME_TYPED_FORMAT                       = "CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)";
		private static readonly string[] DATETIME_WITH_PRECISION_FORMATS = new[]
		{
			"CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)"
		};
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
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffzzz}' AS DATETIMEOFFSET(5))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ffffffzzz}' AS DATETIMEOFFSET(6))",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fffffffzzz}' AS DATETIMEOFFSET)"
		};
		private static readonly string[] DATETIMEOFFSET_AS_DATETIME_TYPED_FORMATS = new[]
		{
			"CAST('{0:yyyy-MM-ddTHH:mm:ss}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.f}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.ff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
			"CAST('{0:yyyy-MM-ddTHH:mm:ss.fff}' AS DATETIME)",
		};
#endif

		private static readonly string[] TIME_RAW_FORMATS            = new[]
		{
			"hh\\:mm\\:ss",
			"hh\\:mm\\:ss\\.f",
			"hh\\:mm\\:ss\\.ff",
			"hh\\:mm\\:ss\\.fff",
			"hh\\:mm\\:ss\\.ffff",
			"hh\\:mm\\:ss\\.fffff",
			"hh\\:mm\\:ss\\.ffffff",
			"hh\\:mm\\:ss\\.fffffff"
		};
		private static readonly string[] DATETIMEOFFSET_RAW_FORMATS   = new[]
		{
			"yyyy-MM-ddTHH:mm:sszzz",
			"yyyy-MM-ddTHH:mm:ss.fzzz",
			"yyyy-MM-ddTHH:mm:ss.ffzzz",
			"yyyy-MM-ddTHH:mm:ss.fffzzz",
			"yyyy-MM-ddTHH:mm:ss.ffffzzz",
			"yyyy-MM-ddTHH:mm:ss.fffffzzz",
			"yyyy-MM-ddTHH:mm:ss.ffffffzzz",
			"yyyy-MM-ddTHH:mm:ss.fffffffzzz",
		};

		SqlServerMappingSchema() : base(ProviderName.SqlServer)
		{
			ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

			SetConvertExpression<SqlXml, XmlReader>(
				s => s.IsNull ? DefaultValue<XmlReader>.Value : s.CreateReader(),
				s => s.CreateReader());

			SetConvertExpression<string,SqlXml>(s => new SqlXml(new MemoryStream(Encoding.UTF8.GetBytes(s))));

			AddScalarType(typeof(SqlChars),     SqlChars.   Null, true);
			AddScalarType(typeof(SqlBinary),    SqlBinary.  Null, true, DataType.VarBinary);
			AddScalarType(typeof(SqlBoolean),   SqlBoolean. Null, true, DataType.Boolean);
			AddScalarType(typeof(SqlByte),      SqlByte.    Null, true, DataType.Byte);
			AddScalarType(typeof(SqlDateTime),  SqlDateTime.Null, true, DataType.DateTime);
			AddScalarType(typeof(SqlDecimal),   SqlDecimal. Null, true, DataType.Decimal);
			AddScalarType(typeof(SqlDouble),    SqlDouble.  Null, true, DataType.Double);
			AddScalarType(typeof(SqlGuid),      SqlGuid.    Null, true, DataType.Guid);
			AddScalarType(typeof(SqlInt16),     SqlInt16.   Null, true, DataType.Int16);
			AddScalarType(typeof(SqlInt32),     SqlInt32.   Null, true, DataType.Int32);
			AddScalarType(typeof(SqlInt64),     SqlInt64.   Null, true, DataType.Int64);
			AddScalarType(typeof(SqlMoney),     SqlMoney.   Null, true, DataType.Money);
			AddScalarType(typeof(SqlSingle),    SqlSingle.  Null, true, DataType.Single);
			AddScalarType(typeof(SqlString),    SqlString.  Null, true, DataType.NVarChar);
			AddScalarType(typeof(SqlXml),       SqlXml.     Null, true, DataType.Xml);

			AddScalarType(typeof(DateTime),  DataType.DateTime2);

			SqlServerTypes.Configure(this);

			SetValueToSqlConverter(typeof(string), (sb,dt,_,v) => ConvertStringToSql(sb, dt.Type.DataType, (string)v));
			SetValueToSqlConverter(typeof(char),   (sb,dt,_,v) => ConvertCharToSql  (sb, dt, (char)v));
			SetValueToSqlConverter(typeof(byte[]), (sb, _,_,v) => ConvertBinaryToSql(sb, (byte[])v));
			SetValueToSqlConverter(typeof(Binary), (sb, _,_,v) => ConvertBinaryToSql(sb, ((Binary)v).ToArray()));

			SetDataType(typeof(string), new SqlDataType(DataType.NVarChar, typeof(string)));
			// in SQL Server DECIMAL=DECIMAL(18,0)
			SetDataType(typeof(decimal), new SqlDataType(DataType.Decimal, typeof(decimal), 18, 10));

			if (SystemDataSqlServerAttributeReader.SystemDataSqlClientProvider != null)
				AddMetadataReader(SystemDataSqlServerAttributeReader.SystemDataSqlClientProvider);
			if (SystemDataSqlServerAttributeReader.MicrosoftDataSqlClientProvider != null)
				AddMetadataReader(SystemDataSqlServerAttributeReader.MicrosoftDataSqlClientProvider);
			if (SystemDataSqlServerAttributeReader.MicrosoftSqlServerServerProvider != null)
				AddMetadataReader(SystemDataSqlServerAttributeReader.MicrosoftSqlServerServerProvider);
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
					Expression.Call(to, "Parse", [],
						Expression.New(
							MemberHelper.ConstructorOf(() => new SqlString("")),
							Expression.Call(
								Expression.Convert(p, typeof(object)),
								"ToString",
								[]))),
					p);
			}

			return base.TryGetConvertExpression(@from, to);
		}

		static readonly Action<StringBuilder, int> AppendConversionAction = AppendConversion;
		static void AppendConversion(StringBuilder stringBuilder, int value)
		{
			stringBuilder.Append(CultureInfo.InvariantCulture, $"char({value})");
		}

		internal static void ConvertStringToSql(StringBuilder stringBuilder, DataType dataType, string value)
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
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _)
					when value.Hour == 0 && value.Minute == 0 && value.Second == 0 && value.Millisecond == 0 :
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
					break;
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
					break;

				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _)
					when value.Hour == 0 && value.Minute == 0 && value.Second == 0 && value.Millisecond == 0 :
					stringBuilder.Append('N');
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FORMAT, value);
					break;
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _):
					stringBuilder.Append('N');
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_FORMAT, value);
					break;

				case (DataType.SmallDateTime, _, _):
					// don't use SMALLDATETIMEFROMPARTS as it doesn't accept seconds/milliseconds, which used for rounding
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, SMALLDATETIME_TYPED_FORMAT, value);
					break;

				case (DataType.Date, true, true):
					// DATEFROMPARTS ( year, month, day )
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_FROMPARTS_FORMAT, value.Year, value.Month, value.Day);
					break;
				case (DataType.Date, true, false):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_TYPED_FORMAT, value);
					break;
				case (DataType.Date, false, _):
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATE_AS_DATETIME_TYPED_FORMAT, value);
					break;

				case (DataType.DateTime2, true, true):
				{
					var precision = dt.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIME2 type precision is out-of-bounds: {precision}"));

					// DATETIME2FROMPARTS ( year, month, day, hour, minute, seconds, fractions, precision )
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DaTETIME2_FROMPARTS_FORMAT, value.Year, value.Month, value.Day, value.Hour, value.Minute, value.Second, GetFractionalSecondFromTicks(value.Ticks, precision), precision);
					break;
				}
				case (DataType.DateTime2, true, false):
				{
					var precision = dt.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIME2 type precision is out-of-bounds: {precision}"));

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME2_TYPED_FORMATS[precision], value);
					break;
				}
				case (DataType.DateTime2, false, _):
				{
					var precision = dt.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIME2 type precision is out-of-bounds: {precision}"));

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIME_WITH_PRECISION_FORMATS[precision], value);
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

		internal static string ConvertTimeSpanToString(TimeSpan value, int precision)
		{
			if (precision < 0 || precision > 7)
				throw new InvalidOperationException(FormattableString.Invariant($"TIME type precision is out-of-bounds: {precision}"));

			return value.ToString(TIME_RAW_FORMATS[precision], DateTimeFormatInfo.InvariantInfo);
		}

		internal static string ConvertDateTimeOffsetToString(DateTimeOffset value, int precision)
		{
			if (precision < 0 || precision > 7)
				throw new InvalidOperationException(FormattableString.Invariant($"DATETIMEOFFSET type precision is out-of-bounds: {precision}"));

			return value.ToString(DATETIMEOFFSET_RAW_FORMATS[precision], DateTimeFormatInfo.InvariantInfo);
		}

		static void ConvertTimeSpanToSql(StringBuilder stringBuilder, SqlDataType sqlDataType, TimeSpan value, bool supportsTime, bool supportsFromParts)
		{
			switch (sqlDataType.Type.DataType, supportsTime, supportsFromParts)
			{
				case (DataType.Int64, _, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"TIME type precision is out-of-bounds: {precision}"));

					var ticks = value.Ticks - (value.Ticks % ValueExtensions.TICKS_DIVIDERS[precision]);

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, TIME_TICKS_FORMAT, ticks);
					break;
				}
				case (DataType.Text, _, _) or (DataType.Char, _, _) or (DataType.VarChar, _, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"TIME type precision is out-of-bounds: {precision}"));

					var ticks = value.Ticks - (value.Ticks % ValueExtensions.TICKS_DIVIDERS[precision]);

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "'{0:c}'", TimeSpan.FromTicks(ticks));
					break;
				}
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _) or (_, false, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"TIME type precision is out-of-bounds: {precision}"));

					var ticks = value.Ticks - (value.Ticks % ValueExtensions.TICKS_DIVIDERS[precision]);

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, "N'{0:c}'", TimeSpan.FromTicks(ticks));
					break;
				}
				default:
				{
					if (value < TimeSpan.Zero || value >= TimeSpan.FromDays(1))
						throw new InvalidOperationException($"TIME value is out-of-bounds: {value:c}");

					var precision = sqlDataType.Type.Precision ?? 7;

					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"TIME type precision is out-of-bounds: {precision}"));

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
		private static long GetFractionalSecondFromTicks(long ticks, int precision) => (ticks % ValueExtensions.TICKS_DIVIDERS[0]) / ValueExtensions.TICKS_DIVIDERS[precision];

#if NET8_0_OR_GREATER
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
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIMEOFFSET type precision is out-of-bounds: {precision}"));

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_FORMATS[precision], value);
					break;
				}
				case (DataType.NText, _, _) or (DataType.NChar, _, _) or (DataType.NVarChar, _, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIMEOFFSET type precision is out-of-bounds: {precision}"));

					stringBuilder.Append('N');
					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_FORMATS[precision], value);
					break;
				}

				case (DataType.Date, _, _) or (DataType.DateTime, _, _) or (DataType.DateTime2, _, _) or (DataType.SmallDateTime, _, _):
					ConvertDateTimeToSql(stringBuilder, sqlDataType, value.LocalDateTime, v2008plus, supportsFromParts);
					return;

				case (_, false, _):
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIMEOFFSET type precision is out-of-bounds: {precision}"));

					stringBuilder.AppendFormat(CultureInfo.InvariantCulture, DATETIMEOFFSET_AS_DATETIME_TYPED_FORMATS[precision], value.LocalDateTime);
					break;
				}

				default:
				{
					var precision = sqlDataType.Type.Precision ?? 7;
					if (precision < 0 || precision > 7)
						throw new InvalidOperationException(FormattableString.Invariant($"DATETIMEOFFSET type precision is out-of-bounds: {precision}"));

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

		static MappingSchema Instance2005 = new SqlServer2005MappingSchema();
		static MappingSchema Instance2008 = new SqlServer2008MappingSchema();
		static MappingSchema Instance2012 = new SqlServer2012MappingSchema();
		static MappingSchema Instance2014 = new SqlServer2014MappingSchema();
		static MappingSchema Instance2016 = new SqlServer2016MappingSchema();
		static MappingSchema Instance2017 = new SqlServer2017MappingSchema();
		static MappingSchema Instance2019 = new SqlServer2019MappingSchema();
		static MappingSchema Instance2022 = new SqlServer2022MappingSchema();
		static MappingSchema Instance2025 = new SqlServer2025MappingSchema();

		sealed class SqlServer2005MappingSchema : LockedMappingSchema
		{
			public SqlServer2005MappingSchema() : base(ProviderName.SqlServer2005, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				AddScalarType(typeof(DateTime) , DataType.DateTime);

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , false, false));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, false, false));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , false, false));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , false, false));
#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , false, false));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type from, Type to)
			{
				return Instance.TryGetConvertExpression(from, to);
			}
		}

		sealed class SqlServer2008MappingSchema : LockedMappingSchema
		{
			public SqlServer2008MappingSchema() : base(ProviderName.SqlServer2008, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, false));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, false));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, false));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, false));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, false));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type from, Type to)
			{
				return Instance.TryGetConvertExpression(from, to);
			}
		}

		sealed class SqlServer2012MappingSchema : LockedMappingSchema
		{
			public SqlServer2012MappingSchema() : base(ProviderName.SqlServer2012, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		sealed class SqlServer2014MappingSchema : LockedMappingSchema
		{
			public SqlServer2014MappingSchema() : base(ProviderName.SqlServer2014, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		sealed class SqlServer2016MappingSchema : LockedMappingSchema
		{
			public SqlServer2016MappingSchema() : base(ProviderName.SqlServer2016, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		sealed class SqlServer2017MappingSchema : LockedMappingSchema
		{
			public SqlServer2017MappingSchema() : base(ProviderName.SqlServer2017, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		sealed class SqlServer2019MappingSchema : LockedMappingSchema
		{
			public SqlServer2019MappingSchema() : base(ProviderName.SqlServer2019, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		sealed class SqlServer2022MappingSchema : LockedMappingSchema
		{
			public SqlServer2022MappingSchema() : base(ProviderName.SqlServer2022, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		sealed class SqlServer2025MappingSchema : LockedMappingSchema
		{
			public SqlServer2025MappingSchema() : base(ProviderName.SqlServer2025, Instance)
			{
				ColumnNameComparer = StringComparer.OrdinalIgnoreCase;

				SetValueToSqlConverter(typeof(TimeSpan)      , (sb,dt,_,v) => ConvertTimeSpanToSql      (sb, dt, (TimeSpan)v             , true, true));
				SetValueToSqlConverter(typeof(SqlDateTime)   , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)(SqlDateTime)v, true, true));
				SetValueToSqlConverter(typeof(DateTime)      , (sb,dt,_,v) => ConvertDateTimeToSql      (sb, dt, (DateTime)v             , true, true));
				SetValueToSqlConverter(typeof(DateTimeOffset), (sb,dt,_,v) => ConvertDateTimeOffsetToSql(sb, dt, (DateTimeOffset)v       , true, true));

#if NET8_0_OR_GREATER
				SetValueToSqlConverter(typeof(DateOnly)      , (sb,dt,_,v) => ConvertDateToSql          (sb, dt, (DateOnly)v             , true, true));
#endif
			}

			public override LambdaExpression? TryGetConvertExpression(Type @from, Type to)
			{
				return Instance.TryGetConvertExpression(@from, to);
			}
		}

		const string SDS = ".System";
		const string MDS = ".Microsoft";

		public sealed class SqlServer2005MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2005 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2005 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2005MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2005 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2005 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2008MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2008 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2008 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2008MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2008 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2008 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2012MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2012 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2012 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2012MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2012 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2012 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2014MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2014 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2014 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2014MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2014 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2014 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2016MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2016 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2016 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2016MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2016 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2016 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2017MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2017 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2017 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2017MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2017 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2017 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2019MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2019 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2019 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2019MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2019 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2019 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2022MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2022 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2022 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2022MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2022 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2022 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2025MappingSchemaSystem   () : LockedMappingSchema(ProviderName.SqlServer2025 + SDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.SystemDataSqlClient   ).MappingSchema, Instance2025 }.Where(ms => ms != null).ToArray()!);
		public sealed class SqlServer2025MappingSchemaMicrosoft() : LockedMappingSchema(ProviderName.SqlServer2025 + MDS, new[] { SqlServerProviderAdapter.GetInstance(SqlServerProvider.MicrosoftDataSqlClient).MappingSchema, Instance2025 }.Where(ms => ms != null).ToArray()!);
	}
}
