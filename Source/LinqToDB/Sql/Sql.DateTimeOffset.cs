using System;
using System.Globalization;

using LinqToDB.Mapping;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public partial class Sql
	{
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
		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateTimeOffset? date)
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
				DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.LocalDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
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
		public static long? DatePartLong([SqlQueryDependent] DateParts part, [ExprParameter] DateTimeOffset? date)
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
				DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.LocalDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1,
				DateParts.Hour          => date.Value.Hour,
				DateParts.Minute        => date.Value.Minute,
				DateParts.Second        => date.Value.Second,
				DateParts.Millisecond   => date.Value.Millisecond,
				DateParts.Microsecond   => date.Value.Ticks % TimeSpan.TicksPerSecond / 10,
				DateParts.Nanosecond    => date.Value.Ticks % TimeSpan.TicksPerSecond * 100,
				DateParts.Tick          => date.Value.Ticks % TimeSpan.TicksPerSecond,
				_                       => throw new InvalidOperationException(),
			};
		}

		#endregion

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
		public static DateTimeOffset? DateAdd([SqlQueryDependent] DateParts part, double? number, DateTimeOffset? date)
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
		/// Use <see cref="DateDiffLong(DateParts, DateTimeOffset?, DateTimeOffset?)"/> for the provider-independent UTC-normalized
		/// boundary-counting contract and a 64-bit result.
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
		[Extension(PN.PostgreSQL, "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Extension(PN.Access,     "",              BuilderType = typeof(DateDiffBuilderAccess))]
		[Extension(PN.ClickHouse, "",              BuilderType = typeof(DateDiffBuilderClickHouse))]
		[Extension(PN.Ydb,        "",              BuilderType = typeof(DateDiffBuilderYdb))]
		[Extension(PN.DuckDB,     "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		public static int? DateDiff(DateParts part, DateTimeOffset? startDate, DateTimeOffset? endDate)
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
		/// Both values are normalized to UTC before boundaries are counted. Counts calendar boundaries for year,
		/// quarter, month, week and day parts, and fixed-duration boundaries for hour through tick. Weeks start on Sunday.
		/// One tick is 100 nanoseconds; nanosecond results are therefore multiples of 100. An <see cref="OverflowException"/>
		/// is thrown when a nanosecond result doesn't fit into <see cref="long"/>.
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
		public static long? DateDiffLong(DateParts part, DateTimeOffset? startDate, DateTimeOffset? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return DateDiffLongCore(part, startDate.Value.UtcDateTime, endDate.Value.UtcDateTime);
		}

		#endregion
	}
}
