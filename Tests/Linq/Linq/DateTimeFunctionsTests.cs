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
		[Test]
		public void GetDate([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.AsSql(Sql.GetDate()) };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void CurrentTimestamp([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.CurrentTimestamp };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void CurrentTimestampUpdate([DataSources] string context)
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

		[Test]
		public void Now([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { DateTime.Now };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void Parse1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from d in from t in    Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date,
					from d in from t in db.Types select DateTime.Parse(Sql.ConvertTo<string>.From(t.DateTimeValue)) where d.Day > 0 select d.Date);
		}

		[Test]
		public void Parse2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from d in from t in    Types select           DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")  where d.Day > 0 select d,
					from d in from t in db.Types select Sql.AsSql(DateTime.Parse(t.DateTimeValue.Year + "-02-24 00:00:00")) where d.Day > 0 select d);
		}

		#region DatePart

		[Test]
		public void DatePartYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Year, t.DateTimeValue)));
		}

		[Test]
		public void DatePartQuarter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Quarter, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMonth([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Month, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.DayOfYear, t.DateTimeValue)));
		}

		[Test]
		public void DatePartDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Day, t.DateTimeValue)));
		}

		[Test]
		public void DatePartWeek([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Week, t.DateTimeValue))).ToList();
		}

		[Test]
		public void DatePartWeekDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.WeekDay, t.DateTimeValue)));
		}

		[Test]
		public void DatePartHour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Hour, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMinute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Minute, t.DateTimeValue)));
		}

		[Test]
		public void DatePartSecond([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Second, t.DateTimeValue)));
		}

		[Test]
		public void DatePartMillisecond([DataSources(
			ProviderName.Informix, ProviderName.MySql, ProviderName.Access,
			ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue)));
		}

		[Test]
		public void Year([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Year,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Year));
		}

		[Test]
		public void Month([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Month,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Month));
		}

		[Test]
		public void DayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfYear,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfYear));
		}

		[Test]
		public void Day([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Day,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Day));
		}

		[Test]
		public void DayOfWeek([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.DayOfWeek,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.DayOfWeek));
		}

		[Test]
		public void Hour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Hour));
		}

		[Test]
		public void Minute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Minute));
		}

		[Test]
		public void Second([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Second));
		}

		[Test]
		public void Millisecond([DataSources(
			ProviderName.Informix, ProviderName.MySql, ProviderName.Access,
			ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.Millisecond,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Millisecond));
		}

		[Test]
		public void Date([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select Sql.AsSql(t.DateTimeValue.Date),
					from t in db.Types select Sql.AsSql(t.DateTimeValue.Date));
		}

		static TimeSpan TruncMilliseconds(TimeSpan ts)
		{
			return new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds);
		}

		static TimeSpan RoundMilliseconds(TimeSpan ts)
		{
			return new TimeSpan(ts.Hours, ts.Minutes, ts.Seconds + (ts.Milliseconds >= 500 ? 1 : 0));
		}

		[Test]
		public void TimeOfDay1([DataSources(TestProvName.MySql57)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		[Test]
		public void TimeOfDay2([IncludeDataSources(TestProvName.MySql57)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select RoundMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		#endregion

		#region DateAdd

		[Test]
		public void DateAddYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddQuarter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddMonth([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddDayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddWeek([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddWeekDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue). Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue)).Value.Date);
		}

		[Test]
		public void DateAddHour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue). Value.Hour,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)).Value.Hour);
		}

		[Test]
		public void DateAddMinute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue). Value.Minute,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue)).Value.Minute);
		}

		[Test]
		public void DateAddSecond([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue). Value.Second,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue)).Value.Second);
		}

		[Test]
		public void DateAddMillisecond([DataSources(
			ProviderName.Informix, ProviderName.MySql, ProviderName.Access,
			ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
			string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Millisecond, 41, t.DateTimeValue))).ToList();
		}

		[Test]
		public void AddYears([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddYears(1). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddYears(1)).Date);
		}

		[Test]
		public void AddMonths([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddMonths(-2). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMonths(-2)).Date);
		}

		[Test]
		public void AddDays([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddDays(5). Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddDays(5)).Date);
		}

		[Test]
		public void AddHours([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddHours(22). Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(22)).Hour);
		}

		[Test]
		public void AddMinutes([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddMinutes(-8). Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMinutes(-8)).Minute);
		}

		[Test]
		public void AddSeconds([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           t.DateTimeValue.AddSeconds(-35). Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddSeconds(-35)).Second);
		}

		[Test]
		public void AddMilliseconds([DataSources(
			ProviderName.Informix, ProviderName.MySql, ProviderName.Access,
			ProviderName.SapHana, TestProvName.MariaDB, TestProvName.MySql57)]
			string context)
		{
			using (var db = GetDataContext(context))
				(from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMilliseconds(221))).ToList();
		}

		[Test]
		public void AddDaysFromColumnPositive([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = 2, DateTimeValue = new DateTime(2018, 01, 03) });
				try
				{
					var result = db.Types
						.Count(t => t.ID == 5000 && t.DateTimeValue.AddDays(t.SmallIntValue) > new DateTime(2018, 01, 02));
					Assert.AreEqual(1, result);
				}
				finally
				{
					db.Types.Delete(t => t.ID == 5000);
				}
			}
		}

		[Test]
		public void AddDaysFromColumnNegative([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = -2, DateTimeValue = new DateTime(2018, 01, 03) });

				var result = db.Types
					.Count(t => t.ID == 5000 && t.DateTimeValue.AddDays(t.SmallIntValue) < new DateTime(2018, 01, 02));

				Assert.AreEqual(1, result);

				db.Types.Delete(t => t.ID == 5000);
			}
		}

		[Test]
		public void AddDaysFromColumn([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var needsFix = db.ProviderNeedsTimeFix(context);

				AreEqual(Types.Select(t => TestUtils.FixTime(t.DateTimeValue.AddDays(t.SmallIntValue), needsFix)),
					db.Types.Select(t => t.DateTimeValue.AddDays(t.SmallIntValue)));
			}
		}

		[Test]
		public void AddWeekFromColumn([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue).Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue)).Value.Date);
			}
		}

		[Test]
		public void AddQuarterFromColumn([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue).Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue)).Value.Date);
			}
		}

		[Test]
		public void AddYearFromColumn([DataSources(ProviderName.Informix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue).Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue)).Value.Date);
			}
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays));
		}

		[Test,]
		public void DateDiffDay([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateHour([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours));
		}

		[Test]
		public void DateDiffHour([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateMinute([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes));
		}

		[Test]
		public void DateDiffMinute([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test]
		public void SubDateSecond([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds));
		}

		[Test]
		public void DateDiffSecond([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, ProviderName.MySql, ProviderName.SQLiteClassic,
			ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test]
		public void SubDateMillisecond([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, TestProvName.MariaDB, TestProvName.MySql57, ProviderName.MySql,
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds));
		}

		[Test]
		public void DateDiffMillisecond([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, TestProvName.MariaDB, TestProvName.MySql57, ProviderName.MySql,
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
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
		public void MakeDateTime([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void MakeDateTimeParameters([DataSources] string context)
		{
			var year = 2010;
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(year, p.ID, 1) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(year, p.ID, 1) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void NewDateTime1([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1) where t.Month == 10 select t);
		}

		[Test]
		public void NewDateTime2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1),
					from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1));
		}

		[Test]
		public void MakeDateTime2([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010, p.ID, 1, 20, 35, 44) where t.Value.Year == 2010 select t);
		}

		[Test]
		public void NewDateTime3([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44) where t.Month == 10 select t);
		}

		[Test]
		public void NewDateTime4([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from p in    Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44),
					from p in db.Types select new DateTime(p.DateTimeValue.Year, 10, 1, 20, 35, 44));
		}

		[Test]
		public void NewDateTime5([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t,
					from t in from p in db.Types select new DateTime(p.DateTimeValue.Year + 1, 10, 1) where t.Month == 10 select t);
		}

		#endregion

		[Test]
		public void GetDateTest1([DataSources(ProviderName.PostgreSQL)] string context)
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

				var _ = countByDates.Take(5).ToList();
			}
		}

		[Test]
		public void GetDateTest2([DataSources] string context)
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

				var _ = countByDates.Take(5).ToList();
			}
		}

		[Test]
		public void DateTimeSum([DataSources(
			ProviderName.Informix, ProviderName.OracleNative, ProviderName.OracleManaged,
			ProviderName.PostgreSQL, TestProvName.MariaDB, TestProvName.MySql57, ProviderName.MySql,
			ProviderName.SQLiteClassic, ProviderName.SQLiteMS, ProviderName.Access)]
			string context)
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
