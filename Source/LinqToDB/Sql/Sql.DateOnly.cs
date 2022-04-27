﻿#if NET6_0_OR_GREATER
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
		[Sql.Extension(               "DatePart",                                        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilder))]
		[Sql.Extension(PN.DB2,        "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))]
		[Sql.Extension(PN.Informix,   "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))]
		[Sql.Extension(PN.MySql,      "Extract({part} from {date})",                     ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Sql.Extension(PN.PostgreSQL, "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Sql.Extension(PN.Firebird,   "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Sql.Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)",        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Sql.Extension(PN.Access,     "DatePart('{part}', {date})",                      ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Sql.Extension(PN.Oracle,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		public static int? DatePart([SqlQueryDependent] Sql.DateParts part, [ExprParameter] DateOnly? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				Sql.DateParts.Year      => date.Value.Year,
				Sql.DateParts.Quarter   => (date.Value.Month - 1) / 3 + 1,
				Sql.DateParts.Month     => date.Value.Month,
				Sql.DateParts.DayOfYear => date.Value.DayOfYear,
				Sql.DateParts.Day       => date.Value.Day,
				Sql.DateParts.Week      => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.ToDateTime(TimeOnly.MinValue), CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				Sql.DateParts.WeekDay   => ((int)date.Value.DayOfWeek + 1 + Sql.DateFirst + 6) % 7 + 1,
				_                       => throw new InvalidOperationException(),
			};
		}
		#endregion

		#region DateAdd

		[Sql.Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilder))]
		[Sql.Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderPostgreSQL))]
		[Sql.Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Sql.Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Sql.Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Sql.Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Sql.Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateOnlyAddBuilderSQLite))]
		[Sql.Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Sql.Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		public static DateOnly? DateAdd([SqlQueryDependent] Sql.DateParts part, double? number, DateOnly? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				Sql.DateParts.Year      => date.Value.AddYears((int)number),
				Sql.DateParts.Quarter   => date.Value.AddMonths((int)number * 3),
				Sql.DateParts.Month     => date.Value.AddMonths((int)number),
				Sql.DateParts.DayOfYear => date.Value.AddDays((int)number.Value),
				Sql.DateParts.Day       => date.Value.AddDays((int)number.Value),
				Sql.DateParts.Week      => date.Value.AddDays((int)number.Value * 7),
				Sql.DateParts.WeekDay   => date.Value.AddDays((int)number.Value),
				_                       => throw new InvalidOperationException(),
			};
		}

		class DateOnlyAddBuilderSQLite : Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part   = builder.GetValue<Sql.DateParts>("part");
				var date   = builder.GetExpression("date");
				var number = builder.GetExpression("number", true);

				string expStr = "strftime('%Y-%m-%d', {0},";
				switch (part)
				{
					case Sql.DateParts.Year:      expStr += "{1} || ' Year')"; break;
					case Sql.DateParts.Quarter:   expStr += "({1}*3) || ' Month')"; break;
					case Sql.DateParts.Month:     expStr += "{1} || ' Month')"; break;
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
					case Sql.DateParts.Day:       expStr += "{1} || ' Day')"; break;
					case Sql.DateParts.Week:      expStr += "({1}*7) || ' Day')"; break;
					default:
						throw new InvalidOperationException($"Unexpected datepart: {part}");
				}

				builder.ResultExpression = new SqlExpression(typeof(DateTime?), expStr, Precedence.Concatenate, date, number);
			}
		}
		#endregion

		#region DateDiff
		[CLSCompliant(false)]
		[Sql.Extension(               "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
		[Sql.Extension(PN.MySql,      "TIMESTAMPDIFF", BuilderType = typeof(DateDiffBuilder))]
		[Sql.Extension(PN.DB2,        "",              BuilderType = typeof(DateDiffBuilderDB2))]
		[Sql.Extension(PN.SapHana,    "",              BuilderType = typeof(DateDiffBuilderSapHana))]
		[Sql.Extension(PN.SQLite,     "",              BuilderType = typeof(DateDiffBuilderSQLite))]
		[Sql.Extension(PN.PostgreSQL, "",              BuilderType = typeof(DateDiffBuilderPostgreSql))]
		[Sql.Extension(PN.Access,     "",              BuilderType = typeof(DateDiffBuilderAccess))]
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
