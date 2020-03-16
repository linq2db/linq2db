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

			switch (part)
			{
				case Sql.DateParts.Year        : return date.Value.Year;
				case Sql.DateParts.Quarter     : return (date.Value.Month - 1) / 3 + 1;
				case Sql.DateParts.Month       : return date.Value.Month;
				case Sql.DateParts.DayOfYear   : return date.Value.DayOfYear;
				case Sql.DateParts.Day         : return date.Value.Day;
				case Sql.DateParts.Week        : return CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value.LocalDateTime, CalendarWeekRule.FirstDay, DayOfWeek.Sunday);
				case Sql.DateParts.WeekDay     : return ((int)date.Value.DayOfWeek + 1 + Sql.DateFirst + 6) % 7 + 1;
				case Sql.DateParts.Hour        : return date.Value.Hour;
				case Sql.DateParts.Minute      : return date.Value.Minute;
				case Sql.DateParts.Second      : return date.Value.Second;
				case Sql.DateParts.Millisecond : return date.Value.Millisecond;
			}

			throw new InvalidOperationException();
		}
		#endregion

		#region DateAdd
		[Sql.Extension("DateAdd"        , ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilder))]
		[Sql.Extension(PN.Oracle,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderOracle))]
		[Sql.Extension(PN.DB2,        "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderDB2))]
		[Sql.Extension(PN.Informix,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderInformix))]
		[Sql.Extension(PN.PostgreSQL, "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderPostgreSQL))]
		[Sql.Extension(PN.MySql,      "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderMySql))]
		[Sql.Extension(PN.SQLite,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSQLite))]
		[Sql.Extension(PN.Access,     "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderSapHana))]
		[Sql.Extension(PN.Firebird,   "", ServerSideOnly = false, PreferServerSide = false, BuilderType = typeof(DateAddBuilderFirebird))]
		public static DateTimeOffset? DateAdd([SqlQueryDependent] Sql.DateParts part, double? number, DateTimeOffset? date)
		{
			if (number == null || date == null)
				return null;

			switch (part)
			{
				case Sql.DateParts.Year        : return date.Value.AddYears       ((int)number);
				case Sql.DateParts.Quarter     : return date.Value.AddMonths      ((int)number * 3);
				case Sql.DateParts.Month       : return date.Value.AddMonths      ((int)number);
				case Sql.DateParts.DayOfYear   : return date.Value.AddDays        (number.Value);
				case Sql.DateParts.Day         : return date.Value.AddDays        (number.Value);
				case Sql.DateParts.Week        : return date.Value.AddDays        (number.Value * 7);
				case Sql.DateParts.WeekDay     : return date.Value.AddDays        (number.Value);
				case Sql.DateParts.Hour        : return date.Value.AddHours       (number.Value);
				case Sql.DateParts.Minute      : return date.Value.AddMinutes     (number.Value);
				case Sql.DateParts.Second      : return date.Value.AddSeconds     (number.Value);
				case Sql.DateParts.Millisecond : return date.Value.AddMilliseconds(number.Value);
			}

			throw new InvalidOperationException();
		}
		#endregion
	}
}
