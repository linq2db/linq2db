using System;
using System.Globalization;

using LinqToDB.Expressions;

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
				DateParts.DayOfYear     => date.Value.AddDays(number.Value),
				DateParts.Day           => date.Value.AddDays(number.Value),
				DateParts.Week          => date.Value.AddDays(number.Value * 7),
				DateParts.WeekDay       => date.Value.AddDays(number.Value),
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
		[Extension(PN.SQLite,     "",              BuilderType = typeof(DateDiffBuilderSQLite))]
		[Extension(PN.PostgreSQL, "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Extension(PN.Access,     "",              BuilderType = typeof(DateDiffBuilderAccess))]
		[Extension(PN.ClickHouse, "",              BuilderType = typeof(DateDiffBuilderClickHouse))]
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
	}
}
