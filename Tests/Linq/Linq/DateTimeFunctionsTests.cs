using System;
using System.Linq;

using LinqToDB;

using NUnit.Framework;

namespace Tests.Linq
{
	using LinqToDB.Mapping;
	using Model;
	using System.Collections.Generic;

	[TestFixture]
	public class DateTimeFunctionsTests : TestBase
	{
		//This custom comparers allows for an error of 1 millisecond.
		public class CustomIntComparer : IEqualityComparer<int>
		{
			public bool Equals(int x, int y) => (x >= (y - 1) && x <= (y + 1));

			public int GetHashCode(int x) => 0;
		}

		public class CustomNullableIntComparer : IEqualityComparer<int?>
		{
			public bool Equals(int? x, int? y)
			{
				if (!x.HasValue) return false;
				if (!y.HasValue) return false;
				return (x.Value >= (y.Value - 1) && x.Value <= (y.Value + 1));
			}

			public int GetHashCode(int? x) => 0;
		}

		public class CustomNullableDateTimeComparer : IEqualityComparer<DateTime?>
		{
			public bool Equals(DateTime? x, DateTime? y)
			{
				if (!x.HasValue) return false;
				if (!y.HasValue) return false;
				return x.Value.Between(y.Value.AddMilliseconds(-1), y.Value.AddMilliseconds(1));
			}

			public int GetHashCode(DateTime? x) => 0;
		}

		public class CustomDateTimeComparer : IEqualityComparer<DateTime>
		{
			public bool Equals(DateTime x, DateTime y)
			{
				return x.Between(y.AddMilliseconds(-1), y.AddMilliseconds(1));
			}

			public int GetHashCode(DateTime x) => 0;
		}

		[Test]
		public void GetDate([DataSources] string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.AsSql(Sql.GetDate()) };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void CurrentTimestamp([DataSources] string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.CurrentTimestamp };
				Assert.AreEqual(DateTime.Now.Year, q.ToList().First().Now.Year);
			}
		}

		[Test]
		public void CurrentTimestampUtcClientSide()
		{
			var delta = Sql.CurrentTimestampUtc - DateTime.UtcNow;
			Assert.IsTrue(delta.Between(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1)));
			Assert.AreEqual(DateTimeKind.Utc, Sql.CurrentTimestampUtc.Kind);
		}

		[Test]
		public void CurrentTimestampUtc(
			[DataSources(ProviderName.Access, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.SqlCe,
				ProviderName.SqlServer2000, ProviderName.SqlServer2005)]
			string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var dbUtcNow = db.Select(() => Sql.CurrentTimestampUtc);

				var now   = DateTime.UtcNow;
				var delta = now - dbUtcNow;
				Assert.IsTrue(
					delta.Between(TimeSpan.FromSeconds(-120), TimeSpan.FromSeconds(120)),
					$"{now}, {dbUtcNow}, {delta}");

				// we don't set kind
				Assert.AreEqual(DateTimeKind.Unspecified, dbUtcNow.Kind);
			}
		}

		[Test]
		public void CurrentTzTimestamp(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle, TestProvName.AllPostgreSQL10Plus)]
			string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var dbTzNow = db.Select(() => Sql.CurrentTzTimestamp);

				var now   = DateTimeOffset.Now;
				var delta = now - dbTzNow;
				Assert.IsTrue(
					delta.Between(TimeSpan.FromSeconds(-120), TimeSpan.FromSeconds(120)),
					$"{now}, {dbTzNow}, {delta}");
			}
		}

		[Test]
		public void CurrentTimestampUtcClientSideParameter(
			[IncludeDataSources(true, TestProvName.AllAccess, ProviderName.Firebird, TestProvName.Firebird3, ProviderName.SqlCe)]
			string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var dbUtcNow = db.Select(() => Sql.CurrentTimestampUtc);

				var delta = dbUtcNow - DateTime.UtcNow;
				Assert.IsTrue(delta.Between(TimeSpan.FromSeconds(-5), TimeSpan.FromSeconds(5)));

				// we don't set kind
				Assert.AreEqual(DateTimeKind.Unspecified, dbUtcNow.Kind);
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
			using (new DisableBaseline("Server-side date generation test"))
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
		public void DatePartWeekNumberingType([DataSources(false)] string context)
		{
			using (var db = new TestDataConnection(context))
			{
				var dates = new[]
				{
					new DateTime(2018, 12, 28),
					new DateTime(2018, 12, 29),
					new DateTime(2018, 12, 30),
					new DateTime(2018, 12, 31),
					new DateTime(2019, 1, 1),
					new DateTime(2019, 1, 2),
					new DateTime(2019, 1, 3),
					new DateTime(2019, 1, 4),
					new DateTime(2019, 1, 5),
					new DateTime(2019, 1, 6),
					new DateTime(2019, 1, 7),
					new DateTime(2019, 1, 8)
				};

				// actually 53 should be 1st week of 2019, but..
				var isoWeeks              = new[] { 52, 52, 52, 53, 1, 1, 1, 1, 1, 1, 2, 2 };
				var sqliteParodyNumbering = new[] { 52, 52, 52, 53, 0, 0, 0, 0, 0, 0, 1, 1 };
				var isoProperWeeks        = new[] { 52, 52, 52,  1, 1, 1, 1, 1, 1, 1, 2, 2 };
				var usWeeks               = new[] { 52, 52, 53, 53, 1, 1, 1, 1, 1, 2, 2, 2 };
				var usWeeksZeroBased      = new[] { 51, 51, 52, 52, 0, 0, 0, 0, 0, 1, 1, 1 };
				var muslimWeeks           = new[] { 52, 53, 53, 53, 1, 1, 1, 1, 2, 2, 2, 2 };
				var primitive             = new[] { 52, 52, 52, 53, 1, 1, 1, 1, 1, 1, 1, 2 };

				var results = dates
					.Select(date => db.Select(() => Sql.AsSql(Sql.DatePart(Sql.DateParts.Week, Sql.ToSql(date)))))
					.AsEnumerable()
					.Select(_ => _!.Value)
					.ToArray();

				if (isoWeeks.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses ISO week numbering schema");
				}
				else if (isoProperWeeks.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses PROPER ISO week numbering schema");
				}
				else if (usWeeks.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses US week numbering schema");
				}
				else if (muslimWeeks.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses Islamic week numbering schema");
				}
				else if (primitive.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses PRIMITIVE week numbering schema");
				}
				else if (sqliteParodyNumbering.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses SQLite inhuman numbering logic");
				}
				else if (usWeeksZeroBased.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses US 0-based week numbering schema");
				}
				else
				{
					Assert.Fail($"Context {db.DataProvider.Name} uses unknown week numbering schema");
				}
			}
		}

		[Test]
		public void DatePartWeekNumberingTypeCSharp()
		{
			var dates = new[]
			{
					new DateTime(2018, 12, 28),
					new DateTime(2018, 12, 29),
					new DateTime(2018, 12, 30),
					new DateTime(2018, 12, 31),
					new DateTime(2019, 1, 1),
					new DateTime(2019, 1, 2),
					new DateTime(2019, 1, 3),
					new DateTime(2019, 1, 4),
					new DateTime(2019, 1, 5),
					new DateTime(2019, 1, 6),
					new DateTime(2019, 1, 7),
					new DateTime(2019, 1, 8)
				};

				// actually 53 should be 1st week of 2019, but..
				var isoWeeks              = new[] { 52, 52, 52, 53, 1, 1, 1, 1, 1, 1, 2, 2 };
				var sqliteParodyNumbering = new[] { 52, 52, 52, 53, 0, 0, 0, 0, 0, 0, 1, 1 };
				var isoProperWeeks        = new[] { 52, 52, 52,  1, 1, 1, 1, 1, 1, 1, 2, 2 };
				var usWeeks               = new[] { 52, 52, 53, 53, 1, 1, 1, 1, 1, 2, 2, 2 };
				var usWeeksZeroBased      = new[] { 51, 51, 52, 52, 0, 0, 0, 0, 0, 1, 1, 1 };
				var muslimWeeks           = new[] { 52, 53, 53, 53, 1, 1, 1, 1, 2, 2, 2, 2 };
				var primitive             = new[] { 52, 52, 52, 53, 1, 1, 1, 1, 1, 1, 1, 2 };

			var results = dates.Select(date => Sql.DatePart(Sql.DateParts.Week, date)!.Value).ToArray();

			if (isoWeeks.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses ISO week numbering schema");
			}
			else if (isoProperWeeks.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses PROPER ISO week numbering schema");
			}
			else if (usWeeks.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses US week numbering schema");
			}
			else if (muslimWeeks.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses Islamic week numbering schema");
			}
			else if (primitive.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses PRIMITIVE week numbering schema");
			}
			else if (sqliteParodyNumbering.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses SQLite inhuman numbering logic");
			}
			else if (usWeeksZeroBased.SequenceEqual(results))
			{
				Assert.Pass("Sql.DatePart C# implementation uses US 0-based week numbering schema");
			}
			else
			{
				Assert.Fail("Sql.DatePart C# implementation uses unknown week numbering schema");
			}
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
		public void DatePartMillisecond([DataSources(TestProvName.AllInformix, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue),
					from t in db.Types select Sql.AsSql(Sql.DatePart(Sql.DateParts.Millisecond, t.DateTimeValue)));
		}

		[Test]
		public void DatepartDynamic(
			[DataSources(TestProvName.AllInformix)] string context,
			[Values(
				Sql.DateParts.Day,
				Sql.DateParts.Hour,
				Sql.DateParts.Minute,
				Sql.DateParts.Month,
				Sql.DateParts.Year,
				Sql.DateParts.Second
				)] Sql.DateParts datepart)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					from t in Types select Sql.DatePart(datepart, t.DateTimeValue);
				var result =
					from t in db.Types select Sql.AsSql(Sql.DatePart(datepart, t.DateTimeValue));

				AreEqual(expected, result);
			}
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
		public void Millisecond([DataSources(TestProvName.AllInformix, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.Millisecond,
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
		public void TimeOfDay1([DataSources(TestProvName.AllMySqlServer57Plus)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		[Test]
		public void TimeOfDay2([IncludeDataSources(TestProvName.AllMySqlServer57Plus)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select RoundMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		#endregion

		#region DateAdd

		[Test]
		public void DateAddYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddQuarter([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddMonth([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddDayOfYear([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddWeek([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddWeekDay([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddHour([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)!. Value.Hour,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue))!.Value.Hour);
		}

		[Test]
		public void DateAddMinute([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue)!. Value.Minute,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue))!.Value.Minute);
		}

		[Test]
		public void DateAddSecond([DataSources] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue)!. Value.Second,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue))!.Value.Second);
		}

		[Test]
		public void DateAddMillisecond([DataSources(TestProvName.AllInformix, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
						from t in db.Types select           Sql.DateAdd(Sql.DateParts.Millisecond, 226, t.DateTimeValue),
						from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Millisecond, 226, t.DateTimeValue)),
						new CustomNullableDateTimeComparer());
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
		public void AddMilliseconds([DataSources(TestProvName.AllInformix, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in db.Types select (t.DateTimeValue.AddMilliseconds(226)),
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMilliseconds(226)),
					new CustomDateTimeComparer());
		}

		[Test]
		public void AddDaysFromColumnPositive([DataSources(TestProvName.AllInformix)] string context)
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
		public void AddDaysFromColumnNegative([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = -2, DateTimeValue = new DateTime(2018, 01, 03) });

				try
				{
					var result = db.Types
						.Count(t => t.ID == 5000 && Sql.AsSql(t.DateTimeValue.AddDays(t.SmallIntValue)) < new DateTime(2018, 01, 02));

					Assert.AreEqual(1, result);
				}
				finally
				{
					db.Types.Delete(t => t.ID == 5000);
				}
			}
		}

		[Test]
		public void AddDaysFromColumn([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				var needsFix = db.ProviderNeedsTimeFix(context);

				AreEqual(Types.Select(t => TestUtils.StripMilliseconds(t.DateTimeValue.AddDays(t.SmallIntValue), needsFix)),
					db.Types.Select(t => t.DateTimeValue.AddDays(t.SmallIntValue)));
			}
		}

		[Test]
		public void AddWeekFromColumn([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue))!.Value.Date);
			}
		}

		[Test]
		public void AddQuarterFromColumn([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue))!.Value.Date);
			}
		}

		[Test]
		public void AddYearFromColumn([DataSources(TestProvName.AllInformix)] string context)
		{
			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue))!.Value.Date);
			}
		}

		public static DateTime Truncate(DateTime date, long resolution)
	    {
	        return new DateTime(date.Ticks - (date.Ticks % resolution), date.Kind);
	    }

		[Test]
		public void AddDynamicFromColumn(
			[DataSources(TestProvName.AllInformix)] string context,
			[Values(
				Sql.DateParts.Day,
				Sql.DateParts.Hour,
				Sql.DateParts.Minute,
				Sql.DateParts.Month,
				Sql.DateParts.Year,
				Sql.DateParts.Second
				)] Sql.DateParts datepart)
		{
			using (var db = GetDataContext(context))
			{
				var expected =
					(from t in Types select Sql.DateAdd(datepart, t.SmallIntValue, t.DateTimeValue)).Select(d =>
						Truncate(d!.Value, TimeSpan.TicksPerSecond));
				var result =
					(from t in db.Types select Sql.AsSql(Sql.DateAdd(datepart, t.SmallIntValue, t.DateTimeValue)))
					.ToList().Select(d => Truncate(d!.Value, TimeSpan.TicksPerSecond));

				AreEqual(expected, result);
			}
		}

		#endregion

		#region DateAdd Expression

		[Test]
		public void DateAddYearExpression([DataSources] string context)
		{
			var part1 = 6;
			var part2 = 5;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Year, 11, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, part1 + part2, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddQuarterExpression([DataSources] string context)
		{
			var part1 = 6;
			var part2 = 5;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Quarter, -1, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, part2 - part1, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddMonthExpression([DataSources] string context)
		{
			var part1 = 5;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Month, 2, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, part1 - part2, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddDayOfYearExpression([DataSources] string context)
		{
			var part1 = 6;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, part1 - part2, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddDayExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Day, 5, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, part1 + part2, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddWeekExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Week, -1, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, part1 - part2, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddWeekDayExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, part2 - part1, t.DateTimeValue))!.Value.Date);
		}

		[Test]
		public void DateAddHourExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)!.Value.Hour,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, part2 - part1, t.DateTimeValue))!.Value.Hour);
		}

		[Test]
		public void DateAddMinuteExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Minute, 5, t.DateTimeValue)!.Value.Minute,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, part1 + part2, t.DateTimeValue))!.Value.Minute);
		}

		[Test]
		public void DateAddSecondExpression([DataSources] string context)
		{
			var part1 = 20;
			var part2 = 21;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Second, 41, t.DateTimeValue)!.Value.Second,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, part1 + part2, t.DateTimeValue))!.Value.Second);
		}

		[Test]
		public void DateAddMillisecondExpression([DataSources(TestProvName.AllInformix, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql)] string context)
		{
			var part1 = 200;
			var part2 = 26;

			using (var db = GetDataContext(context))
				AreEqual(
						from t in db.Types select Sql.DateAdd(Sql.DateParts.Millisecond, 226, t.DateTimeValue),
						from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Millisecond, part1 + part2, t.DateTimeValue)),
						new CustomNullableDateTimeComparer());
		}

		[Test]
		public void AddYearsExpression([DataSources] string context)
		{
			var part1 = 5;
			var part2 = 4;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.AddYears(1).Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddYears(part1 - part2)).Date);
		}

		[Test]
		public void AddMonthsExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 4;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.AddMonths(-2).Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMonths(part1 - part2)).Date);
		}

		[Test]
		public void AddDaysExpression([DataSources] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.AddDays(5).Date,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddDays(part1 + part2)).Date);
		}

		[Test]
		public void AddHoursExpression([DataSources] string context)
		{
			var part1 = 11;
			var part2 = 11;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.AddHours(22).Hour,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(part1 + part2)).Hour);
		}

		[Test]
		public void AddMinutesExpression([DataSources] string context)
		{
			var part1 = 1;
			var part2 = 9;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.AddMinutes(-8).Minute,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMinutes(part1 - part2)).Minute);
		}

		[Test]
		public void AddSecondsExpression([DataSources] string context)
		{
			var part1 = 5;
			var part2 = 40;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select t.DateTimeValue.AddSeconds(-35).Second,
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddSeconds(part1 - part2)).Second);
		}

		[Test]
		public void AddMillisecondsExpression([DataSources(TestProvName.AllInformix, TestProvName.AllAccess, TestProvName.AllSapHana, TestProvName.AllMySql)]
			string context)
		{
			var part1 = 150;
			var part2 = 76;

			using (var db = GetDataContext(context))
				AreEqual(
					from t in db.Types select (t.DateTimeValue.AddMilliseconds(226)),
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddMilliseconds(part1 + part2)),
					new CustomDateTimeComparer());
		}

		[Test]
		public void AddDaysFromColumnPositiveExpression([DataSources(TestProvName.AllInformix)] string context)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = 2, DateTimeValue = new DateTime(2018, 01, 03) });
				try
				{
					var result = db.Types
						.Count(t => t.ID == 5000 && t.DateTimeValue.AddDays(t.SmallIntValue + part1 - part2) > new DateTime(2018, 01, 02));
					Assert.AreEqual(1, result);
				}
				finally
				{
					db.Types.Delete(t => t.ID == 5000);
				}
			}
		}

		[Test]
		public void AddDaysFromColumnNegativeExpression([DataSources(TestProvName.AllInformix)] string context)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				db.Insert(new LinqDataTypes { ID = 5000, SmallIntValue = -2, DateTimeValue = new DateTime(2018, 01, 03) });

				try
				{
					var result = db.Types
						.Count(t => t.ID == 5000 && Sql.AsSql(t.DateTimeValue.AddDays(t.SmallIntValue + part1 - part2)) < new DateTime(2018, 01, 02));

					Assert.AreEqual(1, result);
				}
				finally
				{
					db.Types.Delete(t => t.ID == 5000);
				}
			}
		}

		[Test]
		public void AddDaysFromColumnExpression([DataSources(TestProvName.AllInformix)] string context)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				var needsFix = db.ProviderNeedsTimeFix(context);

				AreEqual(Types.Select(t => TestUtils.StripMilliseconds(t.DateTimeValue.AddDays(t.SmallIntValue + part1 - part2), needsFix)),
					db.Types.Select(t => t.DateTimeValue.AddDays(t.SmallIntValue)));
			}
		}

		[Test]
		public void AddWeekFromColumnExpression([DataSources(TestProvName.AllInformix)] string context)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, t.SmallIntValue + part1 - part2, t.DateTimeValue))!.Value.Date);
			}
		}

		[Test]
		public void AddQuarterFromColumnExpression([DataSources(TestProvName.AllInformix)] string context)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, t.SmallIntValue + part1 - part2, t.DateTimeValue))!.Value.Date);
			}
		}

		[Test]
		public void AddYearFromColumnExpression([DataSources(TestProvName.AllInformix)] string context)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				AreEqual(
					from t in Types select Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue, t.DateTimeValue)!.Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, t.SmallIntValue + part1 - part2, t.DateTimeValue))!.Value.Date);
			}
		}

		[Test]
		public void AddDynamicFromColumnExpression(
			[DataSources(TestProvName.AllInformix)] string context,
			[Values(
				Sql.DateParts.Day,
				Sql.DateParts.Hour,
				Sql.DateParts.Minute,
				Sql.DateParts.Month,
				Sql.DateParts.Year,
				Sql.DateParts.Second
				)] Sql.DateParts datepart)
		{
			var part1 = 4;
			var part2 = 4;

			using (var db = GetDataContext(context))
			{
				var expected =
					(from t in Types select Sql.DateAdd(datepart, t.SmallIntValue, t.DateTimeValue)).Select(d =>
						Truncate(d!.Value, TimeSpan.TicksPerSecond));
				var result =
					(from t in db.Types select Sql.AsSql(Sql.DateAdd(datepart, t.SmallIntValue + part1 - part2, t.DateTimeValue)))
					.ToList().Select(d => Truncate(d!.Value, TimeSpan.TicksPerSecond));

				AreEqual(expected, result);
			}
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays));
		}

		[Test]
		public void DateDiffDay(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateHour(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours));
		}

		[Test]
		public void DateDiffHour(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateMinute(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes));
		}

		[Test]
		public void DateDiffMinute(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[Test]
		public void SubDateSecond(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds));
		}

		[Test]
		public void DateDiffSecond(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		// This test and DateDiffMillisecond could fail for SQLite.MS due to 1 millisecond difference in
		// expected and returned results
		// This happen only on following conditions:
		// - access provider enabled
		// - tests against run before those tests (at least AddDynamicFromColumn)
		// Possible reason:
		// looks like Access runtime modify some C++ runtime options that affect runtime's rounding behavior
		// used also by SQLite provider's native part
		[Test]
		public void SubDateMillisecond(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllMySql,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				if (context.Contains(ProviderName.SQLiteMS))
				{
					AreEqual(
						from t in Types select (int)(t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds,
						from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds),
						new CustomIntComparer());
				}
				else
				{
					AreEqual(
						from t in Types select (int)(t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds,
						from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddSeconds(1) - t.DateTimeValue).TotalMilliseconds));
				}
			}
		}

		// see SubDateMillisecond commet for SQLite.MS
		[Test]
		public void DateDiffMillisecond(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllMySql,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				if(context.Contains(ProviderName.SQLiteMS))
				{
					AreEqual(
						from t in Types select Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1)),
						from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1))),
						new CustomNullableIntComparer());
				}
				else
				{
					AreEqual(
						from t in Types select Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1)),
						from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddSeconds(1))));
				}
			}
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

		[ActiveIssue("SQL0418N", Configuration = ProviderName.DB2)]
		[Test]
		public void GetDateTest1([DataSources] string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
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
		public void DateTimeSum(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllOracle,
				TestProvName.AllMySql,
				TestProvName.AllSQLite,
				TestProvName.AllAccess)]
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
						Duration        = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue))!.Value,
						HasDuration     = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)).HasValue,
						LongestDuration = g.Max(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)!.Value),
					},
					from t in db.Types
					group t by t.ID into g
					select new
					{
						ID              = g.Key,
						Count           = g.Count(),
						Duration        = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue))!.Value,
						HasDuration     = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)).HasValue,
						LongestDuration = g.Max(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue)!.Value),
					});
			}
		}

		[Test]
		public void Issue1615Test([DataSources] string context)
		{
			using (var db = GetDataContext(context))
			{
				var datePart = Sql.DateParts.Day;
				AreEqual(
					from t in    Types select           Sql.DateAdd(datePart, 5, t.DateTimeValue)!. Value.Date,
					from t in db.Types select Sql.AsSql(Sql.DateAdd(datePart, 5, t.DateTimeValue))!.Value.Date);
			}
		}
	}
}
