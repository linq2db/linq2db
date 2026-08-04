using System;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;

using LinqToDB.Expressions;
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
			/// <summary>Microseconds within the current second, from 0 through 999999.</summary>
			Microsecond = 11,
			/// <summary>Nanoseconds within the current second, from 0 through 999999900 in 100-nanosecond increments.</summary>
			Nanosecond  = 12,
			/// <summary>100-nanosecond ticks within the current second, from 0 through 9999999.</summary>
			Tick        = 13,
		}

		#region DatePart

		/// <summary>
		/// Returns the requested component of <paramref name="date"/>.
		/// </summary>
		/// <param name="part">Date component to return.</param>
		/// <param name="date">Date value.</param>
		/// <returns>Requested date component, or <see langword="null"/> when <paramref name="date"/> is <see langword="null"/>.</returns>
		/// <remarks>
		/// <see cref="DateParts.Tick"/> returns the 100-nanosecond component within the current second, from 0 through 9999999.
		/// <see cref="DateParts.Nanosecond"/> returns the component in 100-nanosecond increments, from 0 through 999999900.
		/// </remarks>
		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateTime? date)
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
				DateParts.Microsecond   => (int)(date.Value.Ticks % TimeSpan.TicksPerSecond / 10),
				DateParts.Nanosecond    => (int)(date.Value.Ticks % TimeSpan.TicksPerSecond * 100),
				DateParts.Tick          => (int)(date.Value.Ticks % TimeSpan.TicksPerSecond),
				_                       => throw new InvalidOperationException(),
			};
		}

		/// <summary>
		/// Returns the requested component of <paramref name="date"/> without narrowing the result to 32 bits.
		/// </summary>
		/// <param name="part">Date component to return.</param>
		/// <param name="date">Date value.</param>
		/// <returns>Requested date component, or <see langword="null"/> when <paramref name="date"/> is <see langword="null"/>.</returns>
		/// <remarks>
		/// <see cref="DateParts.Tick"/> returns the 100-nanosecond component within the current second, from 0 through 9999999.
		/// <see cref="DateParts.Nanosecond"/> returns the component in 100-nanosecond increments, from 0 through 999999900.
		/// </remarks>
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

		/// <summary>
		/// Adds the requested number of date-part units to <paramref name="date"/>.
		/// </summary>
		/// <param name="part">Date-part unit to add.</param>
		/// <param name="number">Number of units to add.</param>
		/// <param name="date">Date value.</param>
		/// <returns>Adjusted date, or <see langword="null"/> when an argument is <see langword="null"/>.</returns>
		/// <remarks>
		/// One <see cref="DateParts.Tick"/> is 100 nanoseconds. Nanosecond values are converted to whole ticks by truncating
		/// toward zero: 99 and -99 add zero ticks, 100 adds one tick, and -101 adds negative one tick.
		/// </remarks>
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
				return builder.Mapping.GetDbDataType(typeof(int));
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

		sealed class DateDiffLongBuilder : IExtensionCallBuilder
		{
			private static readonly MethodInfo _datePartLongDateTime = MemberHelper.MethodOf(() => DatePartLong(DateParts.Year, (DateTime?)null));
			private static readonly MethodInfo _duckDbDateDiffMicrosecond = MemberHelper.MethodOf(() => DuckDbDateDiffMicrosecond(null, null));
			private static readonly MethodInfo _floorDecimal         = MemberHelper.MethodOf(() => Floor((decimal?)null));
			private static readonly MethodInfo _accessDecimal        = MemberHelper.MethodOf(() => AccessDecimal(null));
			private static readonly MethodInfo _accessMilliseconds   = MemberHelper.MethodOf(() => AccessMillisecondsSinceEpoch(null));
			private static readonly MethodInfo _accessDate            = MemberHelper.MethodOf(() => AccessDateFromDayIndex(null));
			private static readonly IValueConverter _checkedDecimalLongConverter = new ValueConverter<long, decimal>(
				value => value,
				value => checked((long)value),
				handlesNulls: false);

			[Expression(PN.Access, "CVar({0}) * 10000000000 / 10000000000", ServerSideOnly = true)]
			private static decimal? AccessDecimal(decimal? value) => value;

			// Access stores Date/Time as an OLE Automation double, while DateTime.FromOADate
			// rounds that value to the nearest millisecond. Reproduce that conversion before
			// counting boundaries: Access DatePart itself rounds values close to midnight and
			// can otherwise report the following calendar day for 23:59:59.999.
			[Expression(PN.Access,
				"CVar(IIf(CDbl({0}) >= 0, " +
					"Fix(CDbl({0}) * 86400000 + 0.5), " +
					"Fix(CDbl({0}) * 86400000 - 0.5) - 2 * (Fix(CDbl({0}) * 86400000 - 0.5) - Fix(Fix(CDbl({0}) * 86400000 - 0.5) / 86400000) * 86400000)) + 59926435200000)",
				ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
			private static decimal? AccessMillisecondsSinceEpoch(DateTime? value) => value?.Ticks / TimeSpan.TicksPerMillisecond;

			[Expression(PN.Access, "CDate({0} - 693593)", ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
			private static DateTime? AccessDateFromDayIndex(decimal? dayIndex) => dayIndex == null ? null : DateTime.MinValue.AddDays((double)dayIndex.Value);

			[Expression(PN.DuckDB, "date_diff('microsecond', {0}, {1})", ServerSideOnly = true, IsNullable = IsNullableType.IfAnyParameterNullable)]
			private static long? DuckDbDateDiffMicrosecond(DateTime? startDate, DateTime? endDate)
				=> startDate == null || endDate == null ? null : endDate.Value.Ticks / 10 - startDate.Value.Ticks / 10;

			public void Build(ISqlExtensionBuilder builder)
			{
				var part       = builder.GetValue<DateParts>(0);
				var startDate  = builder.Arguments[1];
				var endDate    = builder.Arguments[2];
				var dateType   = ((MethodInfo)builder.Member).GetParameters()[1].ParameterType;
				var isOffset   = dateType == typeof(DateTimeOffset?);

				if (isOffset && !SupportsDateTimeOffsetBoundaries(builder))
				{
					builder.IsConvertible = false;
					return;
				}

				var normalizedStart = NormalizeDate(startDate, isOffset);
				var normalizedEnd   = NormalizeDate(endDate,   isOffset);
				var indexPart       = part == DateParts.Nanosecond ? DateParts.Tick : part;
				var forceAccessDecimal = builder.Configuration?.Contains(PN.Access, StringComparison.Ordinal) == true;
				ISqlExpression? result;
				if (builder.Configuration?.Contains(PN.DuckDB, StringComparison.Ordinal) == true
					&& indexPart is DateParts.Microsecond or DateParts.Tick)
				{
					// DuckDB's DECIMAL division produces DOUBLE. Building an absolute microsecond/tick
					// index from calendar parts therefore loses low bits for modern dates (well above
					// 2^53). date_diff counts microsecond boundaries using the timestamp's native
					// integer representation, so scale that exact value for tick/nanosecond results.
					var difference = Expression.Call(_duckDbDateDiffMicrosecond, normalizedStart, normalizedEnd);
					result = builder.ConvertExpressionToSql(difference);

					if (result != null && indexPart == DateParts.Tick)
						result = builder.Mul(result, 10);
				}
				else
				{
					var startIndex = CreateBoundaryIndex(indexPart, normalizedStart, forceAccessDecimal);
					var endIndex   = CreateBoundaryIndex(indexPart, normalizedEnd,   forceAccessDecimal);
					var difference = Expression.Subtract(endIndex, startIndex);
					result = builder.ConvertExpressionToSql(difference);
				}

				if (result == null)
				{
					builder.IsConvertible = false;
					return;
				}

				var longType = builder.Mapping.GetDbDataType(typeof(long));

				if (forceAccessDecimal)
				{
					if (part == DateParts.Nanosecond)
						result = builder.Mul(result, 100);

					var decimalType = builder.Mapping.GetDbDataType(typeof(decimal)).WithSystemType(typeof(long));
					builder.ResultExpression = new SqlExpression(decimalType, "{0}", result)
						.WithResultConverter(_checkedDecimalLongConverter);
					return;
				}

				if (part == DateParts.Nanosecond)
				{
					if (builder.Configuration?.Contains(PN.ClickHouse, StringComparison.Ordinal) == true)
					{
						builder.ResultExpression = new SqlExpression(
							longType,
							"accurateCast(CAST({0} AS Decimal(38, 0)) * 100, 'Int64')",
							result);
						return;
					}

					if (builder.Configuration?.Contains(PN.MySql, StringComparison.Ordinal) == true
						|| builder.Configuration?.Contains("MariaDB", StringComparison.Ordinal) == true)
					{
						var decimalType = builder.Mapping.GetDbDataType(typeof(decimal)).WithPrecisionScale(38, 0);
						var decimalTicks = new SqlCastExpression(result, decimalType, null, isMandatory: true);
						var nanoseconds = new SqlBinaryExpression(
							decimalType,
							decimalTicks,
							"*",
							new SqlValue(decimalType, 100m));

						builder.ResultExpression = new SqlExpression(decimalType.WithSystemType(typeof(long)), "{0}", nanoseconds)
							.WithResultConverter(_checkedDecimalLongConverter);
						return;
					}

					result = builder.Mul(result, 100);
				}

				var cast = new SqlCastExpression(result, longType, null, isMandatory: true);

				// SQLite saturates an out-of-range REAL/NUMERIC-to-INTEGER cast instead of reporting
				// overflow. DateDiffLong(Nanosecond) has a checked contract, so guard its decimal
				// intermediate explicitly and execute an expression that SQLite reports as integer
				// overflow only for an out-of-range result. CASE evaluation is lazy in SQLite.
				if (part == DateParts.Nanosecond && builder.Configuration?.Contains(PN.SQLite, StringComparison.Ordinal) == true)
				{
					var decimalType = builder.Mapping.GetDbDataType(typeof(decimal));
					var inRange     = new SqlSearchCondition(
						false,
						canBeUnknown: null,
						new SqlPredicate.ExprExpr(result, SqlPredicate.Operator.GreaterOrEqual, new SqlValue(decimalType, (decimal)long.MinValue), unknownAsValue: null),
						new SqlPredicate.ExprExpr(result, SqlPredicate.Operator.LessOrEqual,    new SqlValue(decimalType, (decimal)long.MaxValue), unknownAsValue: null));
					var overflow = new SqlExpression(longType, "abs(-9223372036854775808)");

					builder.ResultExpression = new SqlConditionExpression(inRange, cast, overflow);
				}
				else
				{
					builder.ResultExpression = cast;
				}
			}

			private static bool SupportsDateTimeOffsetBoundaries(ISqlExtensionBuilder builder)
			{
				var configuration = builder.Configuration;
				if (configuration == null)
					return false;

				var providerPreservesInstant = configuration.Contains(PN.PostgreSQL, StringComparison.Ordinal)
					|| configuration.Contains(PN.Oracle,     StringComparison.Ordinal)
					|| configuration.Contains(PN.ClickHouse, StringComparison.Ordinal)
					|| configuration.Contains(PN.DuckDB,     StringComparison.Ordinal)
					|| configuration.Contains(PN.SQLite,     StringComparison.Ordinal)
					|| configuration.Contains(PN.Firebird4,  StringComparison.Ordinal)
					|| configuration.Contains(PN.Firebird5,  StringComparison.Ordinal)
					|| configuration.Contains(PN.SqlServer,  StringComparison.Ordinal)
						&& !configuration.Contains(PN.SqlServer2005, StringComparison.Ordinal);

				if (!providerPreservesInstant)
					return false;

				var startDate = builder.GetExpression(1);
				var endDate   = builder.GetExpression(2);
				if (startDate == null || endDate == null)
					return false;

				return PreservesInstant(QueryHelper.GetDbDataType(startDate, builder.Mapping))
					&& PreservesInstant(QueryHelper.GetDbDataType(endDate, builder.Mapping));

				bool PreservesInstant(DbDataType dataType)
				{
					if (configuration.Contains(PN.ClickHouse, StringComparison.Ordinal))
						return dataType.DataType is DataType.DateTime or DataType.DateTime2 or DataType.DateTime64 or DataType.SmallDateTime or DataType.DateTimeOffset;

					return dataType.DataType is DataType.DateTimeTz or DataType.DateTime2Tz or DataType.DateTimeOffset;
				}
			}

			private static Expression NormalizeDate(Expression date, bool isOffset)
			{
				if (!isOffset)
					return date.Type == typeof(DateTime?) ? date : Expression.Convert(date, typeof(DateTime?));

				var nullableDate = date.Type == typeof(DateTimeOffset?) ? date : Expression.Convert(date, typeof(DateTimeOffset?));
				var hasValue     = Expression.Property(nullableDate, nameof(Nullable<>.HasValue));
				var value        = Expression.Property(nullableDate, nameof(Nullable<>.Value));
				var utcDate      = Expression.Property(value, nameof(DateTimeOffset.UtcDateTime));

				return Expression.Condition(hasValue, Expression.Convert(utcDate, typeof(DateTime?)), Expression.Default(typeof(DateTime?)));
			}

			private static Expression CreateBoundaryIndex(DateParts part, Expression date, bool forceAccessDecimal)
			{
				if (forceAccessDecimal)
					return CreateAccessBoundaryIndex(part, date);

				Expression Wrap(Expression value)
				{
					return value;
				}

				Expression Part(DateParts value) => Wrap(Expression.Call(_datePartLongDateTime, Expression.Constant(value), date));
				Expression Number(long value) => Expression.Constant((long?)value, typeof(long?));
				Expression Add(Expression left, Expression right) => Wrap(Expression.Add(left, right));
				Expression Sub(Expression left, Expression right) => Wrap(Expression.Subtract(left, right));
				Expression Mul(Expression left, long right) => Wrap(Expression.Multiply(left, Number(right)));
				Expression Div(Expression left, long right)
				{
					var quotient = Expression.Divide(
						Expression.Convert(left, typeof(decimal?)),
						Expression.Constant((decimal?)right, typeof(decimal?)));
					var floor = Expression.Call(_floorDecimal, quotient);
					return Expression.Convert(floor, typeof(long?));
				}

				var year        = Part(DateParts.Year);
				var yearIndex   = Sub(year, Number(1));
				var dayIndex    = Add(
					Add(
						Sub(Add(Mul(yearIndex, 365), Div(yearIndex, 4)), Div(yearIndex, 100)),
						Div(yearIndex, 400)),
					Sub(Part(DateParts.DayOfYear), Number(1)));
				var hourIndex   = Add(Mul(dayIndex, 24), Part(DateParts.Hour));
				var minuteIndex = Add(Mul(hourIndex, 60), Part(DateParts.Minute));
				var secondIndex = Add(Mul(minuteIndex, 60), Part(DateParts.Second));

				return part switch
				{
					DateParts.Year        => yearIndex,
					DateParts.Quarter     => Add(Mul(yearIndex, 4), Div(Sub(Part(DateParts.Month), Number(1)), 3)),
					DateParts.Month       => Add(Mul(yearIndex, 12), Sub(Part(DateParts.Month), Number(1))),
					DateParts.DayOfYear   => dayIndex,
					DateParts.Day         => dayIndex,
					DateParts.Week        => Div(Add(dayIndex, Number(1)), 7),
					DateParts.WeekDay     => dayIndex,
					DateParts.Hour        => hourIndex,
					DateParts.Minute      => minuteIndex,
					DateParts.Second      => secondIndex,
					DateParts.Millisecond => Add(Mul(secondIndex, 1_000), Part(DateParts.Millisecond)),
					DateParts.Microsecond => Add(Mul(secondIndex, 1_000_000), Part(DateParts.Microsecond)),
					DateParts.Tick        => Add(Mul(secondIndex, TimeSpan.TicksPerSecond), Part(DateParts.Tick)),
					_ => throw new InvalidOperationException($"Unexpected datepart: {part}"),
				};
			}

			private static Expression CreateAccessBoundaryIndex(DateParts part, Expression date)
			{
				Expression Wrap(Expression value) => Expression.Call(
					_accessDecimal,
					value.Type == typeof(decimal?) ? value : Expression.Convert(value, typeof(decimal?)));
				Expression Number(long value) => Wrap(Expression.Constant((decimal?)value, typeof(decimal?)));
				Expression Add(Expression left, Expression right) => Wrap(Expression.Add(left, right));
				Expression Sub(Expression left, Expression right) => Wrap(Expression.Subtract(left, right));
				Expression Mul(Expression left, long right) => Wrap(Expression.Multiply(left, Number(right)));
				Expression Div(Expression left, long right)
				{
					var quotient = Expression.Divide(
						Expression.Convert(left, typeof(decimal?)),
						Expression.Constant((decimal?)right, typeof(decimal?)));
					return Wrap(Expression.Call(_floorDecimal, quotient));
				}

				var milliseconds = Wrap(Expression.Call(_accessMilliseconds, date));
				var dayIndex      = Div(milliseconds, 86_400_000);
				var calendarDate  = Expression.Call(_accessDate, dayIndex);
				Expression Part(DateParts value) => Wrap(Expression.Call(_datePartLongDateTime, Expression.Constant(value), calendarDate));

				var yearIndex = Sub(Part(DateParts.Year), Number(1));

				return part switch
				{
					DateParts.Year        => yearIndex,
					DateParts.Quarter     => Add(Mul(yearIndex, 4), Div(Sub(Part(DateParts.Month), Number(1)), 3)),
					DateParts.Month       => Add(Mul(yearIndex, 12), Sub(Part(DateParts.Month), Number(1))),
					DateParts.DayOfYear   => dayIndex,
					DateParts.Day         => dayIndex,
					DateParts.Week        => Div(Add(dayIndex, Number(1)), 7),
					DateParts.WeekDay     => dayIndex,
					DateParts.Hour        => Div(milliseconds, 3_600_000),
					DateParts.Minute      => Div(milliseconds, 60_000),
					DateParts.Second      => Div(milliseconds, 1_000),
					DateParts.Millisecond => milliseconds,
					DateParts.Microsecond => Mul(milliseconds, 1000),
					DateParts.Tick        => Mul(milliseconds, TimeSpan.TicksPerMillisecond),
					_                     => throw new InvalidOperationException($"Unsupported date part: {part}"),
				};
			}
		}

		sealed class DateDiffBuilderFirebird : IExtensionCallBuilder
		{
			public DateDiffBuilderFirebird()
			{
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
			public DateDiffBuilderFirebird3Minus()
			{
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
			public DateDiffBuilderSapHana()
			{
			}

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
			public DateDiffBuilderDB2()
			{
			}

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
			public DateDiffBuilderSQLite()
			{
			}

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
			public DateDiffBuilderPostgreSql()
			{
			}

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
			public DateDiffBuilderAccess()
			{
			}

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
			public DateDiffBuilderOracle()
			{
			}

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
			public DateDiffBuilderClickHouse()
			{
			}

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
			public DateDiffBuilderYdb()
			{
			}

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

		/// <summary>
		/// Returns the difference between two date values in the requested units using the existing <c>DateDiff</c>
		/// contract and a 32-bit result.
		/// </summary>
		/// <param name="part">Date-part unit used to measure the difference.</param>
		/// <param name="startDate">Start date.</param>
		/// <param name="endDate">End date.</param>
		/// <returns>The difference, or <see langword="null"/> when either argument is <see langword="null"/>.</returns>
		/// <remarks>
		/// <see cref="DateParts.Tick"/> expresses the result in 100-nanosecond units. Nanosecond results are multiples of 100.
		/// Use <see cref="DateDiffLong(DateParts, DateTime?, DateTime?)"/> for the provider-independent boundary-counting contract
		/// and a 64-bit result.
		/// </remarks>
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
				DateParts.Microsecond => checked((int)((endDate - startDate).Value.Ticks / 10)),
				DateParts.Nanosecond  => checked((int)checked((endDate - startDate).Value.Ticks * 100)),
				DateParts.Tick        => checked((int)(endDate - startDate).Value.Ticks),
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
		/// <remarks>
		/// Counts calendar boundaries for year, quarter, month, week and day parts, and fixed-duration boundaries
		/// for hour through tick. Weeks start on Sunday. One tick is 100 nanoseconds; nanosecond results are therefore
		/// multiples of 100. An <see cref="OverflowException"/> is thrown when a nanosecond result doesn't fit into <see cref="long"/>.
		/// </remarks>
		[CLSCompliant(false)]
		[Extension(                  "DateDiff",      BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SqlServer,     "DateDiff_Big",  BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SqlServer2005, "DateDiff",      BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SqlServer2008, "DateDiff",      BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SqlServer2012, "DateDiff",      BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SqlServer2014, "DateDiff",      BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.MySql,         "TIMESTAMPDIFF", BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.DB2,           "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SapHana,       "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.Firebird25,    "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.Firebird3,     "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.Firebird,      "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.SQLite,        "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.Oracle,        "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.PostgreSQL,    "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.Access,        "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.ClickHouse,    "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.Ydb,           "",              BuilderType = typeof(DateDiffLongBuilder))]
		[Extension(PN.DuckDB,        "",              BuilderType = typeof(DateDiffLongBuilder))]
		public static long? DateDiffLong(DateParts part, DateTime? startDate, DateTime? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return DateDiffLongCore(part, startDate.Value, endDate.Value);
		}

		private static long DateDiffLongCore(DateParts part, DateTime startDate, DateTime endDate)
		{
			static long SundayWeekIndex(DateTime value)
			{
				var sunday = value.Date.Ticks / TimeSpan.TicksPerDay - (int)value.DayOfWeek;
				return sunday >= 0 ? sunday / 7 : (sunday - 6) / 7;
			}

			return part switch
			{
				DateParts.Year        => endDate.Year - startDate.Year,
				DateParts.Quarter     => endDate.Year * 4L + (endDate.Month - 1) / 3 - (startDate.Year * 4L + (startDate.Month - 1) / 3),
				DateParts.Month       => endDate.Year * 12L + endDate.Month - (startDate.Year * 12L + startDate.Month),
				DateParts.DayOfYear   => endDate.Ticks / TimeSpan.TicksPerDay - startDate.Ticks / TimeSpan.TicksPerDay,
				DateParts.Day         => endDate.Ticks / TimeSpan.TicksPerDay - startDate.Ticks / TimeSpan.TicksPerDay,
				DateParts.Week        => SundayWeekIndex(endDate) - SundayWeekIndex(startDate),
				DateParts.WeekDay     => endDate.Ticks / TimeSpan.TicksPerDay - startDate.Ticks / TimeSpan.TicksPerDay,
				DateParts.Hour        => endDate.Ticks / TimeSpan.TicksPerHour - startDate.Ticks / TimeSpan.TicksPerHour,
				DateParts.Minute      => endDate.Ticks / TimeSpan.TicksPerMinute - startDate.Ticks / TimeSpan.TicksPerMinute,
				DateParts.Second      => endDate.Ticks / TimeSpan.TicksPerSecond - startDate.Ticks / TimeSpan.TicksPerSecond,
				DateParts.Millisecond => endDate.Ticks / TimeSpan.TicksPerMillisecond - startDate.Ticks / TimeSpan.TicksPerMillisecond,
				DateParts.Microsecond => endDate.Ticks / 10 - startDate.Ticks / 10,
				DateParts.Nanosecond  => checked((endDate.Ticks - startDate.Ticks) * 100),
				DateParts.Tick        => endDate.Ticks - startDate.Ticks,
				_                     => throw new InvalidOperationException(),
			};
		}

		#endregion
	}
}
