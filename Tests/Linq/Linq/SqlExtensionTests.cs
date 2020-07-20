﻿using System;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Linq
{
	using PN = ProviderName;

	public static class TestedExtensions
	{
		class DatePartBuilder: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "year",
					Sql.DateParts.Quarter       => "quarter",
					Sql.DateParts.Month         => "month",
					Sql.DateParts.DayOfYear     => "dayofyear",
					Sql.DateParts.Day           => "day",
					Sql.DateParts.Week          => "week",
					Sql.DateParts.WeekDay       => "weekday",
					Sql.DateParts.Hour          => "hour",
					Sql.DateParts.Minute        => "minute",
					Sql.DateParts.Second        => "second",
					Sql.DateParts.Millisecond   => "millisecond",
					_ => throw new ArgumentOutOfRangeException(),
				};
				builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderMySql: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string? partStr = null;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";        break;
					case Sql.DateParts.Quarter     : partStr = "quarter";     break;
					case Sql.DateParts.Month       : partStr = "month";       break;
					case Sql.DateParts.DayOfYear   :
						builder.Expression = "DayOfYear({date})";
						break;
					case Sql.DateParts.Day         : partStr = "day";         break;
					case Sql.DateParts.Week        : partStr = "week";        break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "WeekDay(Date_Add({date}, interval 1 day))";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						break;
					case Sql.DateParts.Hour        : partStr = "hour";        break;
					case Sql.DateParts.Minute      : partStr = "minute";      break;
					case Sql.DateParts.Second      : partStr = "second";      break;
					case Sql.DateParts.Millisecond : partStr = "millisecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderPostgre: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string? partStr = null;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";    break;
					case Sql.DateParts.Quarter     : partStr = "quarter"; break;
					case Sql.DateParts.Month       : partStr = "month";   break;
					case Sql.DateParts.DayOfYear   : partStr = "doy";     break;
					case Sql.DateParts.Day         : partStr = "day";     break;
					case Sql.DateParts.Week        : partStr = "week";    break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "Extract(dow from {date})";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						break;
					case Sql.DateParts.Hour        : partStr = "hour";    break;
					case Sql.DateParts.Minute      : partStr = "minute";  break;
					case Sql.DateParts.Second      : partStr = "second";  break;
					case Sql.DateParts.Millisecond :
						builder.Expression = "Cast(To_Char({date}, 'MS') as int)";
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(part), part, null);
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderSqLite: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string? partStr = null;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "Y"; break;
					case Sql.DateParts.Quarter     :
						builder.Expression = "Cast(strFTime('%m', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						break;
					case Sql.DateParts.Month       : partStr = "m"; break;
					case Sql.DateParts.DayOfYear   : partStr = "j"; break;
					case Sql.DateParts.Day         : partStr = "d"; break;
					case Sql.DateParts.Week        : partStr = "W"; break;
					case Sql.DateParts.WeekDay     :
						builder.Expression = "Cast(strFTime('%w', {date}) as int)";
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						break;
					case Sql.DateParts.Hour        : partStr = "H"; break;
					case Sql.DateParts.Minute      : partStr = "M"; break;
					case Sql.DateParts.Second      : partStr = "S"; break;
					case Sql.DateParts.Millisecond :
						builder.Expression = "Cast(strFTime('%f', {date}) * 1000 as int) % 1000";
						builder.Extension.Precedence = Precedence.Multiplicative;
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(part), part, null);
				}

				if (partStr != null)
					builder.AddExpression("part", partStr);
			}
		}

		class DatePartBuilderAccess: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part    = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year      => "yyyy",
					Sql.DateParts.Quarter   => "q",
					Sql.DateParts.Month     => "m",
					Sql.DateParts.DayOfYear => "y",
					Sql.DateParts.Day       => "d",
					Sql.DateParts.Week      => "ww",
					Sql.DateParts.WeekDay   => "w",
					Sql.DateParts.Hour      => "h",
					Sql.DateParts.Minute    => "n",
					Sql.DateParts.Second    => "s",
					_ => throw new ArgumentOutOfRangeException(),
				};
				builder.AddExpression("part", partStr);
			}
		}


		class DatePartBuilderSapHana: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year      => "Year({date})",
					Sql.DateParts.Quarter   => "Floor((Month({date})-1) / 3) + 1",
					Sql.DateParts.Month     => "Month({date})",
					Sql.DateParts.DayOfYear => "DayOfYear({date})",
					Sql.DateParts.Day       => "DayOfMonth({date})",
					Sql.DateParts.Week      => "Week({date})",
					Sql.DateParts.WeekDay   => "MOD(Weekday({date}) + 1, 7) + 1",
					Sql.DateParts.Hour      => "Hour({date})",
					Sql.DateParts.Minute    => "Minute({date})",
					Sql.DateParts.Second    => "Second({date})",
					_ => throw new ArgumentOutOfRangeException(),
				};
				builder.Expression = partStr;
			}
		}

		class DatePartBuilderInformix: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "Year({date})",
					Sql.DateParts.Quarter       => "((Month({date}) - 1) / 3 + 1)",
					Sql.DateParts.Month         => "Month({date})",
					Sql.DateParts.DayOfYear     => "(Mdy(Month({date}), Day({date}), Year({date})) - Mdy(1, 1, Year({date})) + 1)",
					Sql.DateParts.Day           => "Day({date})",
					Sql.DateParts.Week          => "((Extend({date}, year to day) - (Mdy(12, 31 - WeekDay(Mdy(1, 1, year({date}))), Year({date}) - 1) + Interval(1) day to day)) / 7 + Interval(1) day to day)::char(10)::int",
					Sql.DateParts.WeekDay       => "(weekDay({date}) + 1)",
					Sql.DateParts.Hour          => "({date}::datetime Hour to Hour)::char(3)::int",
					Sql.DateParts.Minute        => "({date}::datetime Minute to Minute)::char(3)::int",
					Sql.DateParts.Second        => "({date}::datetime Second to Second)::char(3)::int",
					Sql.DateParts.Millisecond   => "Millisecond({date})",
					_ => throw new ArgumentOutOfRangeException(),
				};
				builder.Expression = partStr;
			}
		}

		class DatePartBuilderOracle: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "To_Number(To_Char({date}, 'YYYY'))",
					Sql.DateParts.Quarter       => "To_Number(To_Char({date}, 'Q'))",
					Sql.DateParts.Month         => "To_Number(To_Char({date}, 'MM'))",
					Sql.DateParts.DayOfYear     => "To_Number(To_Char({date}, 'DDD'))",
					Sql.DateParts.Day           => "To_Number(To_Char({date}, 'DD'))",
					Sql.DateParts.Week          => "To_Number(To_Char({date}, 'WW'))",
					Sql.DateParts.WeekDay       => "Mod(1 + Trunc({date}) - Trunc({date}, 'IW'), 7) + 1",
					Sql.DateParts.Hour          => "To_Number(To_Char({date}, 'HH24'))",
					Sql.DateParts.Minute        => "To_Number(To_Char({date}, 'MI'))",
					Sql.DateParts.Second        => "To_Number(To_Char({date}, 'SS'))",
					Sql.DateParts.Millisecond   => "To_Number(To_Char({date}, 'FF'))",
					_ => throw new ArgumentOutOfRangeException(),
				};
				builder.Expression = partStr;
			}
		}

		class DatePartBuilderDB2: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				var part = builder.GetValue<Sql.DateParts>("part");
				var partStr = part switch
				{
					Sql.DateParts.Year          => "To_Number(To_Char({date}, 'YYYY'))",
					Sql.DateParts.Quarter       => "To_Number(To_Char({date}, 'Q'))",
					Sql.DateParts.Month         => "To_Number(To_Char({date}, 'MM'))",
					Sql.DateParts.DayOfYear     => "To_Number(To_Char({date}, 'DDD'))",
					Sql.DateParts.Day           => "To_Number(To_Char({date}, 'DD'))",
					Sql.DateParts.Week          => "To_Number(To_Char({date}, 'WW'))",
					Sql.DateParts.WeekDay       => "DayOfWeek({date})",
					Sql.DateParts.Hour          => "To_Number(To_Char({date}, 'HH24'))",
					Sql.DateParts.Minute        => "To_Number(To_Char({date}, 'MI'))",
					Sql.DateParts.Second        => "To_Number(To_Char({date}, 'SS'))",
					Sql.DateParts.Millisecond   => "To_Number(To_Char({date}, 'FF')) / 1000",
					_ => throw new ArgumentOutOfRangeException(),
				};
				builder.Expression = partStr;
			}
		}

		class DatePartBuilderFirebird: Sql.IExtensionCallBuilder
		{
			public void Build(Sql.ISqExtensionBuilder builder)
			{
				string partStr;
				var part = builder.GetValue<Sql.DateParts>("part");
				switch (part)
				{
					case Sql.DateParts.Year        : partStr = "year";        break;
					case Sql.DateParts.Quarter     :
						builder.Expression = "Extract(Month from {date})";
						builder.ResultExpression = builder.Inc(builder.Div(builder.Dec(builder.ConvertToSqlExpression(Precedence.Primary)), 3));
						return;
					case Sql.DateParts.Month       : partStr = "month";       break;
					case Sql.DateParts.DayOfYear   : partStr = "yearday";     break;
					case Sql.DateParts.Day         : partStr = "day";         break;
					case Sql.DateParts.Week        : partStr = "week";        break;
					case Sql.DateParts.WeekDay     : partStr = "weekday";     break;
					case Sql.DateParts.Hour        : partStr = "hour";        break;
					case Sql.DateParts.Minute      : partStr = "minute";      break;
					case Sql.DateParts.Second      : partStr = "second";      break;
					case Sql.DateParts.Millisecond : partStr = "millisecond"; break;
					default:
						throw new ArgumentOutOfRangeException();
				}

				builder.AddExpression("part", partStr);

				switch (part)
				{
					case Sql.DateParts.DayOfYear:
					case Sql.DateParts.WeekDay:
						builder.ResultExpression = builder.Inc(builder.ConvertToSqlExpression(Precedence.Primary));
						break;
				}
			}
		}


		[Sql.Extension(               "DatePart({part}, {date})",                 ServerSideOnly = false, BuilderType = typeof(DatePartBuilder))]
		[Sql.Extension(PN.DB2,        "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderDB2))] // TODO: Not checked
		[Sql.Extension(PN.Informix,   "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderInformix))] // TODO: Not checked
		[Sql.Extension(PN.MySql,      "Extract({part} from {date})",              ServerSideOnly = false, BuilderType = typeof(DatePartBuilderMySql))]
		[Sql.Extension(PN.PostgreSQL, "Extract({part} from {date})",              ServerSideOnly = false, BuilderType = typeof(DatePartBuilderPostgre))]
		[Sql.Extension(PN.Firebird,   "Extract({part} from {date})",              ServerSideOnly = false, BuilderType = typeof(DatePartBuilderFirebird))]
		[Sql.Extension(PN.SQLite,     "Cast(StrFTime('%{part}', {date}) as int)", ServerSideOnly = false, BuilderType = typeof(DatePartBuilderSqLite))]
		[Sql.Extension(PN.Access,     "DatePart('{part}', {date})",               ServerSideOnly = false, BuilderType = typeof(DatePartBuilderAccess))]
		[Sql.Extension(PN.SapHana,    "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderSapHana))]
		[Sql.Extension(PN.Oracle,     "",                                         ServerSideOnly = false, BuilderType = typeof(DatePartBuilderOracle))]
		public static int? DatePart(this Sql.ISqlExtension? ext, Sql.DateParts part, [ExprParameter] DateTime? date)
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
				Sql.DateParts.Week          => CultureInfo.CurrentCulture.Calendar.GetWeekOfYear(date.Value, CalendarWeekRule.FirstDay, DayOfWeek.Sunday),
				Sql.DateParts.WeekDay       => ((int)date.Value.DayOfWeek + 1 + Sql.DateFirst + 6) % 7 + 1,
				Sql.DateParts.Hour          => date.Value.Hour,
				Sql.DateParts.Minute        => date.Value.Minute,
				Sql.DateParts.Second        => date.Value.Second,
				Sql.DateParts.Millisecond   => date.Value.Millisecond,
				_ => throw new InvalidOperationException(),
			};
		}
	}

	public class SqlExtensionTests : TestBase
	{
		#region DatePart

		[Test]
		public void DatePartYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Year, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Year, t.DateTimeValue)));
		}

		[Test]
		public void DatePartQuarter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Quarter, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Quarter, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMonth([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Month, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Month, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Day, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Day, t.DateTimeValue)));
		}

		[Test]
		public void DatePartWeek([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Week, t.DateTimeValue))).ToList();
		}

		[Test]
		public void DatePartWeekDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue)));
		}

		[Test]
		public void DatePartHour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Hour, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Hour, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMinute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Minute, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Minute, t.DateTimeValue)));
		}

		[Test]
		public void DatePartSecond([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.Ext.DatePart(Sql.DateParts.Second, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Second, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMillisecond([DataSources(TestProvName.AllInformix, TestProvName.AllMySql, TestProvName.AllAccess, TestProvName.AllSapHana)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.Ext.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.Ext.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue)));
		}


		#endregion
	}
}
