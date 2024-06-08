#if NET6_0_OR_GREATER
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
		[Extension(PN.DB2,        "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))]
		[Extension(PN.Informix,   "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))]
		[Extension(PN.MySql,      "Extract({part} from {date})",                     ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Extension(PN.PostgreSQL, "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Extension(PN.Firebird,   "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)",        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Extension(PN.Access,     "DatePart('{part}', {date})",                      ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Extension(PN.SapHana,    "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Extension(PN.Oracle,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		[Extension(PN.ClickHouse, "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderClickHouse))]
		public static int? DatePart([SqlQueryDependent] DateParts part, [ExprParameter] DateOnly? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				DateParts.Year      => date.Value.Year,
				DateParts.Quarter   => (date.Value.Month - 1) / 3 + 1,
				DateParts.Month     => date.Value.Month,
				DateParts.DayOfYear => date.Value.DayOfYear,
				DateParts.Day       => date.Value.Day,
				DateParts.Week      => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.ToDateTime(TimeOnly.MinValue), CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				DateParts.WeekDay   => ((int)date.Value.DayOfWeek + 1 + DateFirst + 6) % 7 + 1,
				_                   => throw new InvalidOperationException(),
			};
		}
		#endregion

		#region DateAdd

		[Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilder))]
		[Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderPostgreSQL))]
		[Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateOnlyAddBuilderSQLite))]
		[Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		[Extension(PN.ClickHouse, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderClickHouse))]
		public static DateOnly? DateAdd([SqlQueryDependent] DateParts part, double? number, DateOnly? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				DateParts.Year      => date.Value.AddYears((int)number),
				DateParts.Quarter   => date.Value.AddMonths((int)number * 3),
				DateParts.Month     => date.Value.AddMonths((int)number),
				DateParts.DayOfYear => date.Value.AddDays((int)number.Value),
				DateParts.Day       => date.Value.AddDays((int)number.Value),
				DateParts.Week      => date.Value.AddDays((int)number.Value * 7),
				DateParts.WeekDay   => date.Value.AddDays((int)number.Value),
				_                   => throw new InvalidOperationException(),
			};
		}

		sealed class DateOnlyAddBuilderSQLite : IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr = "strftime('%Y-%m-%d', {0},";
				switch (part)
				{
					case DateParts.Year:      expStr += "{1} || ' Year')"; break;
					case DateParts.Quarter:   expStr += "({1}*3) || ' Month')"; break;
					case DateParts.Month:     expStr += "{1} || ' Month')"; break;
					case DateParts.DayOfYear:
					case DateParts.WeekDay:
					case DateParts.Day:       expStr += "{1} || ' Day')"; break;
					case DateParts.Week:      expStr += "({1}*7) || ' Day')"; break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Concatenate, date, number);
			}
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
		public static int? DateDiff(DateParts part, DateOnly? startDate, DateOnly? endDate)
		{
			if (startDate == null || endDate == null)
				return null;

			return part switch
			{
				DateParts.Day => endDate.Value.DayNumber - startDate.Value.DayNumber,
				_             => throw new InvalidOperationException(),
			};
		}
		#endregion
	}
}
#endif
