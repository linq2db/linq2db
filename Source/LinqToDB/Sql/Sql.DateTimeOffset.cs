using System;
using System.Globalization;

using LinqToDB.Mapping;

using PN = LinqToDB.ProviderName;

namespace LinqToDB
{
	public partial class Sql
	{
		#region DatePart

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
				_                       => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateAdd

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
				_                       => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region DateDiff

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
				_                     => throw new InvalidOperationException(),
			};
		}

		#endregion

		#region AtTimeZone

		/// <summary>
		/// Re-expresses an instant with the offset in effect in <paramref name="timeZone"/>, as SQL's
		/// <c>AT TIME ZONE</c> does. The instant is unchanged; only the offset it carries differs.
		/// </summary>
		/// <param name="value">Instant to re-express. <see langword="null"/> propagates.</param>
		/// <param name="timeZone">
		/// A time zone identifier, interpreted by the <b>database</b> rather than by .NET, so the accepted set is the
		/// server's: SQL Server takes Windows identifiers (<c>"Central European Standard Time"</c>), while PostgreSQL,
		/// Oracle, DuckDB and MySQL take IANA identifiers (<c>"Europe/Prague"</c>). There is no spelling that every
		/// provider accepts - SQL Server rejects a bare UTC offset such as <c>"+02:00"</c>.
		/// </param>
		/// <returns>The same instant, carrying <paramref name="timeZone"/>'s offset.</returns>
		/// <remarks>
		/// Materialising the result needs a column type that carries an offset, so selecting it directly translates on
		/// SQL Server 2016+, Oracle and Firebird 4+ and is refused by name elsewhere. Reading a component or
		/// <see cref="DateTimeOffset.DateTime"/> from it works on every provider that can express a zone conversion at
		/// all, because no offset has to survive into the result.
		/// </remarks>
		public static DateTimeOffset? AtTimeZone(DateTimeOffset? value, string timeZone)
		{
			if (value == null)
				return null;

			return TimeZoneInfo.ConvertTime(value.Value, TimeZoneInfo.FindSystemTimeZoneById(timeZone));
		}

		/// <summary>
		/// Reads a wall-clock value as being in <paramref name="timeZone"/> and returns the instant it denotes,
		/// carrying that zone's offset.
		/// </summary>
		/// <param name="value">Wall-clock reading to interpret. <see langword="null"/> propagates.</param>
		/// <param name="timeZone">See <see cref="AtTimeZone(DateTimeOffset?, string)"/> for how the identifier is interpreted.</param>
		/// <returns>The instant <paramref name="value"/> denotes in <paramref name="timeZone"/>.</returns>
		/// <remarks>
		/// <see cref="DateTime.Kind"/> is deliberately ignored - the value is read as a wall clock whatever it says,
		/// matching the SQL construct, so the zone comes from the argument and never from the machine the expression
		/// happens to run on.
		/// <para>
		/// A wall clock inside a daylight-saving transition is resolved differently by .NET and by the server: .NET
		/// treats an ambiguous or non-existent local time as standard time, while SQL Server uses the offset after a
		/// spring-forward gap and the one before an autumn overlap. The two therefore disagree on the instant for
		/// those readings only.
		/// </para>
		/// </remarks>
		public static DateTimeOffset? AtTimeZone(DateTime? value, string timeZone)
		{
			if (value == null)
				return null;

			var wallClock = DateTime.SpecifyKind(value.Value, DateTimeKind.Unspecified);

			return new DateTimeOffset(wallClock, TimeZoneInfo.FindSystemTimeZoneById(timeZone).GetUtcOffset(wallClock));
		}

		#endregion
	}
}
