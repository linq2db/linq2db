using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Tests.Model;

namespace Tests.Linq
{
	[TestFixture]
	public class DateTimeFunctionsTests : TestBase
	{
		//This custom comparers allows for an error of 1 millisecond.
		public class CustomIntComparer : IEqualityComparer<int>
		{
			private readonly int _precision;

			public CustomIntComparer(int precision)
			{
				_precision = precision;
			}

			public bool Equals(int x, int y) => (x >= (y - _precision) && x <= (y + _precision));

			public int GetHashCode(int obj) => 0;
		}

		public class CustomNullableIntComparer : IEqualityComparer<int?>
		{
			private readonly int _precision;

			public CustomNullableIntComparer(int precision)
			{
				_precision = precision;
			}

			public bool Equals(int? x, int? y)
			{
				if (!x.HasValue) return false;
				if (!y.HasValue) return false;
				return (x.Value >= (y.Value - _precision) && x.Value <= (y.Value + _precision));
			}

			public int GetHashCode(int? obj) => 0;
		}

		public class CustomNullableDateTimeComparer : IEqualityComparer<DateTime?>
		{
			public bool Equals(DateTime? x, DateTime? y)
			{
				if (!x.HasValue) return false;
				if (!y.HasValue) return false;
				return x.Value.Between(y.Value.AddMilliseconds(-1), y.Value.AddMilliseconds(1));
			}

			public int GetHashCode(DateTime? obj) => 0;
		}

		public class CustomDateTimeComparer : IEqualityComparer<DateTime>
		{
			public bool Equals(DateTime x, DateTime y)
			{
				return x.Between(y.AddMilliseconds(-1), y.AddMilliseconds(1));
			}

			public int GetHashCode(DateTime obj) => 0;
		}

		[Test]
		public void GetDate([DataSources] string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.AsSql(Sql.GetDate()) };
				Assert.That(q.ToList().First().Now.Year, Is.EqualTo(DateTime.Now.Year));
			}
		}

		[Test]
		public void CurrentTimestamp([DataSources] string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var q = from p in db.Person where p.ID == 1 select new { Now = Sql.CurrentTimestamp };
				Assert.That(q.ToList().First().Now.Year, Is.EqualTo(DateTime.Now.Year));
			}
		}

		[Test]
		public void CurrentTimestampUtcClientSide()
		{
			var delta = Sql.CurrentTimestampUtc - DateTime.UtcNow;
			Assert.Multiple(() =>
			{
				Assert.That(delta.Between(TimeSpan.FromSeconds(-1), TimeSpan.FromSeconds(1)), Is.True);
				Assert.That(Sql.CurrentTimestampUtc.Kind, Is.EqualTo(DateTimeKind.Utc));
			});
		}

		[Test]
		public void CurrentTimestampUtc(
			[DataSources(TestProvName.AllAccess, TestProvName.AllFirebird, ProviderName.SqlCe,
				TestProvName.AllSqlServer2005)]
			string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var dbUtcNow = db.Select(() => Sql.CurrentTimestampUtc);

				var now   = DateTime.UtcNow;
				var delta = now - dbUtcNow;
				Assert.That(
					delta.Between(TimeSpan.FromSeconds(-120), TimeSpan.FromSeconds(120)), Is.True,
					$"{now}, {dbUtcNow}, {delta}");

				// we don't set kind and rely on provider's behavior
				// Most providers return Unspecified, but at least it shouldn't be Local
				if (context.IsAnyOf(ProviderName.ClickHouseOctonica, ProviderName.ClickHouseClient))
					Assert.That(dbUtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
				else
					Assert.That(dbUtcNow.Kind, Is.EqualTo(DateTimeKind.Unspecified));
			}
		}

		[Test]
		public void CurrentTzTimestamp(
			[IncludeDataSources(TestProvName.AllSqlServer2008Plus, TestProvName.AllOracle, TestProvName.AllPostgreSQL10Plus, TestProvName.AllClickHouse)]
			string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var dbTzNow = db.Select(() => Sql.CurrentTzTimestamp);

				var now   = DateTimeOffset.Now;
				var delta = now - dbTzNow;
				Assert.That(
					delta.Between(TimeSpan.FromSeconds(-120), TimeSpan.FromSeconds(120)), Is.True,
					$"{now}, {dbTzNow}, {delta}");
			}
		}

		[ActiveIssue("Test is broken")]
		[Test]
		public void CurrentTimestampUtcClientSideParameter(
			[IncludeDataSources(true, TestProvName.AllFirebird, ProviderName.SqlCe)]
			string context)
		{
			using (new DisableBaseline("Server-side date generation test"))
			using (var db = GetDataContext(context))
			{
				var dbUtcNow = db.Select(() => Sql.CurrentTimestampUtc);

				var delta = dbUtcNow - DateTime.UtcNow;
				Assert.That(delta.Between(TimeSpan.FromSeconds(-5), TimeSpan.FromSeconds(5)), Is.True);

				// we don't set kind and rely on provider's behavior
				// Most providers return Unspecified, but at least it shouldn't be Local
				if (context.IsAnyOf(ProviderName.ClickHouseOctonica, ProviderName.ClickHouseClient))
					Assert.That(dbUtcNow.Kind, Is.EqualTo(DateTimeKind.Utc));
				else
					Assert.That(dbUtcNow.Kind, Is.EqualTo(DateTimeKind.Unspecified));
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
				var q = 
					from p in db.Person 
					where p.ID == 1 
					select new { Now = Sql.AsSql(DateTime.Now) };

				Assert.That(q.ToList().First().Now.Year, Is.EqualTo(DateTime.Now.Year));
			}
		}

		[Test]
		public void NullabilityCheck([DataSources(false)] string context)
		{
			using (var db = (TestDataConnection)GetDataContext(context))
			{
#pragma warning disable CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'
				var q =
					from p in db.Person
					where p.ID == 1 && 
					      (
						      DateTime.Now != null  &&
							  DateTime.UtcNow != null &&
							  DateTimeOffset.Now != null &&
							  DateTimeOffset.UtcNow != null &&
							  Sql.CurrentTimestamp != null &&
							  Sql.CurrentTimestampUtc != null &&
							  Sql.CurrentTzTimestamp != null
					      )
					select p;
#pragma warning restore CS8073 // The result of the expression is always the same since a value of this type is never equal to 'null'

				var result = q.ToList();

				Assert.Multiple(() =>
				{
					Assert.That(result, Has.Count.EqualTo(1));
					Assert.That(db.LastQuery, Does.Not.Contain("NULL"));
				});
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
			using (var db = GetDataConnection(context))
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
				var isoWeeks                     = new[] { 52, 52, 52, 53, 1, 1, 1, 1, 1, 1, 2, 2 };
				var sqliteParodyNumbering        = new[] { 52, 52, 52, 53, 0, 0, 0, 0, 0, 0, 1, 1 };
				var isoProperWeeks               = new[] { 52, 52, 52,  1, 1, 1, 1, 1, 1, 1, 2, 2 };
				var usWeeks                      = new[] { 52, 52, 53, 53, 1, 1, 1, 1, 1, 2, 2, 2 };
				var usWeeksZeroBased             = new[] { 51, 51, 52, 52, 0, 0, 0, 0, 0, 1, 1, 1 };
				var muslimWeeks                  = new[] { 52, 53, 53, 53, 1, 1, 1, 1, 2, 2, 2, 2 };
				var primitive                    = new[] { 52, 52, 52, 53, 1, 1, 1, 1, 1, 1, 1, 2 };
				var usWeeksZeroBased_byDayOfYear = new[] { 51, 51, 52, 52, 0, 0, 0, 0, 0, 0, 1, 1 };

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
				else if (usWeeksZeroBased_byDayOfYear.SequenceEqual(results))
				{
					Assert.Pass($"Context {db.DataProvider.Name} uses US 0-based week numbering schema by day of year divided by 7");
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
		[ThrowsForProvider(typeof(LinqToDBException), [TestProvName.AllInformix, TestProvName.AllAccess], ErrorMessage = "The LINQ expression 't.DateTimeValue.Millisecond' could not be converted to SQL.")]
		public void Millisecond([DataSources] string context)
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
		public void TimeOfDay1([DataSources(TestProvName.AllMySqlServer)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select TruncMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
		}

		[Test]
		public void TimeOfDay2([IncludeDataSources(TestProvName.AllMySqlServer, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in Types select RoundMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)),
					from t in db.Types select RoundMilliseconds(Sql.AsSql(t.DateTimeValue.TimeOfDay)));
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
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.DateTimeValue)!.Value.Hour));
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
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(22).Hour));
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
					Assert.That(result, Is.EqualTo(1));
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

					Assert.That(result, Is.EqualTo(1));
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

		private static DateTime Truncate(DateTime date, long resolution)
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
					from t in db.Types select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, part2 - part1, t.DateTimeValue)!.Value.Hour));
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
					from t in db.Types select Sql.AsSql(t.DateTimeValue.AddHours(part1 + part2).Hour));
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
					Assert.That(result, Is.EqualTo(1));
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

					Assert.That(result, Is.EqualTo(1));
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
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalDays));
		}

		[Test]
		public void DateDiffDay(
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[Test]
		public void SubDateHour(
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddHours(100) - t.DateTimeValue).TotalHours));
		}

		[Test]
		public void DateDiffHour(
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.DateTimeValue, t.DateTimeValue.AddHours(100))));
		}

		[ActiveIssue("Devart returns 100 as 99.999...", Configuration = TestProvName.AllOracleDevart)]
		[Test]
		public void SubDateMinute(
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalMinutes));
		}

		[ActiveIssue("Devart returns 100 as 99.999...", Configuration = TestProvName.AllOracleDevart)]
		[Test]
		public void DateDiffMinute(
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100)),
					from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.DateTimeValue, t.DateTimeValue.AddMinutes(100))));
		}

		[ActiveIssue("Devart returns 6000 as 5999.999...", Configuration = TestProvName.AllOracleDevart)]
		[Test]
		public void SubDateSecond(
			[DataSources(TestProvName.AllInformix)]
			string context)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in    Types select           (int)(t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds,
					from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMinutes(100) - t.DateTimeValue).TotalSeconds));
		}

		[ActiveIssue("Devart returns 6000 as 5999.999...", Configuration = TestProvName.AllOracleDevart)]
		[Test]
		public void DateDiffSecond(
			[DataSources(TestProvName.AllInformix)]
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
				TestProvName.AllMySql,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				if (context.Contains(ProviderName.SQLiteMS))
				{
					AreEqual(
						from t in Types select (int)(t.DateTimeValue.AddMilliseconds(2023456789) - t.DateTimeValue).TotalMilliseconds,
						from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMilliseconds(2023456789) - t.DateTimeValue).TotalMilliseconds),
						new CustomIntComparer(1));
				}
				// used type has precision == 1/300 of second
				else if (context.IsAnyOf(TestProvName.AllSybase)
					|| context.IsAnyOf(ProviderName.SqlCe)
					|| context.IsAnyOf(TestProvName.AllSqlServer))
				{
					AreEqual(
						from t in Types select (int)(t.DateTimeValue.AddMilliseconds(2023456789) - t.DateTimeValue).TotalMilliseconds,
						from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMilliseconds(2023456789) - t.DateTimeValue).TotalMilliseconds),
						new CustomIntComparer(3));
				}
				else
				{
					AreEqual(
						from t in Types select (int)(t.DateTimeValue.AddMilliseconds(2023456789) - t.DateTimeValue).TotalMilliseconds,
						from t in db.Types select (int)Sql.AsSql((t.DateTimeValue.AddMilliseconds(2023456789) - t.DateTimeValue).TotalMilliseconds));
				}
			}
		}

		// see SubDateMillisecond comment for SQLite.MS
		[Test]
		public void DateDiffMillisecond(
			[DataSources(
				TestProvName.AllInformix,
				TestProvName.AllMySql,
				TestProvName.AllAccess)]
			string context)
		{
			using (var db = GetDataContext(context))
			{
				if(context.IsAnyOf(ProviderName.SQLiteMS))
				{
					AreEqual(
						from t in Types select Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddMilliseconds(2023456789)),
						from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddMilliseconds(2023456789))),
						new CustomNullableIntComparer(1));
				}
				// used type has precision == 1/300 of second
				else if (context.IsAnyOf(TestProvName.AllSybase)
					|| context.IsAnyOf(ProviderName.SqlCe)
					|| context.IsAnyOf(TestProvName.AllSqlServer))
				{
					AreEqual(
						from t in Types select Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddMilliseconds(2023456789)),
						from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddMilliseconds(2023456789))),
						new CustomNullableIntComparer(3));
				}
				else
				{
					AreEqual(
						from t in Types select Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddMilliseconds(2023456789)),
						from t in db.Types select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.DateTimeValue, t.DateTimeValue.AddMilliseconds(2023456789))));
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
		public void MakeDateTimeParametersMonth([DataSources] string context, [Values(1, 10)] int month)
		{
			using (var db = GetDataContext(context))
				AreEqual(
					from t in from p in    Types select Sql.MakeDateTime(2010 + p.ID, month, 1) select t,
					from t in from p in db.Types select Sql.MakeDateTime(2010 + p.ID, month, 1) select t);
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

		#region DateAddViaTimeSpan

		static TimeSpan?[] TimespansForTest()
		{
			return new TimeSpan?[]
			{
//				TimeSpan.FromMilliseconds(1),
				TimeSpan.FromHours(1),
				TimeSpan.FromMinutes(61),
				TimeSpan.FromMinutes(120),
				TimeSpan.FromSeconds(61),
				TimeSpan.FromHours(24),
				TimeSpan.FromHours(24) + TimeSpan.FromSeconds(1),
				null
			};
		}

		class DateTypes
		{
			[Column(CanBeNull = false, IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(DataType = DataType.DateTime, CanBeNull = false)]
			public DateTime DateTime { get; set; }
			[Column(DataType = DataType.DateTime, CanBeNull = true)]
			public DateTime? DateTimeNullable { get; set; }

			[Column(DataType = DataType.DateTime2, CanBeNull = false)]
			[Column(DataType = DataType.DateTime, CanBeNull = false, Configuration = ProviderName.AccessAceOdbc)]
			[Column(DataType = DataType.DateTime, CanBeNull = false, Configuration = ProviderName.AccessJetOdbc)]
			public DateTime DateTime2 { get; set; }

			[Column(DataType = DataType.DateTime2, CanBeNull = true)]
			[Column(DataType = DataType.DateTime, CanBeNull = true, Configuration = ProviderName.AccessAceOdbc)]
			[Column(DataType = DataType.DateTime, CanBeNull = true, Configuration = ProviderName.AccessJetOdbc)]
			public DateTime? DateTime2Nullable { get; set; }

			public static DateTypes[] Seed()
			{
				return new DateTypes[]
				{
					new DateTypes
					{
						Id = 1,
						DateTime = TestData.DateTime,
						DateTimeNullable = TestData.DateTime,
						DateTime2 = TestData.DateTime,
						DateTime2Nullable = TestData.DateTime,
					},
					new DateTypes
					{
						Id = 2,
						DateTime = TestData.DateTime,
						DateTimeNullable = null,
						DateTime2 = TestData.DateTime,
						DateTime2Nullable = null,
					},
				};
			}
		}

		class DateTypesOffset
		{
			[Column(CanBeNull = false, IsPrimaryKey = true)]
			public int Id { get; set; }

			[Column(DataType = DataType.DateTimeOffset, CanBeNull = false)]
			public DateTimeOffset DateTimeOffset { get; set; }

			[Column(DataType = DataType.DateTimeOffset, CanBeNull = true)]
			public DateTimeOffset? DateTimeOffsetNullable { get; set; }

			public static DateTypesOffset[] Seed()
			{
				return new DateTypesOffset[]
				{
					new DateTypesOffset
					{
						Id = 1,
						DateTimeOffset = TestData.DateTimeOffset,
						DateTimeOffsetNullable = TestData.DateTimeOffset,
					},
					new DateTypesOffset
					{
						Id = 2,
						DateTimeOffset = TestData.DateTimeOffset,
						DateTimeOffsetNullable = null,
					},
				};
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllAccess, TestProvName.AllClickHouse, TestProvName.AllDB2, TestProvName.AllFirebird, TestProvName.AllInformix, TestProvName.AllMySql, TestProvName.AllOracle, TestProvName.AllSapHana, ProviderName.SqlCe, TestProvName.AllSqlServer, TestProvName.AllSybase, TestProvName.AllSQLiteClassic])]
		[Test(Description = "https://github.com/linq2db/linq2db/pull/2718")]
		public void DateTimeAddTimeSpan([DataSources(ProviderName.SQLiteMS)] string context, [ValueSource(nameof(TimespansForTest))] TimeSpan? ts)
		{
			// something wrong with retrieving DateTime values for SQLite
			if (context.StartsWith("SQLite") && context.EndsWith(".LinqService"))
				return;

			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(DateTypes.Seed()))
			{

				var query =
					from t in table
					select new
					{
						Id = t.Id,

						DateTime = t.DateTime + ts,
						DateTimeNullable = t.DateTimeNullable + ts,
						DateTime2 = t.DateTime2 + ts,
						DateTime2Nullable = t.DateTime2Nullable + ts,

						M_DateTime = t.DateTime - ts,
						M_DateTimeNullable = t.DateTimeNullable - ts,
						M_DateTime2 = t.DateTime2 - ts,
						M_DateTime2Nullable = t.DateTime2Nullable - ts,

						C_DateTime = ts == null ? null : t.DateTime + Sql.ToSql(ts),
						C_DateTimeNullable = ts == null ? null : t.DateTimeNullable + Sql.ToSql(ts),
						C_DateTime2 = ts == null ? null : t.DateTime2 + Sql.ToSql(ts),
						C_DateTime2Nullable = ts == null ? null : t.DateTime2Nullable + Sql.ToSql(ts),
					};

				var concated = query.Concat(query);

				AssertQuery(concated);
			}
		}

		[ActiveIssue(Configurations = [TestProvName.AllClickHouse, TestProvName.AllMySql, TestProvName.AllOracle, TestProvName.AllSqlServer])]
		[Test(Description = "https://github.com/linq2db/linq2db/pull/2718")]
		public void DateTimeOffsetAddTimeSpan(
			[DataSources(
				TestProvName.AllAccess,
				TestProvName.AllFirebird,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2005,
				ProviderName.DB2,
				TestProvName.AllInformix,
				TestProvName.AllSapHana,
				TestProvName.AllSybase,
				TestProvName.AllMySqlData, // TODO: mysql.data doesn't support DateTimeOffset
				ProviderName.SqlCe)]
			string context,
			[ValueSource(nameof(TimespansForTest))] TimeSpan? ts)
		{
			using (var db = GetDataContext(context))
			using (var table = db.CreateLocalTable(DateTypesOffset.Seed()))
			{

				var query =
					from t in table
					select new
					{
						Id = t.Id,

						DateTimeOffset = t.DateTimeOffset + ts,
						DateTimeOffsetNullable = t.DateTimeOffsetNullable + ts,

						M_DateTimeOffset = t.DateTimeOffset - ts,
						M_DateTimeOffsetNullable = t.DateTimeOffsetNullable - ts,

						C_DateTimeOffset = t.DateTimeOffset + Sql.ToSql(ts),
						C_DateTimeOffsetNullable = t.DateTimeOffsetNullable + Sql.ToSql(ts),
					};

				var concated = query.Concat(query);

				AssertQuery(concated);
			}
		}

		#endregion

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
						Duration        = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue.AddDays(1)))!.Value,
						HasDuration     = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue.AddDays(1))).HasValue,
						LongestDuration = g.Max(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue.AddDays(1))!.Value),
					},
					from t in db.Types
					group t by t.ID into g
					select new
					{
						ID              = g.Key,
						Count           = g.Count(),
						Duration        = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue.AddDays(1)))!.Value,
						HasDuration     = g.Sum(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue.AddDays(1))).HasValue,
						LongestDuration = g.Max(x => Sql.DateDiff(Sql.DateParts.Millisecond, x.DateTimeValue, x.DateTimeValue.AddDays(1))!.Value),
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

		[Table]
		sealed class Issue2950Table
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DataType = DataType.Date)] public DateTime? Date { get; set; }
			[Column(DataType = DataType.Time)] public TimeSpan? Time { get; set; }

			public static readonly Issue2950Table[] Data =
			[
				new Issue2950Table() { Id = 1 },
				new Issue2950Table() { Id = 2, Date = TestData.Date },
				new Issue2950Table() { Id = 3, Date = TestData.Date, Time = TestData.TimeOfDay },
			];
		}

		[ActiveIssue]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/2950")]
		public void Issue2950Test([IncludeDataSources(true, TestProvName.AllPostgreSQL)] string context)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable(Issue2950Table.Data);

			var query = from x in tb
						where x.Time!.Value.Hours >= 0
						select new
						{
							Output = x.Time!.Value.Hours
						};

			var result = query.ToArray();

			Assert.That(result, Has.Length.EqualTo(1));
			Assert.That(result[0].Output, Is.EqualTo(TestData.TimeOfDay.Hours));
		}
	}
}
