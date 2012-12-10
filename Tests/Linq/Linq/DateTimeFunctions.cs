using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	[TestFixture]
	public class DateTimeFunctions : TestBase
	{
		[Test]
		public void GetDate([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.AsSql(Sql.GetDate()) };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void CurrentTimestamp([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.CurrentTimestamp };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void Now([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { DateTime.Now };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void Parse1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from d in from t in    Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date,
					from d in from t in db.Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date);
		}

		[Test]
		public void Parse2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from d in from t in    Types select           DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")  where d.Day > 0 select d,
					from d in from t in db.Types select Sql.AsSql(DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")) where d.Day > 0 select d);
		}

		#region DatePart

		[Test]
		public void DatePartYear([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue)));
		}

		[Test]
		public void DatePartQuarter([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMonth([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDayOfYear([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDay([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue)));
		}

		[Test]
		public void DatePartWeek([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Week, t.DateTimeValue))).ToList();
		}

		[Test]
		public void DatePartWeekDay([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue)));
		}

		[Test]
		public void DatePartHour([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMinute([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue)));
		}

		[Test]
		public void DatePartSecond([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMillisecond([DataContexts(ProviderName.Informix, ProviderName.MySql, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue)));
		}

		[Test]
		public void Year([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Year,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Year));
		}

		[Test]
		public void Month([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Month,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Month));
		}

		[Test]
		public void DayOfYear([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfYear,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfYear));
		}

		[Test]
		public void Day([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Day,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Day));
		}

		[Test]
		public void DayOfWeek([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfWeek,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfWeek));
		}

		[Test]
		public void Hour([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Hour));
		}

		[Test]
		public void Minute([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Minute));
		}

		[Test]
		public void Second([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Second));
		}

		[Test]
		public void Millisecond([DataContexts(ProviderName.Informix, ProviderName.MySql, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Millisecond,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Millisecond));
		}

		[Test]
		public void Date([DataContexts] string context)
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

		[Test]
		public void TimeOfDay([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMiliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		#endregion

		#region DateAdd

		[Test]
		public void DateAddYear([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Year, 1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddQuarter([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddMonth([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddDayOfYear([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddDay([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddWeek([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddWeekDay([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddHour([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue). Value.Hour,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)).Value.Hour);
		}

		[Test]
		public void DateAddMinute([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue). Value.Minute,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue)).Value.Minute);
		}

		[Test]
		public void DateAddSecond([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue). Value.Second,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue)).Value.Second);
		}

		[Test]
		public void DateAddMillisecond([DataContexts(ProviderName.Informix, ProviderName.MySql, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Millisecond, 41, t.DateTimeValue))).ToList();
		}

		[Test]
		public void AddYears([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddYears(1). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddYears(1)).Date);
		}

		[Test]
		public void AddMonths([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddMonths(-2). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMonths(-2)).Date);
		}

		[Test]
		public void AddDays([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddDays(5). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddDays(5)).Date);
		}

		[Test]
		public void AddHours([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddHours(22). Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(22)).Hour);
		}

		[Test]
		public void AddMinutes([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddMinutes(-8). Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMinutes(-8)).Minute);
		}

		[Test]
		public void AddSeconds([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddSeconds(-35). Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddSeconds(-35)).Second);
		}

		[Test]
		public void AddMilliseconds([DataContexts(ProviderName.Informix, ProviderName.MySql, ProviderName.Access)] string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMilliseconds(221))).ToList();
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays));
		}

		[Test]
		public void DateDiffDay([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateHour([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours));
		}

		[Test]
		public void DateDiffHour([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateMinute([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes));
		}

		[Test]
		public void DateDiffMinute([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test]
		public void SubDateSecond([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds));
		}

		[Test]
		public void DateDiffSecond([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test]
		public void SubDateMillisecond([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds));
		}

		[Test]
		public void DateDiffMillisecond([DataContexts(
			ProviderName.Informix, ProviderName.Oracle, ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLite, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1))));
		}

		#endregion

		#region MakeDateTime

		[Test]
		public void MakeDateTime([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void NewDateTime1([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t);
		}

		[Test]
		public void NewDateTime2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1),
					from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1));
		}

		[Test]
		public void MakeDateTime2([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void NewDateTime3([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t);
		}

		[Test]
		public void NewDateTime4([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44),
					from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44));
		}

		[Test]
		public void NewDateTime5([DataContexts] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t);
		}

		#endregion

		[Test]
		public void GetDateTest1([DataContexts(ProviderName.PostgreSQL)] string context)
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

		[Test]
		public void GetDateTest2([DataContexts] string context)
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
	}
}
