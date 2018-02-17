using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using Model;

	[TestFixture]
	public class DateTimeFunctionsTests : TestBase
	{
		[Test, DataContextSource]
		public void GetDate(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.AsSql(Sql.GetDate()) };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test, DataContextSource]
		public void CurrentTimestamp(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.CurrentTimestamp };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test, DataContextSource]
		public void CurrentTimestampUpdate(string context)
		{
			using (var db = GetDataContext(context))
			{
				(
					from p in db.Types where p.ID == 100000 select p
				)
				.Update(t => new LinqDataTypes
				{
					BoolValue     = true,
					DateTimeValue = Sql.CurrentTimestamp
				});
			}
		}

		[Test, DataContextSource]
		public void Now(string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { DateTime.Now };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test, DataContextSource]
		public void Parse1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from d in from t in    Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date,
					from d in from t in db.Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date);
		}

		[Test, DataContextSource]
		public void Parse2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from d in from t in    Types select           DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")  where d.Day > 0 select d,
					from d in from t in db.Types select Sql.AsSql(DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")) where d.Day > 0 select d);
		}

		#region DatePart

		[Test, DataContextSource]
		public void DatePartYear(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartQuarter(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartMonth(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartDayOfYear(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartDay(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartWeek(string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Week, t.DateTimeValue))).ToList();
		}

		[Test, DataContextSource]
		public void DatePartWeekDay(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartHour(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartMinute(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void DatePartSecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue)));
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.MySql, ProviderName.Access, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void DatePartMillisecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue)));
		}

		[Test, DataContextSource]
		public void Year(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Year,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Year));
		}

		[Test, DataContextSource]
		public void Month(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Month,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Month));
		}

		[Test, DataContextSource]
		public void DayOfYear(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfYear,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfYear));
		}

		[Test, DataContextSource]
		public void Day(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Day,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Day));
		}

		[Test, DataContextSource]
		public void DayOfWeek(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfWeek,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfWeek));
		}

		[Test, DataContextSource]
		public void Hour(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Hour));
		}

		[Test, DataContextSource]
		public void Minute(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Minute));
		}

		[Test, DataContextSource]
		public void Second(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Second));
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.MySql, ProviderName.Access, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void Millisecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Millisecond,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Millisecond));
		}

		[Test, DataContextSource]
		public void Date(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.AsSql(t.DateTimeValue.Date),
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Date));
		}

		static TimeSpan TruncMiliseconds(TimeSpan ts)
		{
			return new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
		}

		static TimeSpan RoundMiliseconds(TimeSpan ts)
		{
			return new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds + (ts.Milliseconds >= 500 ? 1 : 0));
		}

		[Test, DataContextSource(TestProvName.MySql57)]
		public void TimeOfDay1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		[Test, IncludeDataContextSource(TestProvName.MySql57)]
		public void TimeOfDay2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select RoundMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		#endregion

		#region DateAdd

		[Test, DataContextSource]
		public void DateAddYear(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddQuarter(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddMonth(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddDayOfYear(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddDay(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddWeek(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddWeekDay(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue)).Value.Date);
		}

		[Test, DataContextSource]
		public void DateAddHour(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue). Value.Hour,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)).Value.Hour);
		}

		[Test, DataContextSource]
		public void DateAddMinute(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue). Value.Minute,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue)).Value.Minute);
		}

		[Test, DataContextSource]
		public void DateAddSecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue). Value.Second,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue)).Value.Second);
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.MySql, ProviderName.Access, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void DateAddMillisecond(string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Millisecond, 41, t.DateTimeValue))).ToList();
		}

		[Test, DataContextSource]
		public void AddYears(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddYears(1). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddYears(1)).Date);
		}

		[Test, DataContextSource]
		public void AddMonths(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddMonths(-2). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMonths(-2)).Date);
		}

		[Test, DataContextSource]
		public void AddDays(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddDays(5). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddDays(5)).Date);
		}

		[Test, DataContextSource]
		public void AddHours(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddHours(22). Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(22)).Hour);
		}

		[Test, DataContextSource]
		public void AddMinutes(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddMinutes(-8). Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMinutes(-8)).Minute);
		}

		[Test, DataContextSource]
		public void AddSeconds(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddSeconds(-35). Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddSeconds(-35)).Second);
		}

		[Test, DataContextSource(ProviderName.Informix, ProviderName.MySql, ProviderName.Access, ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
		public void AddMilliseconds(string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMilliseconds(221))).ToList();
		}

		#endregion

		#region DateDiff

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void SubDateDay(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DateDiffDay(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void SubDateHour(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DateDiffHour(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void SubDateMinute(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DateDiffMinute(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void SubDateSecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DateDiffSecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL,
			TestProvName.MariaDB, TestProvName.MySql57, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void SubDateMillisecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds));
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL,
			TestProvName.MariaDB, TestProvName.MySql57, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DateDiffMillisecond(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1))));
		}

		#endregion

		#region MakeDateTime

		[Test, DataContextSource]
		public void MakeDateTime(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test, DataContextSource]
		public void MakeDateTimeParameters(string context)
		{
			var year = 2010;
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(year, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(year, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test, DataContextSource]
		public void NewDateTime1(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t);
		}

		[Test, DataContextSource]
		public void NewDateTime2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1),
					from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1));
		}

		[Test, DataContextSource]
		public void MakeDateTime2(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t);
		}

		[Test, DataContextSource]
		public void NewDateTime3(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t);
		}

		[Test, DataContextSource]
		public void NewDateTime4(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44),
					from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44));
		}

		[Test, DataContextSource]
		public void NewDateTime5(string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t);
		}

		#endregion

		[Test, DataContextSource(ProviderName.PostgreSQL)]
		public void GetDateTest1(string context)
		{
			using (var db = GetDataContext(context))
			{
				var dates =
					from v in db.Parent
						join s in db.Child on v.ParentID equals s.ParentID
					where v.Value1 > 0
					select Sql.GetDate().Date;

				var countByDates =
					from v in dates
					group v by v into g
					select new { g.Key, Count = g.Count() };

				countByDates.Take(5).ToList();
			}
		}

		[Test, DataContextSource]
		public void GetDateTest2(string context)
		{
			using (var db = GetDataContext(context))
			{
				var dates =
					from v in db.Parent
						join s in db.Child on v.ParentID equals s.ParentID
					where v.Value1 > 0
					select Sql.CurrentTimestamp.Date;

				var countByDates =
					from v in dates
					group v by v into g
					select new { g.Key, Count = g.Count() };

				countByDates.Take(5).ToList();
			}
		}

		[Test, DataContextSource(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged, ProviderName.PostgreSQL,
			TestProvName.MariaDB, TestProvName.MySql57, ProviderName.MySql, ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
		public void DateTimeSum(string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types
					group t by t.ID into g
					select new
					{
						ID              = g.Key,
						Count           = g.Count(),
						Duration        = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)).Value,
						HasDuration     = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)).HasValue,
						LongestDuration = g.Max(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue).Value),
					},
					from t in db.Types
					group t by t.ID into g
					select new
					{
						ID              = g.Key,
						Count           = g.Count(),
						Duration        = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)).Value,
						HasDuration     = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)).HasValue,
						LongestDuration = g.Max(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue).Value),
					});
			}
		}
	}
}
