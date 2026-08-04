using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using DataType = LinqToDB.DataType;

namespace Tests.UserTests.Test3993
{
	[TestFixture]
	public class Test3993Tests : TestBase
	{
		[Test]
		public void DatePartLongUsesSubsecondComponents()
		{
			var date       = new DateTime(2026, 1, 2, 3, 4, 5).AddTicks(1_234_567);
			var dateOffset = new DateTimeOffset(date, TimeSpan.Zero);

			Sql.DatePartLong(Sql.DateParts.Microsecond, date).ShouldBe(123_456);
			Sql.DatePartLong(Sql.DateParts.Nanosecond, date).ShouldBe(123_456_700);
			Sql.DatePartLong(Sql.DateParts.Tick, date).ShouldBe(1_234_567);
			Sql.DatePartLong(Sql.DateParts.Microsecond, dateOffset).ShouldBe(123_456);
			Sql.DatePartLong(Sql.DateParts.Nanosecond, dateOffset).ShouldBe(123_456_700);
			Sql.DatePartLong(Sql.DateParts.Tick, dateOffset).ShouldBe(1_234_567);
		}

		[Test]
		public void DateDiffSupportsSubsecondParts()
		{
			var startDate       = new DateTime(2026, 1, 2, 3, 4, 5);
			var endDate         = startDate.AddTicks(123_456);
			var startDateOffset = new DateTimeOffset(startDate, TimeSpan.Zero);
			var endDateOffset   = new DateTimeOffset(endDate,   TimeSpan.Zero);

			Sql.DateDiff(Sql.DateParts.Microsecond, startDate, endDate).ShouldBe(12_345);
			Sql.DateDiff(Sql.DateParts.Nanosecond, startDate, endDate).ShouldBe(12_345_600);
			Sql.DateDiffLong(Sql.DateParts.Microsecond, startDate, endDate).ShouldBe(12_345L);
			Sql.DateDiffLong(Sql.DateParts.Nanosecond, startDate, endDate).ShouldBe(12_345_600L);
			Sql.DateDiff(Sql.DateParts.Microsecond, startDateOffset, endDateOffset).ShouldBe(12_345);
			Sql.DateDiff(Sql.DateParts.Nanosecond, startDateOffset, endDateOffset).ShouldBe(12_345_600);
			Sql.DateDiffLong(Sql.DateParts.Microsecond, startDateOffset, endDateOffset).ShouldBe(12_345L);
			Sql.DateDiffLong(Sql.DateParts.Nanosecond, startDateOffset, endDateOffset).ShouldBe(12_345_600L);
		}

		[Test]
		public void DateDiffLongCountsBoundaries()
		{
			var start = new DateTime(2024, 1, 6, 23, 59, 59, 999).AddTicks(9_999);
			var end   = start.AddTicks(1);

			Sql.DateDiffLong(Sql.DateParts.Week, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Day, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Hour, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Minute, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Second, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Millisecond, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Microsecond, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Tick, start, end).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Nanosecond, start, end).ShouldBe(100);
			Sql.DateDiffLong(Sql.DateParts.Day, end, start).ShouldBe(-1);

			var monthStart = new DateTime(2023, 12, 31, 23, 59, 59, 999).AddTicks(9_999);
			var monthEnd   = monthStart.AddTicks(1);

			Sql.DateDiffLong(Sql.DateParts.Year, monthStart, monthEnd).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Quarter, monthStart, monthEnd).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Month, monthStart, monthEnd).ShouldBe(1);
		}

		[Test]
		public void TickContractUses100NanosecondUnits()
		{
			var secondStart = new DateTime(2024, 1, 2, 3, 4, 5);
			var secondEnd   = secondStart.AddTicks(TimeSpan.TicksPerSecond - 1);
			var offsetStart = new DateTimeOffset(secondStart, TimeSpan.Zero);

			Sql.DatePart(Sql.DateParts.Tick, secondStart).ShouldBe(0);
			Sql.DatePart(Sql.DateParts.Tick, secondEnd).ShouldBe(9_999_999);
			Sql.DatePartLong(Sql.DateParts.Tick, secondStart).ShouldBe(0);
			Sql.DatePartLong(Sql.DateParts.Tick, secondEnd).ShouldBe(9_999_999);
			Sql.DateAdd(Sql.DateParts.Tick, 1, secondEnd).ShouldBe(secondStart.AddSeconds(1));
			Sql.DateAdd(Sql.DateParts.Tick, -1, secondStart).ShouldBe(secondStart.AddTicks(-1));
			Sql.DateAdd(Sql.DateParts.Tick, 1, offsetStart).ShouldBe(offsetStart.AddTicks(1));
			Sql.DateAdd(Sql.DateParts.Tick, -1, offsetStart).ShouldBe(offsetStart.AddTicks(-1));
			Sql.DateDiff(Sql.DateParts.Tick, secondEnd, secondStart.AddSeconds(1)).ShouldBe(1);
			Sql.DateDiff(Sql.DateParts.Tick, secondStart.AddSeconds(1), secondEnd).ShouldBe(-1);
			Sql.DateDiffLong(Sql.DateParts.Tick, secondEnd, secondStart.AddSeconds(1)).ShouldBe(1);
			Sql.DateDiffLong(Sql.DateParts.Tick, secondStart.AddSeconds(1), secondEnd).ShouldBe(-1);
		}

		[Test]
		public void DateDiffLongDateTimeOffsetUsesUtcBoundaries()
		{
			var instant = new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.Zero);
			var sameInstantDifferentOffset = instant.ToOffset(TimeSpan.FromHours(5));

			Sql.DateDiffLong(Sql.DateParts.Tick, instant, sameInstantDifferentOffset).ShouldBe(0);
			Sql.DateDiffLong(Sql.DateParts.Day, instant, sameInstantDifferentOffset).ShouldBe(0);

			var sameLocalDifferentOffset = new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.FromHours(5));
			Sql.DateDiffLong(Sql.DateParts.Hour, instant, sameLocalDifferentOffset).ShouldBe(-5);
		}

		[Test]
		public void DateAddNanosecondsTruncatesTowardZero()
		{
			var value = new DateTime(2024, 1, 2, 3, 4, 5);

			Sql.DateAdd(Sql.DateParts.Nanosecond, 99, value).ShouldBe(value);
			Sql.DateAdd(Sql.DateParts.Nanosecond, 100, value).ShouldBe(value.AddTicks(1));
			Sql.DateAdd(Sql.DateParts.Nanosecond, -99, value).ShouldBe(value);
			Sql.DateAdd(Sql.DateParts.Nanosecond, -101, value).ShouldBe(value.AddTicks(-1));

			var offset = new DateTimeOffset(value, TimeSpan.Zero);
			Sql.DateAdd(Sql.DateParts.Nanosecond, 99, offset).ShouldBe(offset);
			Sql.DateAdd(Sql.DateParts.Nanosecond, 100, offset).ShouldBe(offset.AddTicks(1));
			Sql.DateAdd(Sql.DateParts.Nanosecond, -99, offset).ShouldBe(offset);
			Sql.DateAdd(Sql.DateParts.Nanosecond, -101, offset).ShouldBe(offset.AddTicks(-1));
		}

		[Test]
		public void DateDiffLongNanosecondsChecksOverflow()
		{
			Action action = () => Sql.DateDiffLong(Sql.DateParts.Nanosecond, DateTime.MinValue, DateTime.MaxValue);

			Assert.Throws<OverflowException>(() => action());
		}

		public class Test
		{
			public virtual DateTime? StartDateTime { get; set; }
			public virtual DateTime StartDateTime2 { get; set; }
			public virtual DateTime? EndDateTime { get; set; }
			public virtual TimeSpan? PreNotification { get; set; }
			public virtual TimeSpan? PreNotification2 { get; set; }
			public virtual TimeSpan PreNotification3 { get; set; }
			public virtual DateTime? StrField { get; set; }

			public virtual string? Status { get; set; }
		}

		sealed class DateDiffLongCase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public DateTime Start { get; set; }
			[Column] public DateTime End { get; set; }
		}

		sealed class DateDiffLongOffsetCase
		{
			[PrimaryKey] public int Id { get; set; }
			[Column] public DateTimeOffset Start { get; set; }
			[Column] public DateTimeOffset End { get; set; }
			[Column] public DateTimeOffset? NullableStart { get; set; }
			[Column] public DateTimeOffset? NullableEnd { get; set; }
		}

		sealed class DateAddSubsecondCase
		{
			[PrimaryKey] public int      Id    { get; set; }
			[Column]     public DateTime Start { get; set; }
		}

		sealed class DatePartDateTime64Case
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DataType = DataType.DateTime64, Precision = 7)] public DateTime Value { get; set; }
		}

		static TimeSpan GetDateTimeResolution(string configuration)
		{
			if (configuration.Contains("Sybase", StringComparison.Ordinal)
				|| configuration.Contains("SqlServer.2005", StringComparison.Ordinal)
				|| configuration.Contains("SqlCe", StringComparison.Ordinal))
				return TimeSpan.FromTicks(33_334);
			if (configuration.Contains("Access", StringComparison.Ordinal) || configuration.Contains("SQLite", StringComparison.Ordinal))
				return TimeSpan.FromMilliseconds(1);
			if (configuration.Contains("Firebird", StringComparison.Ordinal))
				return TimeSpan.FromTicks(1_000);
			if (configuration.Contains("Informix", StringComparison.Ordinal))
				return TimeSpan.FromTicks(100);
			if (configuration.Contains("DB2", StringComparison.Ordinal) || configuration.Contains("DuckDB", StringComparison.Ordinal) || configuration.Contains("MariaDB", StringComparison.Ordinal) || configuration.Contains("MySql", StringComparison.Ordinal) || configuration.Contains("Oracle", StringComparison.Ordinal) || configuration.Contains("PostgreSQL", StringComparison.Ordinal) || configuration.Contains("Ydb", StringComparison.Ordinal))
				return TimeSpan.FromTicks(10);

			return TimeSpan.FromTicks(1);
		}

		static TempTable<T> CreateTestTable<T>(IDataContext db, string configuration, T[] items)
			where T : notnull
		{
			// SQL CE doesn't implement DROP TABLE IF EXISTS and its provider ignores the
			// non-throwing drop flag used by the general test helper when the table is absent.
			if (!configuration.Contains("SqlCe", StringComparison.Ordinal))
				return db.CreateLocalTable(items);

			var table = new TempTable<T>(db, new CreateTempTableOptions(
				TableName: $"Issue3993_{Guid.NewGuid():N}",
				TableOptions: TableOptions.CreateIfNotExists));
			if (db is DataConnection)
				table.Copy(items);
			else
				foreach (var item in items)
					db.Insert(item, table.TableName);

			return table;
		}

		[Test]
		public void DateDiffLongRuntimeMatrix(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase,
				TestProvName.AllYdb)] string configuration)
		{
			var boundaryStart = new DateTime(2024, 1, 6, 23, 59, 59, 999);
			var longStart     = new DateTime(1990, 1, 2, 3, 4, 5, 123);
			var source = new[]
			{
				new DateDiffLongCase { Id = 1, Start = boundaryStart,              End = boundaryStart.AddMilliseconds(1) },
				new DateDiffLongCase { Id = 2, Start = longStart,                  End = longStart.AddDays(365).AddMilliseconds(456) },
				new DateDiffLongCase { Id = 3, Start = longStart,                  End = longStart.AddYears(30).AddMilliseconds(456) },
				new DateDiffLongCase { Id = 4, Start = longStart.AddDays(30),      End = longStart },
				new DateDiffLongCase { Id = 5, Start = longStart,                  End = longStart },
				new DateDiffLongCase { Id = 6, Start = longStart.AddTicks(1_234),  End = longStart.AddSeconds(1).AddTicks(1_235_801) },
			};

			using var db    = GetDataContext(configuration, options => options.UseDisableQueryCache(true));
			using var table = CreateTestTable(db, configuration, source);

			var stored = table.OrderBy(row => row.Id).ToArray();
			var parts = new[]
			{
				Sql.DateParts.Year,
				Sql.DateParts.Quarter,
				Sql.DateParts.Month,
				Sql.DateParts.Week,
				Sql.DateParts.Day,
				Sql.DateParts.Hour,
				Sql.DateParts.Minute,
				Sql.DateParts.Second,
				Sql.DateParts.Millisecond,
				Sql.DateParts.Microsecond,
				Sql.DateParts.Tick,
				Sql.DateParts.Nanosecond,
			};

			foreach (var part in parts)
			{
				var query = table
					.OrderBy(row => row.Id)
					.Select(row => new { row.Id, Value = Sql.DateDiffLong(part, row.Start, row.End) });

				var sqlResultExpression = query.GetSelectQuery().Select.Columns.Last().Expression;
				var sqlResultType       = sqlResultExpression.SystemType;
				sqlResultType.ShouldNotBeNull();
				var sqlUnderlyingType = Nullable.GetUnderlyingType(sqlResultType!) ?? sqlResultType;
				new[] { typeof(long), typeof(decimal) }.ShouldContain(sqlUnderlyingType);
				if (configuration.Contains("Access", StringComparison.Ordinal))
					QueryHelper.GetValueConverter(sqlResultExpression).ShouldNotBeNull();
				var actual = query.ToArray();
				actual[0].Value.ShouldNotBeNull();
				actual[0].Value!.Value.GetType().ShouldBe(typeof(long));

				for (var i = 0; i < stored.Length; i++)
				{
					actual[i].Id.ShouldBe(stored[i].Id);
					actual[i].Value.ShouldBe(Sql.DateDiffLong(part, stored[i].Start, stored[i].End), $"{part}, row {stored[i].Id}");
				}
			}
		}

		[Test]
		public void DateDiffLongNanosecondsChecksOverflow(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase)] string configuration)
		{
			using var db    = GetDataContext(configuration);
			using var table = CreateTestTable(db, configuration, new[]
			{
				new DateDiffLongCase { Id = 1, Start = new DateTime(1900, 1, 1), End = new DateTime(2200, 1, 1) },
			});

			Action action = () =>
			{
				_ = table.Select(row => Sql.DateDiffLong(Sql.DateParts.Nanosecond, row.Start, row.End)).Single();
			};

			Assert.Throws<Exception>(() => action());
		}

		[Test]
		public void DateDiffLongDateTimeOffsetRuntimeMatrix(
			[IncludeDataSources(false,
				TestProvName.AllClickHouse,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird4Plus,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2008Plus)] string configuration)
		{
			var instant = new DateTimeOffset(2024, 3, 4, 5, 6, 7, 123, TimeSpan.Zero).AddTicks(4_560);
			var source = new[]
			{
				new DateDiffLongOffsetCase
				{
					Id            = 1,
					Start         = instant,
					End           = instant.ToOffset(TimeSpan.FromHours(5)),
					NullableStart = instant,
					NullableEnd   = instant.ToOffset(TimeSpan.FromHours(-4)),
				},
				new DateDiffLongOffsetCase
				{
					Id            = 2,
					Start         = instant,
					End           = new DateTimeOffset(instant.DateTime, TimeSpan.FromHours(5)),
					NullableStart = null,
					NullableEnd   = instant,
				},
			};

			using var db    = GetDataContext(configuration, options => options.UseDisableQueryCache(true));
			using var table = db.CreateLocalTable(source);

			var stored = table.OrderBy(row => row.Id).ToArray();
			var parts  = new[] { Sql.DateParts.Day, Sql.DateParts.Hour, Sql.DateParts.Tick };

			foreach (var part in parts)
			{
				var actual = table
					.OrderBy(row => row.Id)
					.Select(row => new
					{
						row.Id,
						Required = Sql.DateDiffLong(part, row.Start, row.End),
						Nullable = Sql.DateDiffLong(part, row.NullableStart, row.NullableEnd),
					})
					.ToArray();

				for (var i = 0; i < stored.Length; i++)
				{
					actual[i].Id.ShouldBe(stored[i].Id);
					actual[i].Required.ShouldBe(Sql.DateDiffLong(part, stored[i].Start, stored[i].End), $"{part}, required row {stored[i].Id}");
					actual[i].Nullable.ShouldBe(Sql.DateDiffLong(part, stored[i].NullableStart, stored[i].NullableEnd), $"{part}, nullable row {stored[i].Id}");
				}
			}
		}

		[Test]
		public void DateAddSubsecondRuntimeMatrix(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase,
				TestProvName.AllYdb)] string configuration)
		{
			var start = new DateTime(2024, 1, 2, 3, 4, 5).AddTicks(TimeSpan.TicksPerSecond - 1);
			var source = new DateAddSubsecondCase
			{
				Id    = 1,
				Start = start,
			};

			using var db    = GetDataContext(configuration);
			using var table = CreateTestTable(db, configuration, new[] { source });
			var stored      = table.Single();
			var actual      = table.Select(row => new
			{
				TickPart       = Sql.DatePartLong(Sql.DateParts.Tick, row.Start),
				Nanosecond99   = Sql.DateAdd(Sql.DateParts.Nanosecond, 99, row.Start),
				Nanosecond100  = Sql.DateAdd(Sql.DateParts.Nanosecond, 100, row.Start),
				NanosecondM99  = Sql.DateAdd(Sql.DateParts.Nanosecond, -99, row.Start),
				NanosecondM101 = Sql.DateAdd(Sql.DateParts.Nanosecond, -101, row.Start),
				TickPlusOne    = Sql.DateAdd(Sql.DateParts.Tick, 1, row.Start),
				TickMinusOne   = Sql.DateAdd(Sql.DateParts.Tick, -1, row.Start),
			}).Single();

			actual.TickPart.ShouldBe(Sql.DatePartLong(Sql.DateParts.Tick, stored.Start));
			var resolution = GetDateTimeResolution(configuration);
			actual.Nanosecond99.ShouldNotBeNull();
			actual.Nanosecond100.ShouldNotBeNull();
			actual.NanosecondM99.ShouldNotBeNull();
			actual.NanosecondM101.ShouldNotBeNull();
			actual.TickPlusOne.ShouldNotBeNull();
			actual.TickMinusOne.ShouldNotBeNull();
			actual.Nanosecond99.GetValueOrDefault().ShouldBe(stored.Start, resolution);
			actual.Nanosecond100.GetValueOrDefault().ShouldBe(stored.Start.AddTicks(1), resolution);
			actual.NanosecondM99.GetValueOrDefault().ShouldBe(stored.Start, resolution);
			actual.NanosecondM101.GetValueOrDefault().ShouldBe(stored.Start.AddTicks(-1), resolution);
			actual.TickPlusOne.GetValueOrDefault().ShouldBe(stored.Start.AddTicks(1), resolution);
			actual.TickMinusOne.GetValueOrDefault().ShouldBe(stored.Start.AddTicks(-1), resolution);
		}

		[Test]
		public void ClickHousePreEpochDatePartComponents(
			[IncludeDataSources(TestProvName.AllClickHouse)] string configuration)
		{
			var source = new DatePartDateTime64Case
			{
				Id    = 1,
				Value = new DateTime(1969, 12, 31, 23, 59, 59).AddTicks(1_234_567),
			};

			using var db    = GetDataContext(configuration);
			using var table = db.CreateLocalTable(new[] { source });
			var stored      = table.Single();
			var actual      = table.Select(row => new
			{
				Microsecond = Sql.DatePartLong(Sql.DateParts.Microsecond, row.Value),
				Nanosecond  = Sql.DatePartLong(Sql.DateParts.Nanosecond,  row.Value),
				Tick        = Sql.DatePartLong(Sql.DateParts.Tick,        row.Value),
			}).Single();

			actual.Microsecond.ShouldBe(Sql.DatePartLong(Sql.DateParts.Microsecond, stored.Value));
			actual.Nanosecond.ShouldBe(Sql.DatePartLong(Sql.DateParts.Nanosecond, stored.Value));
			actual.Tick.ShouldBe(Sql.DatePartLong(Sql.DateParts.Tick, stored.Value));
		}

		[Test]
		public void TimeSpanComponentMappings([IncludeDataSources(TestProvName.AllSQLite)] string configuration)
		{
			var mappingSchema = new MappingSchema();
			mappingSchema.AddScalarType(typeof(TimeSpan), DataType.Int64);
			var value = new TimeSpan(1, 2, 3, 4, 5).Add(TimeSpan.FromTicks(67));

			using var db    = GetDataContext(configuration, mappingSchema);
			using var table = db.CreateLocalTable(new[] { new Test { PreNotification3 = value } });

			var result = table
				.Select(row => new
				{
					row.PreNotification3.Days,
					row.PreNotification3.Hours,
					row.PreNotification3.Minutes,
					row.PreNotification3.Seconds,
					row.PreNotification3.Milliseconds,
#if NET7_0_OR_GREATER
					row.PreNotification3.Microseconds,
					row.PreNotification3.Nanoseconds,
#endif
				})
				.Single();

			result.Days.ShouldBe(1);
			result.Hours.ShouldBe(2);
			result.Minutes.ShouldBe(3);
			result.Seconds.ShouldBe(4);
			result.Milliseconds.ShouldBe(5);
#if NET7_0_OR_GREATER
			result.Microseconds.ShouldBe(6);
			result.Nanoseconds.ShouldBe(700);
#endif
		}

		[Test]
		public void DateTimeDifferenceSql(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDB2,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird3Plus,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlCe,
				TestProvName.AllSqlServer,
				TestProvName.AllSybase,
				TestProvName.AllYdb)] string configuration)
		{
			var mappingSchema = new MappingSchema();
			var dataType = configuration.Contains("PostgreSQL") ||
				configuration.Contains("Oracle") ||
				configuration.Contains("Informix")
					? DataType.Interval
					: DataType.Int64;

			mappingSchema.AddScalarType(typeof(TimeSpan), dataType);
			mappingSchema.AddScalarType(typeof(TimeSpan?), dataType);

			using var db = GetDataContext(configuration, mappingSchema);

			var query =
				from row in db.GetTable<Test>()
				let difference = row.EndDateTime - row.StartDateTime
				where difference < TimeSpan.FromHours(5) && difference!.Value.TotalHours < 5
				select new
				{
					Difference        = difference,
					Days              = difference.Value.Days,
					Hours             = difference.Value.Hours,
					TotalHours        = difference.Value.TotalHours,
					TotalMilliseconds = difference.Value.TotalMilliseconds,
				};

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldNotBeNullOrWhiteSpace();

			if (configuration.Contains("DuckDB", StringComparison.Ordinal))
				sql.ShouldContain("date_diff('microsecond'");
			else if (configuration.Contains("Informix", StringComparison.Ordinal))
			{
				sql.ShouldContain("INTERVAL DAY(9) TO DAY");
				sql.ShouldContain("INTERVAL SECOND(9) TO FRACTION(5)");
			}
			else if (configuration.Contains("YDB", StringComparison.Ordinal))
				sql.ShouldContain("DateTime::ToMicroseconds");
		}

		[Test]
		public void SqlServerLargeIntervalAddSql([IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string configuration)
		{
			var mappingSchema = new MappingSchema();
			mappingSchema.AddScalarType(typeof(TimeSpan), DataType.Int64);

			using var db = GetDataContext(configuration, mappingSchema);

			var sql = db.GetTable<Test>()
				.Select(row => row.StartDateTime2 + row.PreNotification3)
				.ToSqlQuery()
				.Sql;

			sql.ShouldContain("DateAdd(day");
			sql.ShouldContain("DateAdd(millisecond");
			sql.ShouldContain("DateAdd(nanosecond");
		}

		[Test]
		public void FirebirdLargeIntervalArithmeticUsesBoundedPieces(
			[IncludeDataSources(false, TestProvName.AllFirebird3Plus)] string configuration)
		{
			var mappingSchema = new MappingSchema();
			mappingSchema.AddScalarType(typeof(TimeSpan), DataType.Int64);

			using var db = GetDataContext(configuration, mappingSchema);

			var addSql = db.GetTable<Test>()
				.Select(row => row.StartDateTime2 + row.PreNotification3)
				.ToSqlQuery()
				.Sql;
			var differenceSql = db.GetTable<Test>()
				.Select(row => row.EndDateTime - row.StartDateTime)
				.ToSqlQuery()
				.Sql;

			addSql.ShouldContain("TRUNC");
			addSql.ShouldContain("BIGINT");
			addSql.ShouldContain("DATEADD(day");
			addSql.ShouldContain("DATEADD(millisecond");
			differenceSql.ShouldContain("DATEDIFF(day");
			differenceSql.ShouldContain("DATEDIFF(millisecond");
			differenceSql.ShouldContain("BIGINT");
		}

		public enum AisleStatus
		{
			Available,
			OutOfOrder,
		}

		public enum StorageShelfStatus
		{
			Available,
		}

		public class AisleDTO
		{
			public Guid Id { get; set; }
			public AisleStatus Status { get; set; }
			public string? Name { get; set; }
		}

		public class StorageShelfDTO
		{
			public Guid Id { get; set; }
			public StorageShelfStatus Status { get; set; }
			public Guid AisleID { get; set; }
			public int HeightClass { get; set; }
		}

		[Test]
		public void TestIssue3993_Test1([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllSapHana)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle") || configuration.Contains("Informix"))
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Interval)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Interval)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
				}
				else
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Int64)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
					ms.AddScalarType(typeof(TimeSpan?), DataType.Int64);
				}

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc,
						StartDateTime2    = TestData.DateTime4Utc,
						EndDateTime      = TestData.DateTime4Utc.AddHours(4),
						PreNotification  = TimeSpan.FromSeconds(20000),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					}
				});

				_ =
				(from t in db.GetTable<Test>()
				 select new
				 {
					 NotificationDateTime5 = t.StartDateTime - t.PreNotification,
				 }).ToList();

				_ = db.GetTable<Test>().ToList();

				_ = db.GetTable<Test>().Where(x => x.StartDateTime2.Year == 2023).ToList();
				_ = db.GetTable<Test>().Where(x => x.StartDateTime2 + TimeSpan.FromMinutes(5) > DateTime.UtcNow).ToList();
				_ = db.GetTable<Test>().Where(x => x.StartDateTime2 + TimeSpan.FromDays(365 * 100) > DateTime.UtcNow).ToList();

				_ =
				(from t in db.GetTable<Test>()
				 select new
				 {
					 t1 = t.PreNotification!.Value.TotalMilliseconds,
					 t2 = t.PreNotification!.Value.TotalSeconds
				 }).Where(x => x.t2 < x.t1).ToList();

				var qry =
				from t in db.GetTable<Test>()
				select new
				{
					StartDateTime         = t.StartDateTime,
					PreNotification       = t.PreNotification,
					NotificationDateTime  = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification!.Value.TotalMilliseconds, t.StartDateTime),
					NotificationDateTime2 = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification2!.Value.TotalMilliseconds, t.StartDateTime),
					NotificationDateTime3 = Sql.DateAdd(Sql.DateParts.Millisecond, -1 * t.PreNotification3.TotalMilliseconds, t.StartDateTime),
					NotificationDateTime4 = t.StartDateTime - t.PreNotification3,
					NotificationDateTime5 = t.StartDateTime - t.PreNotification,
					NotificationDateTime6 = t.StartDateTime + t.PreNotification,
					NotificationDateTime7 = t.StartDateTime2 - t.PreNotification,
					NotificationDateTime8 = t.StartDateTime2 - t.PreNotification3,
					NotificationDateTime9 = t.StartDateTime2 + -t.PreNotification3,
					t.StrField!.Value.Day
				};

				var res = qry.Where(x => x.NotificationDateTime < TestData.DateTime4Utc).ToList();
				Assert.That(res, Has.Count.EqualTo(1));
				var res2 = qry.Where(x => x.NotificationDateTime2 < TestData.DateTime4Utc).ToList();
				Assert.That(res2, Has.Count.EqualTo(1));
				var res3 = qry.Where(x => x.NotificationDateTime4 < TestData.DateTime4Utc).ToList();
				Assert.That(res3, Has.Count.EqualTo(1));
				var res31 = qry.Where(x => x.NotificationDateTime5 < TestData.DateTime4Utc).ToList();
				Assert.That(res31, Has.Count.EqualTo(1));
				var res33 = qry.Where(x => x.NotificationDateTime6 < TestData.DateTime4Utc).ToList();
				Assert.That(res33, Has.Count.EqualTo(0));
				var res22 = qry.Where(x => x.NotificationDateTime7 < TestData.DateTime4Utc).ToList();
				Assert.That(res22, Has.Count.EqualTo(1));
				var res11 = qry.Where(x => x.NotificationDateTime8 < TestData.DateTime4Utc).ToList();
				Assert.That(res11, Has.Count.EqualTo(1));

				var qry4 =
				from t in db.GetTable<Test>()
				select new
				{
					NotificationDateTime4 = t.StartDateTime - t.PreNotification3,
				};

				var res4 = qry4.Where(x => x.NotificationDateTime4 < TestData.DateTimeUtc).ToList();
				Assert.That(res4, Has.Count.EqualTo(1));

				var qry5 =
				from t in db.GetTable<Test>()
				select new
				{
					diff = t.EndDateTime - t.StartDateTime,
				};

				var res6 = qry5.ToList();
				Assert.That(res6, Has.Count.EqualTo(1));
				var res21 = qry5.Select(x => x.diff).ToList();
				var stored = db.GetTable<Test>()
					.Select(t => new { t.StartDateTime, t.EndDateTime })
					.Single();
				Assert.That(res21[0], Is.EqualTo(stored.EndDateTime - stored.StartDateTime));
				var res5 = qry5.Where(x => x.diff < TimeSpan.FromHours(5)).ToList();
				Assert.That(res5, Has.Count.EqualTo(1));
				var res7 = qry5.Where(x => x.diff!.Value.TotalHours < 5).ToList();
				Assert.That(res7, Has.Count.EqualTo(1));
				var res8 = qry5.Where(x => x.diff < TimeSpan.FromHours(2)).ToList();
				Assert.That(res8, Has.Count.EqualTo(0));
				var res9 = qry5.Where(x => x.diff!.Value.TotalHours < 2).ToList();
				Assert.That(res9, Has.Count.EqualTo(0));
			}
			finally
			{
				db?.Dispose();
			}
		}

		[Test]
		public void TestIssue3993_Test2([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllSapHana, TestProvName.AllSybase)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle"))
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Interval)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Interval)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
				}
				else
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Int64)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
				}

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc,
						StartDateTime2    = TestData.DateTime4Utc,
						EndDateTime      = TestData.DateTime4Utc.AddHours(4),
						PreNotification  = TimeSpan.FromSeconds(20000),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					},
					new Test
					{
						StartDateTime    = new DateTime(2023,10,17, 9,40,23),
						StartDateTime2    = TestData.DateTime4Utc,
						EndDateTime      = TestData.DateTime4Utc.AddHours(4),
						PreNotification  = TimeSpan.FromDays(7),
						PreNotification2 = TimeSpan.FromSeconds(20000),
						PreNotification3 = TimeSpan.FromSeconds(20000),
						StrField         = TestData.Date,
					}
				});

				var qryComplex = from t in db.GetTable<Test>()
								 select new
								 {
									 Task = t,
									 NotificationDateTime = t.StartDateTime - t.PreNotification

								 };

				var qryComplexWhere = qryComplex.Where(x => (x.Task.Status != "New" && x.Task.Status != "Completed" && x.NotificationDateTime < DateTime.UtcNow) && (x.Task.StartDateTime!.Value.Date < DateTime.UtcNow.Date)).ToList();
			}
			finally
			{
				db?.Dispose();
			}
		}

		[Test]
		public void TestIssue3993_Test3([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllSapHana, TestProvName.AllSybase)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle"))
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Interval)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Interval)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
				}
				else
				{
					ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
							.Property(e => e.StartDateTime2)
							.Property(e => e.PreNotification)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification2)
								.HasDataType(DataType.Int64)
							.Property(e => e.PreNotification3)
								.HasDataType(DataType.Int64)
							.Property(e => e.StrField)
						.Build()
						.MappingSchema;
					ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
				}

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc,
						PreNotification = TimeSpan.FromSeconds(4 * 60 * 60 + 3 * 60 + 2)
					}
				});

				var qryComplex = from t in db.GetTable<Test>()
								 select new
								 {
									 Task = t,
									 NotificationDateTime = t.StartDateTime - t.PreNotification
								 };

				var lst = qryComplex.First();

				var val = qryComplex.Select(x=>
				new
				{
					StartDateTime = x.Task.StartDateTime,
					PreNotification = x.Task.PreNotification,
					x.NotificationDateTime
				}).First();

				var hour = qryComplex.Where(x => x.NotificationDateTime!.Value.Hour == 13).First();
				var minute = qryComplex.Where(x => x.NotificationDateTime!.Value.Minute == 51).First();
				var second = qryComplex.Where(x => x.NotificationDateTime!.Value.Second >= 52 && x.NotificationDateTime!.Value.Second <= 54).First();
			}
			finally
			{
				db?.Dispose();
			}
		}

		[Test]
		public void TestIssue3993_Test4([IncludeDataSources(TestProvName.AllSqlServer, TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllOracle, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllFirebird3Plus, TestProvName.AllInformix, TestProvName.AllClickHouse, TestProvName.AllDuckDB, TestProvName.AllSapHana, TestProvName.AllSybase)] string configuration)
		{
			MappingSchema ms;
			Model.ITestDataContext? db = null;
			try
			{
				ms = new FluentMappingBuilder()
						.Entity<Test>()
							.HasTableName("Common_Topology_Locations")
							.Property(e => e.StartDateTime)
						.Build()
						.MappingSchema;

				ms.AddScalarType(typeof(TimeSpan), DataType.Int64);

				db = GetDataContext(configuration, ms);

				using var tbl = db.CreateLocalTable(new[]
				{
					new Test
					{
						StartDateTime    = TestData.DateTime4Utc
					}
				});

				var hour =  db.GetTable<Test>().Where(x => x.StartDateTime!.Value.Hour == 13).FirstOrDefault();
			}
			finally
			{
				db?.Dispose();
			}
		}

		public class LanguageDTO
		{
			public string? LanguageID { get; set; }

			public TimeSpan TimeSpan { get; set; }

			public TimeSpan? TimeSpanNull { get; set; }
		}

		[Test]
		public void TestIssue3993_BulkCopy([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllPostgreSQL, TestProvName.AllAccess, TestProvName.AllDuckDB, TestProvName.AllOracle, TestProvName.AllInformix, TestProvName.AllMariaDB, TestProvName.AllMySql, TestProvName.AllYdb)] string configuration)
		{
			var isMySql        = configuration.Contains("MariaDB", StringComparison.Ordinal) || configuration.Contains("MySql", StringComparison.Ordinal);
			var largeValue     = TimeSpan.FromDays(isMySql ? 30 : 2000) + new TimeSpan(4, 3, 2) + TimeSpan.FromTicks(1_234_567);
			var negativeValue  = -TimeSpan.FromDays(30) - TimeSpan.FromTicks(9_999);
			var subsecondValue = TimeSpan.FromTicks(1_234_567);
			var ms = new FluentMappingBuilder()
					.Entity<LanguageDTO>()
						.HasTableName("Common_Language")
						.Property(e => e.LanguageID).IsNullable()
					.Build()
					.MappingSchema;

			if (configuration.Contains("PostgreSQL") || configuration.Contains("Oracle") || configuration.Contains("Informix") || configuration.Contains("Ydb"))
			{
				ms.AddScalarType(typeof(TimeSpan), DataType.Interval);
			}
			else if (configuration.Contains("Access"))
			{
				new FluentMappingBuilder(ms)
					.Entity<LanguageDTO>()
						.Property(e => e.TimeSpan)
							.HasDataType(DataType.Decimal)
							.HasPrecision(18)
							.HasScale(0)
							.HasConversion(value => (decimal)value.Ticks, value => TimeSpan.FromTicks(decimal.ToInt64(value)))
						.Property(e => e.TimeSpanNull)
							.HasDataType(DataType.Decimal)
							.HasPrecision(18)
							.HasScale(0)
							.HasConversion(
								value => value == null ? (decimal?)null : value.Value.Ticks,
								value => value == null ? (TimeSpan?)null : TimeSpan.FromTicks(decimal.ToInt64(value.Value)),
								handlesNulls: true)
					.Build();
			}
			else if (isMySql)
			{
				ms.AddScalarType(typeof(TimeSpan),  DataType.Time);
				ms.AddScalarType(typeof(TimeSpan?), DataType.Time);
			}
			else
			{
				ms.AddScalarType(typeof(TimeSpan), DataType.Int64);
				ms.AddScalarType(typeof(TimeSpan?), DataType.Int64);
			}

			using var db = (DataConnection) GetDataContext(configuration, ms);

			using var tbl = db.CreateLocalTable(new[]
				{
					new LanguageDTO
					{
						LanguageID = "de",
						TimeSpan = largeValue,
						TimeSpanNull = null,
					},

				});

			db.BulkCopy(new BulkCopyOptions { BulkCopyType = BulkCopyType.ProviderSpecific }, new[]
				{
					new LanguageDTO
					{
						LanguageID = "en",
						TimeSpan = negativeValue,
						TimeSpanNull = subsecondValue,
					},
					new LanguageDTO
					{
						LanguageID = "fr",
						TimeSpan = subsecondValue,
						TimeSpanNull = negativeValue,
					},
				});

			var result = tbl.OrderBy(row => row.LanguageID).ToArray();
			result.Length.ShouldBe(3);

			var precision = configuration.Contains("PostgreSQL", StringComparison.Ordinal) || configuration.Contains("DuckDB", StringComparison.Ordinal) || configuration.Contains("Ydb", StringComparison.Ordinal) || configuration.Contains("Oracle", StringComparison.Ordinal) || isMySql
				? TimeSpan.FromTicks(10)
				: configuration.Contains("Informix", StringComparison.Ordinal)
					? TimeSpan.FromTicks(100)
				: TimeSpan.FromTicks(1);

			result[0].LanguageID.ShouldBe("de");
			result[0].TimeSpan.ShouldBe(largeValue, precision);
			result[0].TimeSpanNull.ShouldBeNull();
			result[1].LanguageID.ShouldBe("en");
			result[1].TimeSpan.ShouldBe(negativeValue, precision);
			result[1].TimeSpanNull.ShouldNotBeNull();
			result[1].TimeSpanNull!.Value.ShouldBe(subsecondValue, precision);
			result[2].LanguageID.ShouldBe("fr");
			result[2].TimeSpan.ShouldBe(subsecondValue, precision);
			result[2].TimeSpanNull.ShouldNotBeNull();
			result[2].TimeSpanNull!.Value.ShouldBe(negativeValue, precision);

			var serverMembers = tbl
				.OrderBy(row => row.LanguageID)
				.Select(row => new
				{
					row.TimeSpan.Days,
					row.TimeSpan.Hours,
				})
				.ToArray();
			var serverNullableTicks = tbl
				.Where(row => row.TimeSpanNull != null)
				.OrderBy(row => row.LanguageID)
				.Select(row => new { row.LanguageID, row.TimeSpanNull!.Value.Ticks })
				.ToArray();

			serverMembers[0].Days.ShouldBe(result[0].TimeSpan.Days);
			serverMembers[0].Hours.ShouldBe(result[0].TimeSpan.Hours);
			serverMembers[1].Days.ShouldBe(result[1].TimeSpan.Days);
			serverNullableTicks.Length.ShouldBe(2);
			serverNullableTicks[0].LanguageID.ShouldBe("en");
			serverNullableTicks[0].Ticks.ShouldBe(result[1].TimeSpanNull!.Value.Ticks);
			serverNullableTicks[1].LanguageID.ShouldBe("fr");
			serverNullableTicks[1].Ticks.ShouldBe(result[2].TimeSpanNull!.Value.Ticks);
		}

		[Test]
		public void BrokenWithConvertChangesInQueryHelper([IncludeDataSources(TestProvName.AllSQLite, TestProvName.AllSqlServer)] string context)
		{
			var ms = new FluentMappingBuilder()
				.Entity<AisleDTO>()
					.HasTableName("WMS_Aisle")
					.HasPrimaryKey(x => new { x.Id })
					.Property(x => x.Id).HasColumnName("ID")
					.Property(x => x.Name).HasColumnName("Name").IsNullable()
				.Entity<StorageShelfDTO>()
					.HasTableName("WMS_StorageShelf")
					.HasPrimaryKey(x => new { x.Id })
					.Property(x => x.Id).HasColumnName("ID")
					.Property(x => x.Status).HasColumnName("Status").HasDataType(DataType.Int32)
				.Build() .MappingSchema;

			using var db = GetDataContext(context, ms);

			using var aisle = db.CreateLocalTable<AisleDTO>([new AisleDTO { Id = TestData.Guid1, Name = "Aisle1" }]);
			using var refTable = db.CreateLocalTable<StorageShelfDTO>([new StorageShelfDTO { Id = TestData.Guid2, AisleID = TestData.Guid1, HeightClass = 1 }]);

			var used = (from ss in db.GetTable<StorageShelfDTO>().Where(x => x.Status != StorageShelfStatus.Available)
						join a in db.GetTable<AisleDTO>() on ss.AisleID equals a.Id
						group ss by new {a.Name, ss.HeightClass} into ssPerAisle
						select new {ssPerAisle.Key.Name, hname = "Used_" + ssPerAisle.Key.HeightClass.ToString(), Count = ssPerAisle.Count()}).ToList();
		}
	}
}
