using LinqToDB;
using NUnit.Framework;
using System;
using System.Linq;

namespace Tests.Linq
{
	using System.Runtime.InteropServices;
	using LinqToDB.Mapping;

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
				new Transaction() { TransactionId = 2 , TransactionDate = DateTimeOffset. UtcNow                                                                                   },
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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

		[ActiveIssue(SkipForNonLinqService = true, Details = "Should be fixed in v3")]
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
	}
}
