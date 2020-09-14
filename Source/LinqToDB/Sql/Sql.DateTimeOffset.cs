﻿using System;
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
		[Sql.Extension(PN.DB2,        "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Sql.Extension(PN.Informix,   "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderInformix))] 
		[Sql.Extension(PN.MySql,      "Extract({part} from {date})",                     ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Sql.Extension(PN.PostgreSQL, "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Sql.Extension(PN.Firebird,   "Cast(Floor(Extract({part} from {date})) as int)", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Sql.Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)",        ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Sql.Extension(PN.Access,     "DatePart('{part}', {date})",                      ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Sql.Extension(PN.Oracle,     "",                                                ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DatePartBuilderOracle))]
		public static int? DatePart([SqlQueryDependent] Sql.DateParts part, [ExprParameter] DateTimeOffset? date)
		{
			if (date == null)
				return null;

			return part switch
			{
				Sql.DateParts.Year          => date.Value.Year,
				Sql.DateParts.Quarter       => (date.Value.Month - 1) / 3 + 1,
				Sql.DateParts.Month         => date.Value.Month,
				Sql.DateParts.DayOfYear     => date.Value.DayOfYear,
				Sql.DateParts.Day           => date.Value.Day,
				Sql.DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.LocalDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				Sql.DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + Sql.DateFirst + 6) % 7 + 1,
				Sql.DateParts.Hour          => date.Value.Hour,
				Sql.DateParts.Minute        => date.Value.Minute,
				Sql.DateParts.Second        => date.Value.Second,
				Sql.DateParts.Millisecond   => date.Value.Millisecond,
				_                           => throw new InvalidOperationException(),
			};
		}
		#endregion

		#region DateAdd

		class DateAddOffsetBuilder : Sql.IExtensionCallBuilder
		{
			public void Build(ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var partStr = DatePartBuilder.DatePartToStr(part);
				var date    = builder.GetExpression("date");
				var number  = builder.GetExpression("number");
				builder.ResultExpression = new SqlFunction(typeof(DateTimeOffset?), builder.Expression,
					new SqlExpression(partStr, Precedence.Primary), number, date);
			}
		}

		[Sql.Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddOffsetBuilder))]
		public static DateTimeOffset? DateAdd([SqlQueryDependent] Sql.DateParts part, double? number, DateTimeOffset? date)
		{
			if (number == null || date == null)
				return null;

			return part switch
			{
				Sql.DateParts.Year          => date.Value.AddYears((int)number),
				Sql.DateParts.Quarter       => date.Value.AddMonths((int)number * 3),
				Sql.DateParts.Month         => date.Value.AddMonths((int)number),
				Sql.DateParts.DayOfYear     => date.Value.AddDays(number.Value),
				Sql.DateParts.Day           => date.Value.AddDays(number.Value),
				Sql.DateParts.Week          => date.Value.AddDays(number.Value * 7),
				Sql.DateParts.WeekDay       => date.Value.AddDays(number.Value),
				Sql.DateParts.Hour          => date.Value.AddHours(number.Value),
				Sql.DateParts.Minute        => date.Value.AddMinutes(number.Value),
				Sql.DateParts.Second        => date.Value.AddSeconds(number.Value),
				Sql.DateParts.Millisecond   => date.Value.AddMilliseconds(number.Value),
				_                           => throw new InvalidOperationException(),
			};
		}

		[CLSCompliant(false)]
		[Sql.Extension(                "DateDiff",      BuilderType = typeof(DateDiffBuilder))]
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
