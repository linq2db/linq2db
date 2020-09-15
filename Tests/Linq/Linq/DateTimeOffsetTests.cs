using LinqToDB;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.Linq
{
	using System.Collections.Generic;
	using System.Runtime.InteropServices;
	using LinqToDB.Mapping;
	using Tests.Model;

	[TestFixture]
	public class DateTimeOffsetTests : TestBase
	{
		private static string GetNepalTzId()
		{
			if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
			{
				// windows TZ ID
				return "Nepal Standard Time";
			}

			// IANA TZ ID
			return "Asia/Kathmandu";
		}

		[Table("Transactions")]
		private class Transaction
		{
			[PrimaryKey] public int            TransactionId   { get; set; }
			[Column]     public DateTimeOffset TransactionDate { get; set; }

			public static Transaction[] Data { get; } = new[]
			{
				new Transaction() { TransactionId = 1 , TransactionDate = DateTimeOffset.Now                                                                                       },
				new Transaction() { TransactionId = 2 , TransactionDate = DateTimeOffset.UtcNow                                                                                    },
				new Transaction() { TransactionId = 3 , TransactionDate = DateTimeOffset.Now.AddYears(1)                                                                           },
				new Transaction() { TransactionId = 4 , TransactionDate = DateTimeOffset.Now.AddYears(-1)                                                                          },
				new Transaction() { TransactionId = 5 , TransactionDate = DateTimeOffset.Now.AddMonths(1)                                                                          },
				new Transaction() { TransactionId = 6 , TransactionDate = DateTimeOffset.Now.AddMonths(-1)                                                                         },
				new Transaction() { TransactionId = 7 , TransactionDate = DateTimeOffset.Now.AddDays(1)                                                                            },
				new Transaction() { TransactionId = 8 , TransactionDate = DateTimeOffset.Now.AddDays(-1)                                                                           },
				new Transaction() { TransactionId = 9 , TransactionDate = DateTimeOffset.Now.AddHours(1)                                                                           },
				new Transaction() { TransactionId = 10, TransactionDate = DateTimeOffset.Now.AddHours(-1)                                                                          },
				new Transaction() { TransactionId = 11, TransactionDate = DateTimeOffset.Now.AddMinutes(1)                                                                         },
				new Transaction() { TransactionId = 12, TransactionDate = DateTimeOffset.Now.AddMinutes(-1)                                                                        },
				new Transaction() { TransactionId = 13, TransactionDate = DateTimeOffset.Now.AddSeconds(1)                                                                         },
				new Transaction() { TransactionId = 14, TransactionDate = DateTimeOffset.Now.AddSeconds(-1)                                                                        },
				new Transaction() { TransactionId = 15, TransactionDate = DateTimeOffset.Now.AddMilliseconds(1)                                                                    },
				new Transaction() { TransactionId = 16, TransactionDate = DateTimeOffset.Now.AddMilliseconds(-1)                                                                   },
				new Transaction() { TransactionId = 17, TransactionDate = DateTimeOffset.Now.AddTicks(1)                                                                           },
				new Transaction() { TransactionId = 18, TransactionDate = DateTimeOffset.Now.AddTicks(-1)                                                                          },
				new Transaction() { TransactionId = 19, TransactionDate = TimeZoneInfo.ConvertTime(DateTimeOffset.Now, TimeZoneInfo.FindSystemTimeZoneById(GetNepalTzId()))        },
				new Transaction() { TransactionId = 20, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero)                                                   },
				new Transaction() { TransactionId = 21, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(10))                                          },
				new Transaction() { TransactionId = 22, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(-10))                                         },
				new Transaction() { TransactionId = 23, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromMinutes(10))                                        },
				new Transaction() { TransactionId = 24, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromMinutes(-10))                                       },
				new Transaction() { TransactionId = 25, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 16, 1, TimeSpan.FromMinutes(15))                                       },
				new Transaction() { TransactionId = 26, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 16, 1, TimeSpan.FromMinutes(-15))                                      }

			};
		}

		#region Group By Tests
		[Test]
		public void GroupByDateTimeOffsetByDateTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual   = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Date)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Date)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByTimeOfDayTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.TimeOfDay)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.TimeOfDay)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByDayTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Day)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Day)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByDayOfWeekTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.DayOfWeek)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.DayOfWeek)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByDayOfYearTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.DayOfYear)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.DayOfYear)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByHourTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Hour)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Hour)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByLocalDateTimeTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.LocalDateTime)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.LocalDateTime)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByMillisecondTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Millisecond)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Millisecond)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByMinuteTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Minute)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Minute)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByMonthTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Month)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Month)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetBySecondTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Second)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Second)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByYearTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.Year)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.Year)
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddDaysTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddDays(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddDays(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddHoursTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddHours(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddHours(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddMillisecondsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddMilliseconds(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddMilliseconds(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddMinutesTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddMinutes(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddMinutes(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddMonthsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddMonths(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddMonths(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddSecondsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddSeconds(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddSeconds(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByAddYearsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			{
				using (db.CreateLocalTable(Transaction.Data))
				{
					var actual = db.GetTable<Transaction>()
						.GroupBy(d => d.TransactionDate.AddYears(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					var expected = Transaction.Data
						.GroupBy(d => d.TransactionDate.AddYears(-1))
						.Select(x => new { x.Key, Count = x.Count() })
						.OrderBy(x => x.Key);

					Assert.That(actual, Is.EqualTo(expected));
				}
			}
		}
		#endregion

		#region DateAdd

		public class CustomNullableDateTimeOffsetComparer : IEqualityComparer<DateTimeOffset?>
		{
			public bool Equals(DateTimeOffset? x, DateTimeOffset? y)
			{
				if (!x.HasValue) return false;
				if (!y.HasValue) return false;
				return x.Value.Between(y.Value.AddMilliseconds(-1), y.Value.AddMilliseconds(1));
			}

			public int GetHashCode(DateTimeOffset? x) => 0;
		}

		public class CustomDateTimeOffsetComparer : IEqualityComparer<DateTimeOffset>
		{
			public bool Equals(DateTimeOffset x, DateTimeOffset y)
			{
				return x.Between(y.AddMilliseconds(-1), y.AddMilliseconds(1));
			}

			public int GetHashCode(DateTimeOffset x) => 0;
		}

		[Test]
		public void DateAddYear([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Year, 11, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 11, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddQuarter([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddMonth([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddDayOfYear([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddDay([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddWeek([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddWeekDay([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddHour([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.TransactionDate)!. Value.Hour,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.TransactionDate))!.Value.Hour);
		}

		[Test]
		public void DateAddMinute([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.TransactionDate)!. Value.Minute,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.TransactionDate))!.Value.Minute);
		}

		[Test]
		public void DateAddSecond([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Second, 41, t.TransactionDate)!. Value.Second,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.TransactionDate))!.Value.Second);
		}

		[Test]
		public void AddYears([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddYears(1). Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddYears(1)).Date);
		}

		[Test]
		public void AddMonths([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddMonths(-2). Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddMonths(-2)).Date);
		}

		[Test]
		public void AddDays([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddDays(5). Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddDays(5)).Date);
		}

		[Test]
		public void AddHours([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddHours(22). Hour,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddHours(22)).Hour);
		}

		[Test]
		public void AddMinutes([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddMinutes(-8). Minute,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddMinutes(-8)).Minute);
		}

		[Test]
		public void AddSeconds([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddSeconds(-35). Second,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddSeconds(-35)).Second);
		}

		#endregion

		#region DateAdd Expression

		[Test]
		public void DateAddYearExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 6;
			var part2 = 5;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Year, 11, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, part1 + part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddQuarterExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 6;
			var part2 = 5;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, part2 - part1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddMonthExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 5;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, part1 - part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddDayOfYearExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 6;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.DayOfYear, 3, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.DayOfYear, part1 - part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddDayExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, part1 + part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddWeekExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, part1 - part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddWeekDayExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.WeekDay, 1, t.TransactionDate)!.Value.Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.WeekDay, part2 - part1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddHourExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.TransactionDate)!.Value.Hour,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, part2 - part1, t.TransactionDate))!.Value.Hour);
		}

		[Test]
		public void DateAddMinuteExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.TransactionDate)!.Value.Minute,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, part1 + part2, t.TransactionDate))!.Value.Minute);
		}

		[Test]
		public void DateAddSecondExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 20;
			var part2 = 21;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateAdd(Sql.DateParts.Second, 41, t.TransactionDate)!.Value.Second,
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, part1 + part2, t.TransactionDate))!.Value.Second);
		}

		[Test]
		public void AddYearsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 5;
			var part2 = 4;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddYears(1).Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddYears(part1 - part2)).Date);
		}

		[Test]
		public void AddMonthsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 4;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddMonths(-2).Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddMonths(part1 - part2)).Date);
		}

		[Test]
		public void AddDaysExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddDays(5).Date,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddDays(part1 + part2)).Date);
		}

		[Test]
		public void AddHoursExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 11;
			var part2 = 11;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddHours(22).Hour,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddHours(part1 + part2)).Hour);
		}

		[Test]
		public void AddMinutesExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 1;
			var part2 = 9;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddMinutes(-8).Minute,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddMinutes(part1 - part2)).Minute);
		}

		[Test]
		public void AddSecondsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)] string context)
		{
			var part1 = 5;
			var part2 = 40;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddSeconds(-35).Second,
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddSeconds(part1 - part2)).Second);
		}

		[Test]
		public void AddMillisecondsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			var part1 = 150;
			var part2 = 76;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           t.TransactionDate.AddMilliseconds(226),
					from t in db.GetTable<Transaction>() select Sql.AsSql(t.TransactionDate.AddMilliseconds(part1 + part2)),
					new CustomDateTimeOffsetComparer());
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           (int)(t.TransactionDate.AddHours(96) - t.TransactionDate).TotalDays,
					from t in db.GetTable<Transaction>() select (int)Sql.AsSql((t.TransactionDate.AddHours(96) - t.TransactionDate).TotalDays));
		}

		[Test]
		public void DateDiffDay(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateDiff(Sql.DateParts.Day, t.TransactionDate, t.TransactionDate.AddHours(96)),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.TransactionDate, t.TransactionDate.AddHours(96))));
		}

		[Test]
		public void SubDateHour(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           (int)(t.TransactionDate.AddHours(100) - t.TransactionDate).TotalHours,
					from t in db.GetTable<Transaction>() select (int)Sql.AsSql((t.TransactionDate.AddHours(100) - t.TransactionDate).TotalHours));
		}

		[Test]
		public void DateDiffHour(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateDiff(Sql.DateParts.Hour, t.TransactionDate, t.TransactionDate.AddHours(100)),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.TransactionDate, t.TransactionDate.AddHours(100))));
		}

		[Test]
		public void SubDateMinute(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           (int)(t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalMinutes,
					from t in db.GetTable<Transaction>() select (int)Sql.AsSql((t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalMinutes));
		}

		[Test]
		public void DateDiffMinute(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateDiff(Sql.DateParts.Minute, t.TransactionDate, t.TransactionDate.AddMinutes(100)),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.TransactionDate, t.TransactionDate.AddMinutes(100))));
		}

		[Test]
		public void SubDateSecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           (int)(t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalSeconds,
					from t in db.GetTable<Transaction>() select (int)Sql.AsSql((t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalSeconds));
		}

		[Test]
		public void DateDiffSecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateDiff(Sql.DateParts.Second, t.TransactionDate, t.TransactionDate.AddMinutes(100)),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.TransactionDate, t.TransactionDate.AddMinutes(100))));
		}

		[Test]
		public void SubDateMillisecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select (int)          (t.TransactionDate.AddSeconds(1) - t.TransactionDate).TotalMilliseconds,
					from t in db.GetTable<Transaction>() select (int)Sql.AsSql((t.TransactionDate.AddSeconds(1) - t.TransactionDate).TotalMilliseconds));
		}

		[Test]
		public void DateDiffMillisecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.Data))
				AreEqual(
					from t in Transaction.Data           select           Sql.DateDiff(Sql.DateParts.Millisecond, t.TransactionDate, t.TransactionDate.AddSeconds(1)),
					from t in db.GetTable<Transaction>() select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.TransactionDate, t.TransactionDate.AddSeconds(1))));
		}

		#endregion

	}
}
