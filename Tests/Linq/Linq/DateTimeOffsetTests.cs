using System;
using System.Linq;
using System.Runtime.InteropServices;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Linq
{
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
		private sealed class Transaction
		{
			[PrimaryKey] public int            TransactionId   { get; set; }
			[Column]     public DateTimeOffset TransactionDate { get; set; }

			public static Transaction[] AllData { get; } = new[]
			{
				new Transaction() { TransactionId = 1 , TransactionDate = TestData.DateTimeOffset                                                                                  },
				new Transaction() { TransactionId = 2 , TransactionDate = TestData.DateTimeOffsetUtc                                                                               },
				new Transaction() { TransactionId = 3 , TransactionDate = TestData.DateTimeOffset.AddYears(1)                                                                      },
				new Transaction() { TransactionId = 4 , TransactionDate = TestData.DateTimeOffset.AddYears(-1)                                                                     },
				new Transaction() { TransactionId = 5 , TransactionDate = TestData.DateTimeOffset.AddMonths(1)                                                                     },
				new Transaction() { TransactionId = 6 , TransactionDate = TestData.DateTimeOffset.AddMonths(-1)                                                                    },
				new Transaction() { TransactionId = 7 , TransactionDate = TestData.DateTimeOffset.AddDays(1)                                                                       },
				new Transaction() { TransactionId = 8 , TransactionDate = TestData.DateTimeOffset.AddDays(-1)                                                                      },
				new Transaction() { TransactionId = 9 , TransactionDate = TestData.DateTimeOffset.AddHours(1)                                                                      },
				new Transaction() { TransactionId = 10, TransactionDate = TestData.DateTimeOffset.AddHours(-1)                                                                     },
				new Transaction() { TransactionId = 11, TransactionDate = TestData.DateTimeOffset.AddMinutes(1)                                                                    },
				new Transaction() { TransactionId = 12, TransactionDate = TestData.DateTimeOffset.AddMinutes(-1)                                                                   },
				new Transaction() { TransactionId = 13, TransactionDate = TestData.DateTimeOffset.AddSeconds(1)                                                                    },
				new Transaction() { TransactionId = 14, TransactionDate = TestData.DateTimeOffset.AddSeconds(-1)                                                                   },
				new Transaction() { TransactionId = 15, TransactionDate = TestData.DateTimeOffset.AddMilliseconds(1)                                                               },
				new Transaction() { TransactionId = 16, TransactionDate = TestData.DateTimeOffset.AddMilliseconds(-1)                                                              },
				new Transaction() { TransactionId = 17, TransactionDate = TestData.DateTimeOffset.AddTicks(1)                                                                      },
				new Transaction() { TransactionId = 18, TransactionDate = TestData.DateTimeOffset.AddTicks(-1)                                                                     },
				new Transaction() { TransactionId = 19, TransactionDate = TimeZoneInfo.ConvertTime(TestData.DateTimeOffset, TimeZoneInfo.FindSystemTimeZoneById(GetNepalTzId()))   },
				new Transaction() { TransactionId = 20, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.Zero)                                                   },
				new Transaction() { TransactionId = 21, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(10))                                          },
				new Transaction() { TransactionId = 22, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromHours(-10))                                         },
				new Transaction() { TransactionId = 23, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromMinutes(10))                                        },
				new Transaction() { TransactionId = 24, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 1, 1, TimeSpan.FromMinutes(-10))                                       },
				new Transaction() { TransactionId = 25, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 16, 1, TimeSpan.FromMinutes(15))                                       },
				new Transaction() { TransactionId = 26, TransactionDate = new DateTimeOffset(2000, 1, 1, 1, 16, 1, TimeSpan.FromMinutes(-15))                                      },
			};

			/* Currently, only SQL Server properly handles DateTimeOffset with full fidelity in both directions.
			 * Other Server behaviors:
			 *  - PostgreSQL:
			 *     - Translates to UTC before transmitting to server. Original TZ lost in translation.
			 *     - Does not keep precision to the Tick level, only to the 100ns level.
			 * ClickHouse: operates with UTC values
			 */
			public static Transaction[] LocalTzData { get; } = AllData
				.Where(t => t.TransactionDate.Offset == TestData.DateTimeOffset.Offset) // only local TZ is accurate
				.Where(t => t.TransactionId.NotIn(17, 18))                              // ignore items w/ 1-tick variance
				.ToArray();

			public static Transaction[] GetDbDataForContext(string context) =>
				context.IsAnyOf(TestProvName.AllSqlServer)
					? AllData
						: context.IsAnyOf(TestProvName.AllClickHouse)
							? TzDataInUtc
							: LocalTzData;

			public static Transaction[] LocalTzDataInUtc { get; } = LocalTzData
				.Select(t => new Transaction { TransactionId = t.TransactionId, TransactionDate = t.TransactionDate.ToLocalTime(), })
				.ToArray();

			public static Transaction[] TzDataInUtc { get; } = LocalTzData
				.Select(t => new Transaction { TransactionId = t.TransactionId, TransactionDate = t.TransactionDate.ToUniversalTime(), })
				.ToArray();

			public static Transaction[] GetTestDataForContext(string context) =>
				context.IsAnyOf(TestProvName.AllSqlServer)
					? AllData
					: context.IsAnyOf(TestProvName.AllClickHouse)
						? TzDataInUtc
						: LocalTzDataInUtc;
		}

		class DateTimeOffsetTable
		{
			[PrimaryKey] public int            TransactionId   { get; set; }
			[Column]     public DateTimeOffset TransactionDate { get; set; }
		}

		[Test]
		public void TestMinMaxValues([IncludeDataSources(TestProvName.AllPostgreSQL)] string context)
		{
			var data = new[]
			{
				new DateTimeOffsetTable { TransactionId = 1, TransactionDate = DateTimeOffset.MinValue },
				new DateTimeOffsetTable { TransactionId = 2, TransactionDate = DateTimeOffset.MaxValue },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			
			AreEqualWithComparer(table, data);
		}

		#region Group By Tests
		// Group by tests are only done for Sql Server due to complexity of db variances in handling TZ
		[Test]
		public void GroupByDateTimeOffsetByDateTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse, TestProvName.AllPostgreSQL)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual   = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Date)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Date)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByTimeOfDayTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.TimeOfDay)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.TimeOfDay)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByDayTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Day)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Day)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByDayOfWeekTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.DayOfWeek)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.DayOfWeek)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByDayOfYearTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.DayOfYear)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.DayOfYear)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByHourTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Hour)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Hour)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByLocalDateTimeTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByMillisecondTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Millisecond)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Millisecond)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByMinuteTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Minute)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Minute)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByMonthTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Month)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Month)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetBySecondTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Second)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Second)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[Test]
		public void GroupByDateTimeOffsetByYearTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.Year)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.Year)
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddDaysTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddDays(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddDays(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddHoursTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddHours(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddHours(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddMillisecondsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddMilliseconds(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddMilliseconds(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddMinutesTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddMinutes(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddMinutes(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddMonthsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddMonths(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddMonths(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddSecondsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddSeconds(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddSeconds(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}

		[ActiveIssue("https://github.com/ClickHouse/ClickHouse/issues/55310", Configuration = ProviderName.ClickHouseMySql)]
		[Test]
		public void GroupByDateTimeOffsetByAddYearsTest([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
			{
				var actual = db.GetTable<Transaction>()
					.GroupBy(d => d.TransactionDate.AddYears(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				var expected = Transaction.GetDbDataForContext(context)
					.GroupBy(d => d.TransactionDate.AddYears(-1))
					.Select(x => new { x.Key, Count = x.Count() })
					.OrderBy(x => x.Key);

				Assert.That(actual, Is.EqualTo(expected));
			}
		}
		#endregion

		#region DateAdd

		[Test]
		public void DateAddYear([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Year, 11, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, 11, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddQuarter([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddMonth([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddDay([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddWeek([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context)  select           Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate)!. Value.Date,
					from t in db.GetTable<Transaction>()              select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddHour([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.TransactionDate)!. Value.Hour,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, 1, t.TransactionDate)!.Value.Hour));
		}

		[Test]
		public void DateAddMinute([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.TransactionDate)!. Value.Minute,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, 5, t.TransactionDate))!.Value.Minute);
		}

		[Test]
		public void DateAddSecond([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Second, 41, t.TransactionDate)!. Value.Second,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, 41, t.TransactionDate))!.Value.Second);
		}

		[Test]
		public void AddYears([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddYears(1). Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddYears(1)).Date);
		}

		[Test]
		public void AddMonths([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddMonths(-2). Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddMonths(-2)).Date);
		}

		[Test]
		public void AddDays([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddDays(5). Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddDays(5)).Date);
		}

		[Test]
		public void AddHours([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddHours(22).Hour,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddHours(22).Hour));
		}

		[Test]
		public void AddMinutes([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddMinutes(-8). Minute,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddMinutes(-8)).Minute);
		}

		[Test]
		public void AddSeconds([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddSeconds(-35). Second,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddSeconds(-35)).Second);
		}

		#endregion

		#region DateAdd Expression

		[Test]
		public void DateAddYearExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 6;
			var part2 = 5;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Year, 11, t.TransactionDate)!.            Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Year, part1 + part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddQuarterExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 6;
			var part2 = 5;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Quarter, -1, t.TransactionDate)!.            Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Quarter, part2 - part1, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddMonthExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 5;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Month, 2, t.TransactionDate)!.             Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Month, part1 - part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddDayExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Day, 5, t.TransactionDate)!.             Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, part1 + part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddWeekExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Week, -1, t.TransactionDate)!.            Value.Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Week, part1 - part2, t.TransactionDate))!.Value.Date);
		}

		[Test]
		public void DateAddHourExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Hour, 1, t.TransactionDate)!.             Value.Hour,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Hour, part2 - part1, t.TransactionDate)!.Value.Hour));
		}

		[Test]
		public void DateAddMinuteExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Minute, 5, t.TransactionDate)!.             Value.Minute,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Minute, part1 + part2, t.TransactionDate))!.Value.Minute);
		}

		[Test]
		public void DateAddSecondExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 20;
			var part2 = 21;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateAdd(Sql.DateParts.Second, 41, t.TransactionDate)!.            Value.Second,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateAdd(Sql.DateParts.Second, part1 + part2, t.TransactionDate))!.Value.Second);
		}

		[Test]
		public void AddYearsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 5;
			var part2 = 4;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddYears(1)             .Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddYears(part1 - part2)).Date);
		}

		[Test]
		public void AddMonthsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 2;
			var part2 = 4;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddMonths(-2)            .Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddMonths(part1 - part2)).Date);
		}

		[Test]
		public void AddDaysExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 2;
			var part2 = 3;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddDays(5)             .Date,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddDays(part1 + part2)).Date);
		}

		[Test]
		public void AddHoursExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 11;
			var part2 = 11;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddHours(22)            .Hour,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddHours(part1 + part2).Hour));
		}

		[Test]
		public void AddMinutesExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 1;
			var part2 = 9;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddMinutes(-8)            .Minute,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddMinutes(part1 - part2)).Minute);
		}

		[Test]
		public void AddSecondsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			var part1 = 5;
			var part2 = 40;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddSeconds(-35)           .Second,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddSeconds(part1 - part2)).Second);
		}

		[Test]
		public void AddMillisecondsExpression([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			var part1 = 150;
			var part2 = 76;

			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           t.TransactionDate.AddMilliseconds(226)           .Millisecond,
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(t.TransactionDate.AddMilliseconds(part1 + part2)).Millisecond);
		}

		#endregion

		#region DateDiff

		[Test]
		public void SubDateDay(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           (int)(t.TransactionDate.AddHours(96) - t.TransactionDate).TotalDays,
					from t in db.GetTable<Transaction>()                 select (int)Sql.AsSql((t.TransactionDate.AddHours(96) - t.TransactionDate).TotalDays));
		}

		[Test]
		public void DateDiffDay(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateDiff(Sql.DateParts.Day, t.TransactionDate, t.TransactionDate.AddHours(96)),
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Day, t.TransactionDate, t.TransactionDate.AddHours(96))));
		}

		[Test]
		public void SubDateHour(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           (int)(t.TransactionDate.AddHours(100) - t.TransactionDate).TotalHours,
					from t in db.GetTable<Transaction>()                 select (int)Sql.AsSql((t.TransactionDate.AddHours(100) - t.TransactionDate).TotalHours));
		}

		[Test]
		public void DateDiffHour(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateDiff(Sql.DateParts.Hour, t.TransactionDate, t.TransactionDate.AddHours(100)),
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Hour, t.TransactionDate, t.TransactionDate.AddHours(100))));
		}

		[Test]
		public void SubDateMinute(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           (int)(t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalMinutes,
					from t in db.GetTable<Transaction>()                 select (int)Sql.AsSql((t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalMinutes));
		}

		[Test]
		public void DateDiffMinute(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateDiff(Sql.DateParts.Minute, t.TransactionDate, t.TransactionDate.AddMinutes(100)),
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Minute, t.TransactionDate, t.TransactionDate.AddMinutes(100))));
		}

		[Test]
		public void SubDateSecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           (int)(t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalSeconds,
					from t in db.GetTable<Transaction>()                 select (int)Sql.AsSql((t.TransactionDate.AddMinutes(100) - t.TransactionDate).TotalSeconds));
		}

		[Test]
		public void DateDiffSecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateDiff(Sql.DateParts.Second, t.TransactionDate, t.TransactionDate.AddMinutes(100)),
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Second, t.TransactionDate, t.TransactionDate.AddMinutes(100))));
		}

		[Test]
		public void SubDateMillisecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select (int)          (t.TransactionDate.AddSeconds(1) - t.TransactionDate).TotalMilliseconds,
					from t in db.GetTable<Transaction>()                 select (int)Sql.AsSql((t.TransactionDate.AddSeconds(1) - t.TransactionDate).TotalMilliseconds));
		}

		[Test]
		public void DateDiffMillisecond(
			[IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)]
			string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) select           Sql.DateDiff(Sql.DateParts.Millisecond, t.TransactionDate, t.TransactionDate.AddSeconds(1)),
					from t in db.GetTable<Transaction>()                 select Sql.AsSql(Sql.DateDiff(Sql.DateParts.Millisecond, t.TransactionDate, t.TransactionDate.AddSeconds(1))));
		}

		#endregion

		#region Issue Tests
		[Test]
		public void Issue2508Test([IncludeDataSources(true, TestProvName.AllSqlServer2008Plus, TestProvName.AllPostgreSQL, TestProvName.AllClickHouse)] string context)
		{
			using (var db = GetDataContext(context))
			using (db.CreateLocalTable(Transaction.GetDbDataForContext(context)))
				AreEqual(
					from t in Transaction.GetTestDataForContext(context) where           t.TransactionDate > TestData.DateTimeOffset.AddMinutes(200).ToUniversalTime() select t.TransactionId,
					from t in db.GetTable<Transaction>()                 where Sql.AsSql(t.TransactionDate > TestData.DateTimeOffset.AddMinutes(200))                  select t.TransactionId);
		}
		#endregion

		#region Issue 1855
		[ActiveIssue(Configurations =
		[
			// caused by difference in how DTO parameter stored into database by provider
			TestProvName.AllSQLiteClassic,
			// for FB we need to map DTO parameters to FbzonedDateTime : https://github.com/FirebirdSQL/NETProvider/issues/1189
			TestProvName.AllFirebird
		])]
		[Test(Description = "https://github.com/linq2db/linq2db/issues/1855")]
		public void Issue1855Test(
			// DateTimeOffset not mapped
			[DataSources(TestProvName.AllFirebirdLess4, TestProvName.AllAccess, TestProvName.AllDB2, TestProvName.AllSybase, ProviderName.SqlCe, TestProvName.AllSapHana, TestProvName.AllInformix, TestProvName.AllSqlServer2005)]
				string context,
			[Values(0, 1, 2, 3)] int testCase)
		{
			using var db = GetDataContext(context);
			using var tb = db.CreateLocalTable<Issue1855Table>();

			var dtoBase = new DateTimeOffset(2019,08,08,08,08,08, TimeSpan.Zero);
			var insert = tb
					.Value(r => r.Id, 1)
					.Value(r => r.SomeDateTimeOffset, dtoBase)
					.Value(r => r.SomeNullableDateTimeOffset, dtoBase);

			insert.Insert();

			insert = tb
					.Value(r => r.Id, 2)
					.Value(r => r.SomeDateTimeOffset, dtoBase);

			insert.Insert();

			var interval = 10;
			var clientSideIn = dtoBase.AddSeconds(interval);
			IQueryable<Issue1855Table> query = tb;

			if (testCase == 2)
			{
				query = query.Where(r => clientSideIn != r.SomeNullableDateTimeOffset);
			}
			else if (testCase == 3)
			{
				query = query.Where(r => clientSideIn != r.SomeDateTimeOffset);
			}
			else
			{
				query = query
					.Where(
						r => Sql.DateAdd(
							Sql.DateParts.Second,
							interval,
							(testCase == 1 ? r.SomeNullableDateTimeOffset : r.SomeDateTimeOffset)) >= clientSideIn);

			}

			var result = query.ToArray();

			Assert.That(result, Has.Length.EqualTo(testCase == 1? 1 : 2));
			if (testCase == 1)
			{
				Assert.That(result[0].Id, Is.EqualTo(1));
			}
		}

		sealed class Issue1855Table
		{
			[PrimaryKey] public int Id { get; set; }
			public DateTimeOffset SomeDateTimeOffset { get; set; }
			public DateTimeOffset? SomeNullableDateTimeOffset { get; set; }
		}

		#endregion
	}
}
