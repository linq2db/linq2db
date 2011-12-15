using System;
using System.Linq;

using LinqToDB.Data.DataProvider;
using LinqToDB.Data.Linq;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class DateTimeFunctions : TestBase
	{
		[Test]
		public void GetDate()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.AsSql(Sql.GetDate()) };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			});
		}

		[Test]
		public void CurrentTimestamp()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.CurrentTimestamp };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			});
		}

		[Test]
		public void Now()
		{
			ForEachProvider(db =>
			{
				var q = from p in db.Person where p.ID == 1 select new { DateTime.Now };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			});
		}

		[Test]
		public void Parse1()
		{
			ForEachProvider(db => AreEqual(
				from d in from t in    Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date,
				from d in from t in db.Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date));
		}

		[Test]
		public void Parse2()
		{
			ForEachProvider(db => AreEqual(
				from d in from t in    Types select           DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")  where d.Day > 0 select d,
				from d in from t in db.Types select Sql.AsSql(DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")) where d.Day > 0 select d));
		}

		#region DatePart

		[Test]
		public void DatePartYear()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue))));
		}

		[Test]
		public void DatePartQuarter()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue))));
		}

		[Test]
		public void DatePartMonth()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue))));
		}

		[Test]
		public void DatePartDayOfYear()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue))));
		}

		[Test]
		public void DatePartDay()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue))));
		}

		[Test]
		public void DatePartWeek()
		{
			ForEachProvider(db => 
				(from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Week, t.DateTimeValue))).ToList());
		}

		[Test]
		public void DatePartWeekDay()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue))));
		}

		[Test]
		public void DatePartHour()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue))));
		}

		[Test]
		public void DatePartMinute()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue))));
		}

		[Test]
		public void DatePartSecond()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue))));
		}

		[Test]
		public void DatePartMillisecond()
		{
			ForEachProvider(new[] { ProviderName.Informix, ProviderName.MySql, ProviderName.Access }, db => AreEqual(
				from t in    Types select           Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
				from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue))));
		}

		[Test]
		public void Year()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.Year,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Year)));
		}

		[Test]
		public void Month()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.Month,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Month)));
		}

		[Test]
		public void DayOfYear()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.DayOfYear,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfYear)));
		}

		[Test]
		public void Day()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.Day,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Day)));
		}

		[Test]
		public void DayOfWeek()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.DayOfWeek,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfWeek)));
		}

		[Test]
		public void Hour()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.Hour,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Hour)));
		}

		[Test]
		public void Minute()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.Minute,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Minute)));
		}

		[Test]
		public void Second()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.Second,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Second)));
		}

		[Test]
		public void Millisecond()
		{
			ForEachProvider(new[] { ProviderName.Informix, ProviderName.MySql, ProviderName.Access }, db => AreEqual(
				from t in    Types select           t.DateTimeValue.Millisecond,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Millisecond)));
		}

		[Test]
		public void Date()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select Sql.AsSql(t.DateTimeValue.Date),
				from t in db.Types select Sql.AsSql(t.DateTimeValue.Date)));
		}

		static TimeSpan TruncMiliseconds(TimeSpan ts)
		{
			return new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
		}

		[Test]
		public void TimeOfDay()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
				from t in db.Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay))));
		}

		#endregion

		#region DateAdd

		[Test]
		public void DateAddYear()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Year, 1, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 1, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddQuarter()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddMonth()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddDayOfYear()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddDay()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddWeek()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddWeekDay()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue). Value.Date,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue)).Value.Date));
		}

		[Test]
		public void DateAddHour()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue). Value.Hour,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)).Value.Hour));
		}

		[Test]
		public void DateAddMinute()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue). Value.Minute,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue)).Value.Minute));
		}

		[Test]
		public void DateAddSecond()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue). Value.Second,
				from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue)).Value.Second));
		}

		[Test]
		public void DateAddMillisecond()
		{
			ForEachProvider(new[] { ProviderName.Informix, ProviderName.MySql, ProviderName.Access },
				db => (from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Millisecond, 41, t.DateTimeValue))).ToList());
		}

		[Test]
		public void AddYears()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.AddYears(1). Date,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.AddYears(1)).Date));
		}

		[Test]
		public void AddMonths()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.AddMonths(-2). Date,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMonths(-2)).Date));
		}

		[Test]
		public void AddDays()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.AddDays(5). Date,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.AddDays(5)).Date));
		}

		[Test]
		public void AddHours()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.AddHours(22). Hour,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(22)).Hour));
		}

		[Test]
		public void AddMinutes()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.AddMinutes(-8). Minute,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMinutes(-8)).Minute));
		}

		[Test]
		public void AddSeconds()
		{
			ForEachProvider(db => AreEqual(
				from t in    Types select           t.DateTimeValue.AddSeconds(-35). Second,
				from t in db.Types select Sql.AsSql(t.DateTimeValue.AddSeconds(-35)).Second));
		}

		[Test]
		public void AddMilliseconds()
		{
			ForEachProvider(new[] { ProviderName.Informix, ProviderName.MySql, ProviderName.Access },
				db => (from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMilliseconds(221))).ToList());
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays)));
		}

		[Test]
		public void DateDiffDay()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)))));
		}

		[Test]
		public void SubDateHour()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours)));
		}

		[Test]
		public void DateDiffHour()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)))));
		}

		[Test]
		public void SubDateMinute()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes)));
		}

		[Test]
		public void DateDiffMinute()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)))));
		}

		[Test]
		public void SubDateSecond()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds)));
		}

		[Test]
		public void DateDiffSecond()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)))));
		}

		[Test]
		public void SubDateMillisecond()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds)));
		}

		[Test]
		public void DateDiffMillisecond()
		{
			ForEachProvider(
				new[] { ProviderName.Informix, "Oracle", ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access },
				db => AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1)))));
		}

		#endregion

		#region MakeDateTime

		[Test]
		public void MakeDateTime()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t,
				from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t));
		}

		[Test]
		public void NewDateTime1()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t,
				from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t));
		}

		[Test]
		public void NewDateTime2()
		{
			ForEachProvider(db => AreEqual(
				from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1),
				from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1)));
		}

		[Test]
		public void MakeDateTime2()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t,
				from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t));
		}

		[Test]
		public void NewDateTime3()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t,
				from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t));
		}

		[Test]
		public void NewDateTime4()
		{
			ForEachProvider(db => AreEqual(
				from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44),
				from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44)));
		}

		[Test]
		public void NewDateTime5()
		{
			ForEachProvider(db => AreEqual(
				from t in from p in    Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t,
				from t in from p in db.Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t));
		}

		#endregion
	}
}
