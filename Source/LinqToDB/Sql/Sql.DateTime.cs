using System;
using System.Globalization;

using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public partial class Sql
	{
		[Enum]
		public enum DateParts
		{
			Year        =  0,
			Quarter     =  1,
			Month       =  2,
			DayOfYear   =  3,
			Day         =  4,
			/// <summary>
			/// This date part behavior depends on used database and also depends on where if calculated - in C# code or in database.
			/// Eeach database could have own week numbering logic, see notes below.
			///
			/// Current implementation uses following schemas per-provider:
			/// C# evaluation:
			/// <para>
			/// <c>CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday)</c>
			/// </para>
			/// Databases:
			/// <list type="bullet">
			/// <item>US numbering schema used by:
			/// <list type="bullet">
			/// <item>MS Access</item>
			/// <item>SQL CE</item>
			/// <item>SQL Server</item>
			/// <item>SAP/Sybase ASE</item>
			/// <item>Informix</item>
			/// <item>Ydb</item>
			/// </list>
			/// </item>
			/// <item>US 0-based numbering schema used by MySQL database</item>
			/// <item>ISO numbering schema with incorrect numbering of first week used by SAP HANA database</item>
			/// <item>ISO numbering schema with proper numbering of first week used by:
			/// <list type="bullet">
			/// <item>Firebird</item>
			/// <item>PostgreSQL</item>
			/// <item>ClickHouse</item>
			/// </list>
			/// </item>
			/// <item>Primitive (each 7 days counted as week) numbering schema:
			/// <list type="bullet">
			/// <item>DB2</item>
			/// <item>Oracle</item>
			/// </list>
			/// </item>
			/// <item>SQLite numbering logic cannot be classified by human being</item>
			/// </list>
			/// </summary>
			Week        =  5,
			WeekDay     =  6,
			Hour        =  7,
			Minute      =  8,
			Second      =  9,
			Millisecond = 10,
			/// <summary>Microseconds within the current second.</summary>
			Microsecond = 11,
			/// <summary>Nanoseconds within the current second.</summary>
			Nanosecond  = 12,
			/// <summary>100-nanosecond ticks within the current second.</summary>
			Tick        = 13,
		}

		#region DatePart

		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateTime? date)
		{
			return (int?)DatePartLong(part, date);
		}

		/// <summary>
		/// Returns the requested component of <paramref name="date"/> without narrowing the result to 32 bits.
		/// </summary>
		/// <param name="part">Date component to return.</param>
		/// <param name="date">Date value.</param>
		/// <returns>Requested date component, or <see langword="null"/> when <paramref name="date"/> is <see langword="null"/>.</returns>
		public static long? DatePartLong([SqlQueryDependent] DateParts part, [ExprParameter] DateTime? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				DateParts.Year          => date.Value.Year,
				DateParts.Quarter       => (date.Value.Month - 1) / 3 + 1,
				DateParts.Month         => date.Value.Month,
				DateParts.DayOfYear     => date.Value.DayOfYear,
				DateParts.Day           => date.Value.Day,
				DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1,
				DateParts.Hour          => date.Value.Hour,
				DateParts.Minute        => date.Value.Minute,
				DateParts.Second        => date.Value.Second,
				DateParts.Millisecond   => date.Value.Millisecond,
				DateParts.Microsecond   => date.Value.Ticks % TimeSpan.TicksPerSecond / 10,
				DateParts.Nanosecond    => date.Value.Ticks % TimeSpan.TicksPerSecond * 100,
				DateParts.Tick          => date.Value.Ticks % TimeSpan.TicksPerSecond,
				_                           => throw new InvalidOperationException(),
			};
		}

		#endregion DatePart

		#region DateAdd

		public static DateTime? DateAdd([SqlQueryDependent] DateParts part, double? number, DateTime? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				DateParts.Year          => date.Value.AddYears((int)number),
				DateParts.Quarter       => date.Value.AddMonths((int)number * 3),
				DateParts.Month         => date.Value.AddMonths((int)number),
				DateParts.Day           => date.Value.AddDays(number.Value),
				DateParts.Week          => date.Value.AddDays(number.Value * 7),
				DateParts.Hour          => date.Value.AddHours(number.Value),
				DateParts.Minute        => date.Value.AddMinutes(number.Value),
				DateParts.Second        => date.Value.AddSeconds(number.Value),
				DateParts.Millisecond   => date.Value.AddMilliseconds(number.Value),
#if NET7_0_OR_GREATER
				DateParts.Microsecond   => date.Value.AddMicroseconds(number.Value),
#else
				DateParts.Microsecond   => date.Value.AddTicks((long)(number.Value * 10)),
#endif
				DateParts.Nanosecond    => date.Value.AddTicks((long)(number.Value / 100)),
				DateParts.Tick          => date.Value.AddTicks((long)number.Value),
				_                       => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateDiff

		sealed class DateDiffBuilder : IExtensionCallBuilder
		{
			public static DbDataType GetResultType(ISqlExtensionBuilder builder)
			{
				return builder.Mapping.GetDbDataType(string.Equals(builder.Member.Name, nameof(DateDiffLong), StringComparison.Ordinal) ? typeof(long) : typeof(int));
			}

			public static string DatePartToStr(DateParts part)
			{
				return part switch
				{
					DateParts.Year        => "year",
					DateParts.Quarter     => "quarter",
					DateParts.Month       => "month",
					DateParts.DayOfYear   => "dayofyear",
					DateParts.Day         => "day",
					DateParts.Week        => "week",
					DateParts.WeekDay     => "weekday",
					DateParts.Hour        => "hour",
					DateParts.Minute      => "minute",
					DateParts.Second      => "second",
					DateParts.Millisecond => "millisecond",
					DateParts.Microsecond => "microsecond",
					DateParts.Nanosecond  => "nanosecond",
					DateParts.Tick        => "nanosecond",
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
			}

			public void Build(ISqlExtensionBuilder builder)
			{
				var part      = builder.GetValue<DateParts>(0);
				var startdate = builder.GetExpression(1);
				var endDate   = builder.GetExpression(2);

				if (startdate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var isMySql  = string.Equals(builder.Expression, "TIMESTAMPDIFF", StringComparison.OrdinalIgnoreCase);
				var sqlPart  = isMySql && part is DateParts.Nanosecond or DateParts.Tick ? DateParts.Microsecond : part;
				var partSql  = new SqlFragment(DatePartToStr(sqlPart));
				var result   = (ISqlExpression)new SqlFunction(GetResultType(builder), builder.Expression, partSql, startdate, endDate);

				if (part == DateParts.Tick)
					result = isMySql ? builder.Mul(result, 10) : builder.Div(result, 100);
				else if (part == DateParts.Nanosecond && isMySql)
					result = builder.Mul(result, 1000);

				builder.ResultExpression = result;
			}
		}

		sealed class DateDiffBuilderFirebird : IExtensionCallBuilder
		{
			public static string DatePartToStr(DateParts part)
			{
				return part switch
				{
					DateParts.Year        => "year",
					DateParts.Quarter     => "quarter",
					DateParts.Month       => "month",
					DateParts.DayOfYear   => "dayofyear",
					DateParts.Day         => "day",
					DateParts.Week        => "week",
					DateParts.WeekDay     => "weekday",
					DateParts.Hour        => "hour",
					DateParts.Minute      => "minute",
					DateParts.Second      => "second",
					DateParts.Millisecond => "millisecond",
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
			}

			public void Build(ISqlExtensionBuilder builder)
			{
				var part      = builder.GetValue<DateParts>(0);
				var startdate = builder.GetExpression(1);
				var endDate   = builder.GetExpression(2);

				if (startdate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var partSql   = new SqlFragment(DatePartToStr(part));

				var type = part switch
				{
					// FB 4.0.1+
					DateParts.Millisecond => builder.Mapping.GetDbDataType(typeof(decimal)).WithPrecisionScale(18, 1),
					_                     => builder.Mapping.GetDbDataType(typeof(long)),
				};

				builder.ResultExpression = new SqlFunction(type, "DATEDIFF", partSql, startdate, endDate);
			}
		}

		sealed class DateDiffBuilderFirebird3Minus : IExtensionCallBuilder
		{
			public static string DatePartToStr(DateParts part)
			{
				return part switch
				{
					DateParts.Year        => "year",
					DateParts.Quarter     => "quarter",
					DateParts.Month       => "month",
					DateParts.DayOfYear   => "dayofyear",
					DateParts.Day         => "day",
					DateParts.Week        => "week",
					DateParts.WeekDay     => "weekday",
					DateParts.Hour        => "hour",
					DateParts.Minute      => "minute",
					DateParts.Second      => "second",
					DateParts.Millisecond => "millisecond",
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
			}

			public void Build(ISqlExtensionBuilder builder)
			{
				var part      = builder.GetValue<DateParts>(0);
				var startdate = builder.GetExpression(1);
				var endDate   = builder.GetExpression(2);

				if (startdate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var partSql   = new SqlFragment(DatePartToStr(part));

				builder.ResultExpression = new SqlFunction(builder.Mapping.GetDbDataType(typeof(long)), "DATEDIFF", partSql, startdate, endDate);
			}
		}

		sealed class DateDiffBuilderSapHana : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startdate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				if (startdate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var (funcName, divider, dbType) = part switch
				{
					DateParts.Day         => ("Days_Between",        1, builder.Mapping.GetDbDataType(typeof(int))),
					DateParts.Hour        => ("Seconds_Between",  3600, builder.Mapping.GetDbDataType(typeof(long))),
					DateParts.Minute      => ("Seconds_Between",    60, builder.Mapping.GetDbDataType(typeof(long))),
					DateParts.Second      => ("Seconds_Between",     1, builder.Mapping.GetDbDataType(typeof(long))),
					DateParts.Millisecond => ("Nano100_Between", 10000, builder.Mapping.GetDbDataType(typeof(long))),
					DateParts.Microsecond => ("Nano100_Between",    10, builder.Mapping.GetDbDataType(typeof(long))),
					DateParts.Nanosecond  => ("Nano100_Between",     1, builder.Mapping.GetDbDataType(typeof(long))),
					DateParts.Tick        => ("Nano100_Between",     1, builder.Mapping.GetDbDataType(typeof(long))),
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};

				ISqlExpression func = new SqlFunction(dbType, funcName, startdate, endDate);
				if (divider != 1)
					func = builder.Div(func, divider);

				if (part == DateParts.Nanosecond)
					func = builder.Mul(func, 100);

				builder.ResultExpression = func;
			}
		}

		sealed class DateDiffBuilderDB2 : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startDate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				if (startDate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var secondsExpr = builder.Mul<int>(builder.Sub<int>(
						new SqlFunction(builder.Mapping.GetDbDataType(typeof(int)), "Days", endDate),
						new SqlFunction(builder.Mapping.GetDbDataType(typeof(int)), "Days", startDate)),
					new SqlValue(86400));

				var midnight = builder.Sub<int>(
					new SqlFunction(builder.Mapping.GetDbDataType(typeof(int)), "MIDNIGHT_SECONDS", endDate),
					new SqlFunction(builder.Mapping.GetDbDataType(typeof(int)), "MIDNIGHT_SECONDS", startDate));

				var resultExpr = builder.Add<int>(secondsExpr, midnight);

				switch (part)
				{
					case DateParts.Day         : resultExpr = builder.Div(resultExpr, 86400); break;
					case DateParts.Hour        : resultExpr = builder.Div(resultExpr, 3600);  break;
					case DateParts.Minute      : resultExpr = builder.Div(resultExpr, 60);    break;
					case DateParts.Second      : break;
					case DateParts.Millisecond :
						resultExpr = builder.Add<int>(
							builder.Mul(resultExpr, 1000),
							builder.Div(
								builder.Sub<int>(
									new SqlFunction(builder.Mapping.GetDbDataType(typeof(int)), "MICROSECOND", endDate),
									new SqlFunction(builder.Mapping.GetDbDataType(typeof(int)), "MICROSECOND", startDate)),
								1000));
						break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = resultExpr;
			}
		}

		sealed class DateDiffBuilderSQLite : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);

				if (startDate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var expStr = "round((julianday({1}) - julianday({0}))";
				expStr += part switch
				{
					DateParts.Day         => ")",
					DateParts.Hour        => " * 24)",
					DateParts.Minute      => " * 1440)",
					DateParts.Second      => " * 86400)",
					DateParts.Millisecond => " * 86400000)",
					DateParts.Microsecond => " * 86400000000)",
					DateParts.Nanosecond  => " * 86400000000000)",
					DateParts.Tick        => " * 864000000000)",
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(DateDiffBuilder.GetResultType(builder), expStr, startDate, endDate );
			}
		}

		sealed class DateDiffBuilderPostgreSql : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1)!;
				var endDate = builder.GetExpression(2)!;

				// Types:
				// EXTRACT: numeric
				// DATE_PART: double precision
				var (expStr, dbType, precedence) = part switch
				{
					DateParts.Year        => ("DATE_PART('year', {1}::date) - DATE_PART('year', {0}::date)"                                                                         , builder.Mapping.GetDbDataType(typeof(double)) , Precedence.Subtraction),
					DateParts.Month       => ("(DATE_PART('year', {1}::date) - DATE_PART('year', {0}::date)) * 12 + (DATE_PART('month', {1}::date) - DATE_PART('month', {0}::date))", builder.Mapping.GetDbDataType(typeof(double)) , Precedence.Additive),
					DateParts.Week        => ("TRUNC(DATE_PART('day', {1}::timestamp - {0}::timestamp) / 7)"                                                                        , builder.Mapping.GetDbDataType(typeof(int))    , Precedence.Primary),
					DateParts.Day         => ("EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) / 86400"                                                                       , builder.Mapping.GetDbDataType(typeof(decimal)), Precedence.Multiplicative),
					DateParts.Hour        => ("EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) / 3600"                                                                        , builder.Mapping.GetDbDataType(typeof(decimal)), Precedence.Multiplicative),
					DateParts.Minute      => ("EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) / 60"                                                                          , builder.Mapping.GetDbDataType(typeof(decimal)), Precedence.Multiplicative),
					DateParts.Second      => ("EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp))"                                                                               , builder.Mapping.GetDbDataType(typeof(decimal)), Precedence.Primary),
					DateParts.Millisecond => ("ROUND(EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) * 1000)"                                                                 , builder.Mapping.GetDbDataType(typeof(int))    , Precedence.Multiplicative),
					DateParts.Microsecond => ("TRUNC(EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) * 1000000)"                                                              , DateDiffBuilder.GetResultType(builder)        , Precedence.Multiplicative),
					DateParts.Nanosecond  => ("TRUNC(EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) * 1000000000)"                                                           , DateDiffBuilder.GetResultType(builder)        , Precedence.Multiplicative),
					DateParts.Tick        => ("TRUNC(EXTRACT(EPOCH FROM ({1}::timestamp - {0}::timestamp)) * 10000000)"                                                             , DateDiffBuilder.GetResultType(builder)        , Precedence.Multiplicative),
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};

				builder.ResultExpression = new SqlExpression(dbType, expStr, precedence, startDate, endDate);
			}
		}

		sealed class DateDiffBuilderAccess : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);

				if (startDate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var expStr = "DATEDIFF('";

#pragma warning disable CA2208 // Instantiate argument exceptions correctly
				expStr += part switch
				{
					DateParts.Year        => "yyyy",
					DateParts.Quarter     => "q",
					DateParts.Month       => "m",
					DateParts.DayOfYear   => "y",
					DateParts.Day         => "d",
					DateParts.WeekDay     => "w",
					DateParts.Week        => "ww",
					DateParts.Hour        => "h",
					DateParts.Minute      => "n",
					DateParts.Second      => "s",
					DateParts.Millisecond => throw new ArgumentOutOfRangeException(nameof(part), part, "Access doesn't support milliseconds interval."),
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
#pragma warning restore CA2208 // Instantiate argument exceptions correctly

				expStr += "', {0}, {1})";

				builder.ResultExpression = new SqlExpression(builder.Mapping.GetDbDataType(typeof(int)), expStr, startDate, endDate);
			}
		}

		sealed class DateDiffBuilderOracle : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part = builder.GetValue<DateParts>(0);
				var startDate = builder.GetExpression(1);
				var endDate = builder.GetExpression(2);

				if (startDate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var (expStr, precedence) = part switch
				{
					// DateParts.Year        => "({1} - {0}) / 365",
					// DateParts.Month       => "({1} - {0}) / 30",
					DateParts.Week        => ("(CAST({1} as DATE) - CAST({0} as DATE)) / 7"    , Precedence.Multiplicative),
					DateParts.Day         => ( "CAST({1} as DATE) - CAST({0} as DATE)"         , Precedence.Subtraction),
					DateParts.Hour        => ("(CAST({1} as DATE) - CAST({0} as DATE)) * 24"   , Precedence.Multiplicative),
					DateParts.Minute      => ("(CAST({1} as DATE) - CAST({0} as DATE)) * 1440" , Precedence.Multiplicative),
					DateParts.Second      => ("(CAST({1} as DATE) - CAST({0} as DATE)) * 86400", Precedence.Multiplicative),

					// this is tempting to use but leads to precision loss on big intervals
					//DateParts.Millisecond => "1000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)) + (CAST ({1} as DATE) - CAST ({0} as DATE)) * 86400)",

					// could be really ugly on big start/end expressions
					DateParts.Millisecond => ("1000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(MINUTE FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(HOUR FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 24 * EXTRACT(DAY FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)))))", Precedence.Multiplicative),
					DateParts.Microsecond => ("1000000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(MINUTE FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(HOUR FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 24 * EXTRACT(DAY FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)))))", Precedence.Multiplicative),
					DateParts.Nanosecond => ("1000000000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(MINUTE FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(HOUR FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 24 * EXTRACT(DAY FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)))))", Precedence.Multiplicative),
					DateParts.Tick => ("10000000 * (EXTRACT(SECOND FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(MINUTE FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 60 * (EXTRACT(HOUR FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP))"
					+ " + 24 * EXTRACT(DAY FROM CAST ({1} as TIMESTAMP) - CAST ({0} as TIMESTAMP)))))", Precedence.Multiplicative),
					_                     => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
				builder.ResultExpression = new SqlExpression(DateDiffBuilder.GetResultType(builder), expStr, precedence, startDate, endDate);
			}
		}

		sealed class DateDiffBuilderClickHouse : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startDate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				if (startDate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				string? unit = null;
				switch (part)
				{
					case DateParts.Year   : unit = "year"   ; break;
					case DateParts.Quarter: unit = "quarter"; break;
					case DateParts.Month  : unit = "month"  ; break;
					case DateParts.Week   : unit = "week"   ; break;
					case DateParts.Day    : unit = "day"    ; break;
					case DateParts.Hour   : unit = "hour"   ; break;
					case DateParts.Minute : unit = "minute" ; break;
					case DateParts.Second : unit = "second" ; break;

					case DateParts.Millisecond:
						builder.ResultExpression = new SqlExpression(
							builder.Mapping.GetDbDataType(typeof(long?)),
							"toUnixTimestamp64Milli(toDateTime64({1}, 3)) - toUnixTimestamp64Milli(toDateTime64({0}, 3))",
							Precedence.Subtraction,
							startDate,
							endDate);
						break;

					case DateParts.Microsecond:
						builder.ResultExpression = new SqlExpression(
							DateDiffBuilder.GetResultType(builder),
							"toUnixTimestamp64Micro(toDateTime64({1}, 6)) - toUnixTimestamp64Micro(toDateTime64({0}, 6))",
							Precedence.Subtraction,
							startDate,
							endDate);
						break;

					case DateParts.Nanosecond:
					case DateParts.Tick:
						var nanos = new SqlExpression(
							DateDiffBuilder.GetResultType(builder),
							"toUnixTimestamp64Nano(toDateTime64({1}, 9)) - toUnixTimestamp64Nano(toDateTime64({0}, 9))",
							Precedence.Subtraction,
							startDate,
							endDate);
						builder.ResultExpression = part == DateParts.Tick ? builder.Div(nanos, 100) : nanos;
						break;

					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				if (unit != null)
					builder.ResultExpression = new SqlFunction(builder.Mapping.GetDbDataType(typeof(long)), "date_diff", new SqlValue(unit), startDate, endDate);
			}
		}

		sealed class DateDiffBuilderYdb : IExtensionCallBuilder
		{
			public void Build(ISqlExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startDate  = builder.GetExpression(1);
				var endDate    = builder.GetExpression(2);

				if (startDate is null || endDate is null)
				{
					builder.IsConvertible = false;
					return;
				}

				var divisor = part switch
				{
					DateParts.Week        => 1_000_000L * 60 * 60 * 24 * 7,
					DateParts.Day         => 1_000_000L * 60 * 60 * 24,
					DateParts.Hour        => 1_000_000L * 60 * 60,
					DateParts.Minute      => 1_000_000L * 60,
					DateParts.Second      => 1_000_000L,
					DateParts.Millisecond => 1_000L,
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};

				var longType     = builder.Mapping.GetDbDataType(typeof(long));
				var intervalType = builder.Mapping.GetDbDataType(typeof(TimeSpan)).WithDataType(DataType.Interval);

				// endDate - startDate yields a YQL Interval; CAST(... AS Int64) converts it to its integer
				// microsecond count, which integer-divides by the per-unit microseconds. Dividing the raw
				// Interval would keep it an Interval (not the scalar count linq2db's DateDiff expects). The
				// subtraction must be typed Interval (not Int64) so the cast isn't pruned as a no-op long→long.
				var microseconds = new SqlCastExpression(
					new SqlBinaryExpression(intervalType, endDate, "-", startDate),
					longType,
					null,
					isMandatory: true);

				builder.ResultExpression = new SqlBinaryExpression(
					longType,
					microseconds,
					"/",
					new SqlValue(divisor));
			}
		}

		[CLSCompliant(false)]
		[Extension(               "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.MySql,      "TIMESTAMPDIFF", BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.DB2,        "",              BuilderType = typeof(DateDiffBuilderDB2))]
		[Extension(PN.SapHana,    "",              BuilderType = typeof(DateDiffBuilderSapHana))]
		[Extension(PN.Firebird25, "",              BuilderType = typeof(DateDiffBuilderFirebird3Minus))]
		[Extension(PN.Firebird3,  "",              BuilderType = typeof(DateDiffBuilderFirebird3Minus))]
		[Extension(PN.Firebird,   "",              BuilderType = typeof(DateDiffBuilderFirebird))]
		[Extension(PN.SQLite,     "",              BuilderType = typeof(DateDiffBuilderSQLite))]
		[Extension(PN.Oracle,     "",              BuilderType = typeof(DateDiffBuilderOracle))]
		[Extension(PN.PostgreSQL, "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Extension(PN.Access,     "",              BuilderType = typeof(DateDiffBuilderAccess))]
		[Extension(PN.ClickHouse, "",              BuilderType = typeof(DateDiffBuilderClickHouse))]
		[Extension(PN.Ydb,        "",              BuilderType = typeof(DateDiffBuilderYdb))]
		[Extension(PN.DuckDB,     "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		public static int? DateDiff(DateParts part, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return part switch
			{
				DateParts.Day         => (int)(endDate - startDate).Value.TotalDays,
				DateParts.Hour        => (int)(endDate - startDate).Value.TotalHours,
				DateParts.Minute      => (int)(endDate - startDate).Value.TotalMinutes,
				DateParts.Second      => (int)(endDate - startDate).Value.TotalSeconds,
				DateParts.Millisecond => (int)(endDate - startDate).Value.TotalMilliseconds,
				DateParts.Microsecond => (int)((endDate - startDate).Value.Ticks / 10),
				DateParts.Nanosecond  => (int)((endDate - startDate).Value.Ticks * 100),
				DateParts.Tick        => (int)(endDate - startDate).Value.Ticks,
				_                     => throw new InvalidOperationException(),
			};
		}

		/// <summary>
		/// Returns the number of requested boundaries crossed between two date values using a 64-bit result.
		/// </summary>
		/// <param name="part">Date component used to measure the difference.</param>
		/// <param name="startDate">Start date.</param>
		/// <param name="endDate">End date.</param>
		/// <returns>The difference, or <see langword="null"/> when either argument is <see langword="null"/>.</returns>
		[CLSCompliant(false)]
		[Extension(                  "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.SqlServer,     "DateDiff_Big",  BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.SqlServer2005, "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.SqlServer2008, "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.SqlServer2012, "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.SqlServer2014, "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.MySql,         "TIMESTAMPDIFF", BuilderType = typeof(DateDiffBuilder))]
		[Extension(PN.DB2,           "",              BuilderType = typeof(DateDiffBuilderDB2))]
		[Extension(PN.SapHana,       "",              BuilderType = typeof(DateDiffBuilderSapHana))]
		[Extension(PN.Firebird25,    "",              BuilderType = typeof(DateDiffBuilderFirebird3Minus))]
		[Extension(PN.Firebird3,     "",              BuilderType = typeof(DateDiffBuilderFirebird3Minus))]
		[Extension(PN.Firebird,      "",              BuilderType = typeof(DateDiffBuilderFirebird))]
		[Extension(PN.SQLite,        "",              BuilderType = typeof(DateDiffBuilderSQLite))]
		[Extension(PN.Oracle,        "",              BuilderType = typeof(DateDiffBuilderOracle))]
		[Extension(PN.PostgreSQL,    "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Extension(PN.Access,        "",              BuilderType = typeof(DateDiffBuilderAccess))]
		[Extension(PN.ClickHouse,    "",              BuilderType = typeof(DateDiffBuilderClickHouse))]
		[Extension(PN.Ydb,           "",              BuilderType = typeof(DateDiffBuilderYdb))]
		[Extension(PN.DuckDB,        "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		public static long? DateDiffLong(DateParts part, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return part switch
			{
				DateParts.Day         => (long)(endDate - startDate).Value.TotalDays,
				DateParts.Hour        => (long)(endDate - startDate).Value.TotalHours,
				DateParts.Minute      => (long)(endDate - startDate).Value.TotalMinutes,
				DateParts.Second      => (long)(endDate - startDate).Value.TotalSeconds,
				DateParts.Millisecond => (long)(endDate - startDate).Value.TotalMilliseconds,
				DateParts.Microsecond => (endDate - startDate).Value.Ticks / 10,
				DateParts.Nanosecond  => (endDate - startDate).Value.Ticks * 100,
				DateParts.Tick        => (endDate - startDate).Value.Ticks,
				_                     => throw new InvalidOperationException(),
			};
		}

		#endregion
	}
}
