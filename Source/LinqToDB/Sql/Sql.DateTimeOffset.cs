#pragma warning disable CS8604 // TODO:WAITFIX
using System;
using System.Globalization;

namespace LinqToDB
{
	using SqlQuery;
	using Expressions;

	using PN = ProviderName;

	public partial class Sql
	{
		#region DatePart
		[Extension(               "DatePart",                                        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilder))]
		[Extension(PN.DB2,        "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Extension(PN.Informix,   "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))]
		[Extension(PN.MySql,      "Extract({part} from {date})",                     ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Extension(PN.PostgreSQL, "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Extension(PN.Firebird,   "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)",        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Extension(PN.Access,     "DatePart('{part}', {date})",                      ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Extension(PN.SapHana,    "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Extension(PN.Oracle,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		[Extension(PN.ClickHouse, "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderClickHouse))]
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

		sealed class DateOffsetAddBuilder : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<DateParts>("part");
				var partStr = DatePartBuilder.DatePartToStr(part);
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number", true);


				builder.ResultExpression = new SqlFunction(typeof(DateTimeOffset?), builder.Expression,
					new SqlExpression(partStr, Precedence.Primary), number, date);
			}
		}

		sealed class DateOffsetAddBuilderPostgreSQL : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr;
				switch (part)
				{
					case DateParts.Year        : expStr = "{0} * Interval '1 Year'";         break;
					case DateParts.Quarter     : expStr = "{0} * Interval '1 Month' * 3";    break;
					case DateParts.Month       : expStr = "{0} * Interval '1 Month'";        break;
					case DateParts.DayOfYear   :
					case DateParts.WeekDay     :
					case DateParts.Day         : expStr = "{0} * Interval '1 Day'";          break;
					case DateParts.Week        : expStr = "{0} * Interval '1 Day' * 7";      break;
					case DateParts.Hour        : expStr = "{0} * Interval '1 Hour'";         break;
					case DateParts.Minute      : expStr = "{0} * Interval '1 Minute'";       break;
					case DateParts.Second      : expStr = "{0} * Interval '1 Second'";       break;
					case DateParts.Millisecond : expStr = "{0} * Interval '1 Millisecond'";  break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = builder.Add(
					date,
					new SqlExpression(typeof(TimeSpan?), expStr, Precedence.Multiplicative, number),
					typeof(DateTimeOffset?));
			}
		}

		[Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateOffsetAddBuilder))]
		[Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateOffsetAddBuilderPostgreSQL))]
		[Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSQLite))]
		[Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		[Extension(PN.ClickHouse, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderClickHouse))]
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
