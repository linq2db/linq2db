using System;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests.Issue4308
{
	[TestFixture]
	public class Issue4308Tests : TestBase
	{
		sealed class Test
		{
			[PrimaryKey]
			public int Id { get; set; }

			public DateTime? StartDateTime { get; set; }
			public DateTime? EndDateTime   { get; set; }
			public DateTime  RequiredDateTime { get; set; }
			public TimeSpan? PreNotification { get; set; }
			public TimeSpan  RequiredInterval { get; set; }
		}

		sealed class TimeSpanResult
		{
			public int     Id                { get; set; }
			public int?    Days              { get; set; }
			public int?    Hours             { get; set; }
			public int?    Minutes           { get; set; }
			public int?    Seconds           { get; set; }
			public int?    Milliseconds      { get; set; }
			public long?   Ticks             { get; set; }
			public double? TotalDays         { get; set; }
			public double? TotalHours        { get; set; }
			public double? TotalMinutes      { get; set; }
			public double? TotalSeconds      { get; set; }
			public double? TotalMilliseconds { get; set; }
#if NET7_0_OR_GREATER
			public int?    Microseconds      { get; set; }
			public int?    Nanoseconds       { get; set; }
			public double? TotalMicroseconds { get; set; }
			public double? TotalNanoseconds  { get; set; }
#endif
		}

		sealed class DateTimeOffsetTest
		{
			[PrimaryKey]
			public int Id { get; set; }

			public DateTimeOffset  RequiredStart { get; set; }
			public DateTimeOffset  RequiredEnd   { get; set; }
			public DateTimeOffset? NullableStart { get; set; }
			public DateTimeOffset? NullableEnd   { get; set; }
			public TimeSpan        RequiredInterval { get; set; }
			public TimeSpan?       NullableInterval { get; set; }
		}

		sealed class AccessMaterializationRow
		{
			[PrimaryKey] public int      Id           { get; set; }
			[Column(DataType = DataType.Double)]   public double   DoubleValue  { get; set; }
			[Column(DataType = DataType.Decimal)]  public decimal  DecimalValue { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime DateValue    { get; set; }
			[Column(DataType = DataType.Time)]     public TimeSpan TimeValue    { get; set; }
		}

		sealed class InformixMaterializationRow
		{
			[PrimaryKey] public int      Id            { get; set; }
			[Column(DataType = DataType.VarChar)]  public string   StringValue   { get; set; } = null!;
			[Column(DataType = DataType.Decimal)]  public decimal  DecimalValue  { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime DateValue     { get; set; }
			[Column(DataType = DataType.Interval)] public TimeSpan IntervalValue { get; set; }
		}

		sealed class DurationUnitRow
		{
			[PrimaryKey]
			public int Id { get; set; }

			public DateTime StartDateTime     { get; set; }
			public DateTime EndDateTime       { get; set; }
			public TimeSpan TickDuration      { get; set; }
			public TimeSpan SecondDuration    { get; set; }
			public TimeSpan UndeclaredSeconds { get; set; }
		}

		sealed class InvalidDurationMappingRow
		{
			public TimeSpan Value { get; set; }
		}

		sealed class AttributeDurationMappingRow
		{
			[Column(DataType = DataType.Int64)]
			[Duration(DurationUnit.Second)]
			public TimeSpan Value { get; set; }
		}

		static MappingSchema CreateMappingSchema(string configuration)
		{
			var builder          = new FluentMappingBuilder();
			var storesTicks      = false;
			var durationUnit     = DurationUnit.Native;
			var durationDataType = DataType.Interval;

			if (configuration.Contains("Access", StringComparison.Ordinal))
			{
				durationUnit     = DurationUnit.Tick;
				durationDataType = DataType.Decimal;
			}
			else if (!configuration.Contains("Informix", StringComparison.Ordinal)
				&& !configuration.Contains("Oracle", StringComparison.Ordinal)
				&& !configuration.Contains("PostgreSQL", StringComparison.Ordinal)
				&& !configuration.Contains("Ydb", StringComparison.Ordinal))
			{
				storesTicks      = true;
				durationUnit     = DurationUnit.Tick;
				durationDataType = DataType.Int64;
			}

			var testMapping = builder.Entity<Test>()
				.HasTableName("Common_Topology_Locations")
				.Property(row => row.PreNotification).HasDataType(durationDataType).HasDuration(durationUnit)
				.Property(row => row.RequiredInterval).HasDataType(durationDataType).HasDuration(durationUnit);
			var offsetMapping = builder.Entity<DateTimeOffsetTest>()
				.Property(row => row.RequiredInterval).HasDataType(durationDataType).HasDuration(durationUnit)
				.Property(row => row.NullableInterval).HasDataType(durationDataType).HasDuration(durationUnit);

			if (configuration.Contains("Informix", StringComparison.Ordinal))
			{
				testMapping
					.Property(row => row.PreNotification)
						.HasDataType(DataType.Interval)
						.HasDbType("INTERVAL DAY(9) TO FRACTION(5)")
					.Property(row => row.RequiredInterval)
						.HasDataType(DataType.Interval)
						.HasDbType("INTERVAL DAY(9) TO FRACTION(5)");
			}

			if (storesTicks)
			{
				testMapping
					.Property(row => row.PreNotification)
						.HasConversion(
							value => value == null ? (long?)null : value.Value.Ticks,
							value => value == null ? (TimeSpan?)null : TimeSpan.FromTicks(value.Value),
							handlesNulls: true)
					.Property(row => row.RequiredInterval)
						.HasConversion(value => value.Ticks, value => TimeSpan.FromTicks(value));

				offsetMapping
					.Property(row => row.RequiredInterval)
						.HasConversion(value => value.Ticks, value => TimeSpan.FromTicks(value))
					.Property(row => row.NullableInterval)
						.HasConversion(
							value => value == null ? (long?)null : value.Value.Ticks,
							value => value == null ? (TimeSpan?)null : TimeSpan.FromTicks(value.Value),
							handlesNulls: true);
			}
			else if (durationDataType == DataType.Decimal)
			{
				testMapping
					.Property(row => row.PreNotification)
						.HasPrecision(18)
						.HasScale(0)
						.HasConversion(
							value => value == null ? (decimal?)null : value.Value.Ticks,
							value => value == null ? (TimeSpan?)null : TimeSpan.FromTicks(decimal.ToInt64(value.Value)),
							handlesNulls: true)
					.Property(row => row.RequiredInterval)
						.HasPrecision(18)
						.HasScale(0)
						.HasConversion(value => (decimal)value.Ticks, value => TimeSpan.FromTicks(decimal.ToInt64(value)));

				offsetMapping
					.Property(row => row.RequiredInterval)
						.HasPrecision(18)
						.HasScale(0)
						.HasConversion(value => (decimal)value.Ticks, value => TimeSpan.FromTicks(decimal.ToInt64(value)))
					.Property(row => row.NullableInterval)
						.HasPrecision(18)
						.HasScale(0)
						.HasConversion(
							value => value == null ? (decimal?)null : value.Value.Ticks,
							value => value == null ? (TimeSpan?)null : TimeSpan.FromTicks(decimal.ToInt64(value.Value)),
							handlesNulls: true);
			}

			return builder.Build().MappingSchema;
		}

		static TimeSpan ParseInformixInterval(string value)
		{
			return TimeSpan.Parse(value.Trim().Replace(' ', '.'), CultureInfo.InvariantCulture);
		}

		static TempTable<Test> CreateTestTable(IDataContext db, string configuration, Test[] items)
		{
			// SQL CE doesn't implement DROP TABLE IF EXISTS and its provider ignores the
			// non-throwing drop flag used by the general test helper when the table is absent.
			if (!configuration.Contains("SqlCe", StringComparison.Ordinal))
				return db.CreateLocalTable(items);

			var table = new TempTable<Test>(db, new CreateTempTableOptions(TableOptions: TableOptions.CreateIfNotExists));
			if (db is LinqToDB.Data.DataConnection)
				table.Copy(items);
			else
				foreach (var item in items)
					db.Insert(item, table.TableName);

			return table;
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

		static void AssertDateTime(DateTime? actual, DateTime? expected, TimeSpan resolution)
		{
			if (expected == null)
			{
				actual.ShouldBeNull();
				return;
			}

			actual.ShouldNotBeNull();
			actual.GetValueOrDefault().ShouldBe(expected.Value, resolution);
		}

		static void AssertTimeSpan(TimeSpan? actual, TimeSpan? expected, TimeSpan resolution)
		{
			if (expected == null)
			{
				actual.ShouldBeNull();
				return;
			}

			actual.ShouldNotBeNull();
			actual.GetValueOrDefault().ShouldBe(expected.Value, resolution);
		}

		static long GetMappedTimeSpanResolutionTicks(string configuration)
		{
			if (configuration.Contains("Informix", StringComparison.Ordinal))
				return 100;
			if (configuration.Contains("PostgreSQL", StringComparison.Ordinal)
				|| configuration.Contains("Ydb", StringComparison.Ordinal))
				return 10;

			return 1;
		}

		static void AssertTimeSpanMembers(TimeSpanResult result, TimeSpan expected, long storageResolutionTicks)
		{
			result.Days.ShouldBe(expected.Days);
			result.Hours.ShouldBe(expected.Hours);
			result.Minutes.ShouldBe(expected.Minutes);
			result.Seconds.ShouldBe(expected.Seconds);
			result.Milliseconds.ShouldBe(expected.Milliseconds);
			result.Ticks.ShouldBe(expected.Ticks);
			result.TotalDays.ShouldNotBeNull();
			result.TotalHours.ShouldNotBeNull();
			result.TotalMinutes.ShouldNotBeNull();
			result.TotalSeconds.ShouldNotBeNull();
			result.TotalMilliseconds.ShouldNotBeNull();
			result.TotalDays.Value.ShouldBe(expected.TotalDays, storageResolutionTicks / (double)TimeSpan.TicksPerDay);
			result.TotalHours.Value.ShouldBe(expected.TotalHours, storageResolutionTicks / (double)TimeSpan.TicksPerHour);
			result.TotalMinutes.Value.ShouldBe(expected.TotalMinutes, storageResolutionTicks / (double)TimeSpan.TicksPerMinute);
			result.TotalSeconds.Value.ShouldBe(expected.TotalSeconds, storageResolutionTicks / (double)TimeSpan.TicksPerSecond);
			result.TotalMilliseconds.Value.ShouldBe(expected.TotalMilliseconds, storageResolutionTicks / (double)TimeSpan.TicksPerMillisecond);
#if NET7_0_OR_GREATER
			result.Microseconds.ShouldBe(expected.Microseconds);
			result.Nanoseconds.ShouldBe(expected.Nanoseconds);
			result.TotalMicroseconds.ShouldNotBeNull();
			result.TotalNanoseconds.ShouldNotBeNull();
			result.TotalMicroseconds.Value.ShouldBe(expected.TotalMicroseconds, storageResolutionTicks / 10D);
			result.TotalNanoseconds.Value.ShouldBe(expected.TotalNanoseconds, storageResolutionTicks * 100D);
#endif
		}

		[Test]
		public void DeclaredDurationUnitsNormalizeToTicks(
			[IncludeDataSources(true, TestProvName.AllDuckDB, TestProvName.AllSQLite)] string configuration)
		{
			var mappingSchema = new FluentMappingBuilder()
				.Entity<DurationUnitRow>()
					.Property(row => row.TickDuration)
						.HasDataType(DataType.Int64)
						.HasDuration(DurationUnit.Tick)
						.HasConversion(value => value.Ticks, value => TimeSpan.FromTicks(value))
					.Property(row => row.SecondDuration)
						.HasDataType(DataType.Int64)
						.HasDuration(DurationUnit.Second)
						.HasConversion(
							value => value.Ticks / TimeSpan.TicksPerSecond,
							value => TimeSpan.FromTicks(value * TimeSpan.TicksPerSecond))
					.Property(row => row.UndeclaredSeconds)
						.HasDataType(DataType.Int64)
						.HasConversion(
							value => value.Ticks / TimeSpan.TicksPerSecond,
							value => TimeSpan.FromTicks(value * TimeSpan.TicksPerSecond))
				.Build()
				.MappingSchema;
			var duration = TimeSpan.FromSeconds(90);
			var start    = new DateTime(2024, 2, 3, 4, 5, 6);

			using var db = GetDataContext(configuration, mappingSchema);
			using var table = db.CreateLocalTable(new[]
			{
				new DurationUnitRow
				{
					Id                = 1,
					StartDateTime     = start,
					EndDateTime       = start + duration,
					TickDuration      = duration,
					SecondDuration    = duration,
					UndeclaredSeconds = duration,
				},
			});

			table.Select(row => row.TickDuration).Single().ShouldBe(duration);

			var actual = table.Select(row => new
			{
				TickTicks         = row.TickDuration.Ticks,
				SecondTicks       = row.SecondDuration.Ticks,
				SecondTotal       = row.SecondDuration.TotalSeconds,
				UnitsCompareEqual = row.TickDuration == row.SecondDuration,
				MatchesElapsed     = row.SecondDuration == row.EndDateTime - row.StartDateTime,
				Sum               = row.TickDuration + row.SecondDuration,
				Added             = row.StartDateTime + row.SecondDuration,
			}).Single();

			actual.TickTicks.ShouldBe(duration.Ticks);
			actual.SecondTicks.ShouldBe(duration.Ticks);
			actual.SecondTotal.ShouldBe(duration.TotalSeconds);
			actual.UnitsCompareEqual.ShouldBeTrue();
			actual.MatchesElapsed.ShouldBeTrue();
			actual.Sum.ShouldBe(duration + duration);
			actual.Added.ShouldBe(start + duration);

			var composed = table
				.Select(row => new { Duration = row.TickDuration + row.SecondDuration })
				.AsSubQuery()
				.Select(row => new
				{
					row.Duration.Ticks,
					row.Duration.TotalSeconds,
				})
				.Single();

			composed.Ticks.ShouldBe((duration + duration).Ticks);
			composed.TotalSeconds.ShouldBe((duration + duration).TotalSeconds);

			var projected = table
				.Select(row => new { row.SecondDuration })
				.AsSubQuery()
				.Select(row => new { row.SecondDuration.Ticks, row.SecondDuration.TotalSeconds })
				.Single();
			projected.Ticks.ShouldBe(duration.Ticks);
			projected.TotalSeconds.ShouldBe(duration.TotalSeconds);

			var cte = table
				.Select(row => new { Duration = row.TickDuration + row.SecondDuration })
				.AsCte("duration_cte");
			var fromCte = cte
				.Select(row => new { row.Duration.Ticks, row.Duration.TotalSeconds })
				.Single();
			fromCte.Ticks.ShouldBe((duration + duration).Ticks);
			fromCte.TotalSeconds.ShouldBe((duration + duration).TotalSeconds);

			var setDurations = table.Select(row => row.TickDuration)
				.Concat(table.Select(row => row.SecondDuration));
			var fromSet = setDurations
				.Select(value => new { value.Ticks, value.TotalSeconds })
				.ToArray();
			fromSet.Length.ShouldBe(2);
			fromSet.ShouldAllBe(value => value.Ticks == duration.Ticks && value.TotalSeconds == duration.TotalSeconds);

			Assert.Throws<LinqToDBException>(() => table
				.Where(row => row.UndeclaredSeconds.TotalSeconds > 0)
				.ToArray());
			Assert.Throws<LinqToDBException>(() => table
				.Where(row => row.UndeclaredSeconds == duration)
				.ToArray());
		}

		[Test]
		public void NativeAndNumericDurationUnitsCompose(
			[IncludeDataSources(true, TestProvName.AllDuckDB)] string configuration)
		{
			var mappingSchema = new FluentMappingBuilder()
				.Entity<DurationUnitRow>()
					.Property(row => row.TickDuration)
						.HasDataType(DataType.Interval)
						.HasDuration(DurationUnit.Native)
					.Property(row => row.SecondDuration)
						.HasDataType(DataType.Int64)
						.HasDuration(DurationUnit.Second)
						.HasConversion(
							value => value.Ticks / TimeSpan.TicksPerSecond,
							value => TimeSpan.FromTicks(value * TimeSpan.TicksPerSecond))
				.Build()
				.MappingSchema;
			var duration = TimeSpan.FromSeconds(90);
			var start    = new DateTime(2024, 2, 3, 4, 5, 6);

			using var db = GetDataContext(configuration, mappingSchema);
			using var table = db.CreateLocalTable(new[]
			{
				new DurationUnitRow
				{
					Id             = 1,
					StartDateTime  = start,
					EndDateTime    = start + duration,
					TickDuration   = duration,
					SecondDuration = duration,
				},
			});

			// Declaring Native affects only duration expressions; ordinary materialization keeps the
			// provider's interval mapping and does not force the column through Int64 ticks.
			table.Select(row => row.TickDuration).Single().ShouldBe(duration);

			var actual = table.Select(row => new
			{
				NativeTicks       = row.TickDuration.Ticks,
				NativeTotal       = row.TickDuration.TotalSeconds,
				UnitsCompareEqual = row.TickDuration == row.SecondDuration,
				Sum               = row.TickDuration + row.SecondDuration,
				Added             = row.StartDateTime + row.TickDuration,
			}).Single();

			actual.NativeTicks.ShouldBe(duration.Ticks);
			actual.NativeTotal.ShouldBe(duration.TotalSeconds);
			actual.UnitsCompareEqual.ShouldBeTrue();
			actual.Sum.ShouldBe(duration + duration);
			actual.Added.ShouldBe(start + duration);

			var sql = table.Select(row => row.TickDuration == row.SecondDuration).ToSqlQuery().Sql;
			sql.ShouldContain("date_part");
			sql.ShouldNotContain("epoch(");
		}

		[Test]
		public void DurationMappingRejectsIncompatibleStorage()
		{
			var attributeMapping = new MappingSchema();
			attributeMapping.GetEntityDescriptor(typeof(AttributeDurationMappingRow))
				.Columns.Single()
				.GetDbDataType(true).DataType.ShouldBe(DataType.Int64);

			var nativeNumeric = new FluentMappingBuilder()
				.Entity<InvalidDurationMappingRow>()
					.Property(row => row.Value)
						.HasDataType(DataType.Int64)
						.HasDuration(DurationUnit.Native)
				.Build()
				.MappingSchema;
			var nativeException = Assert.Throws<LinqToDBException>(() => nativeNumeric.GetEntityDescriptor(typeof(InvalidDurationMappingRow)));
			nativeException!.Message.ShouldContain("Native duration mappings require an interval/time database type");

			var unitInterval = new FluentMappingBuilder()
				.Entity<InvalidDurationMappingRow>()
					.Property(row => row.Value)
						.HasDataType(DataType.Interval)
						.HasDuration(DurationUnit.Second)
				.Build()
				.MappingSchema;
			var unitException = Assert.Throws<LinqToDBException>(() => unitInterval.GetEntityDescriptor(typeof(InvalidDurationMappingRow)));
			unitException!.Message.ShouldContain("Explicit duration units require a numeric database type");
		}

		[Test]
		public void UndeclaredTimeSpanKeepsDefaultMapping()
		{
			var mappingSchema = new MappingSchema();
			mappingSchema.GetDataType(typeof(TimeSpan)).Type.DataType.ShouldBe(DataType.Time);
			mappingSchema.GetEntityDescriptor(typeof(DurationUnitRow))
				.Columns.Single(column => column.MemberName == nameof(DurationUnitRow.UndeclaredSeconds))
				.GetDbDataType(true).DataType.ShouldBe(DataType.Time);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4308")]
		public void TimeSpanPropertyAccessIsPreserved(
			[IncludeDataSources(true, TestProvName.AllSqlServer)] string configuration)
		{
			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, new[]
			{
				new Test { Id = 1, PreNotification = TimeSpan.FromSeconds(2000) }
			});

			var result =
				(from t in db.GetTable<Test>()
				 select new
				 {
					 t1 = t.PreNotification!.Value.TotalMilliseconds,
					 t2 = t.PreNotification!.Value.TotalSeconds
				 })
				.Where(x => x.t2 < x.t1)
				.Single();

			result.t1.ShouldBe(2_000_000D);
			result.t2.ShouldBe(2_000D);
		}

		[Test]
		public void TimeSpanPropertyAccessSql(
			[IncludeDataSources(false, TestProvName.AllSqlServer)] string configuration)
		{
			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));

			var query =
				(from t in db.GetTable<Test>()
				 select new
				 {
					 t1 = t.PreNotification!.Value.TotalMilliseconds,
					 t2 = t.PreNotification!.Value.TotalSeconds
				 })
				.Where(x => x.t2 < x.t1);

			var sql = query.ToSqlQuery().Sql;

			sql.ShouldContain("10000000");
			sql.ShouldContain("10000");
			sql.ShouldNotContain("[PreNotification] < [PreNotification]");
		}

		[Test]
		public void TimeSpanMembersMappedAsTicks(
			[IncludeDataSources(
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
			var value = new TimeSpan(1, 2, 3, 4, 5).Add(TimeSpan.FromTicks(67));

			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, new[]
			{
				new Test { Id = 1, RequiredDateTime = new DateTime(2024, 1, 1), PreNotification = value },
				new Test { Id = 2, RequiredDateTime = new DateTime(2024, 1, 1), PreNotification = -value },
				new Test { Id = 3, RequiredDateTime = new DateTime(2024, 1, 1), PreNotification = null }
			});

			var results = table
				.OrderBy(row => row.Id)
				.Select(row => new TimeSpanResult
				{
					Id                = row.Id,
					Days              = (int?)row.PreNotification!.Value.Days,
					Hours             = (int?)row.PreNotification!.Value.Hours,
					Minutes           = (int?)row.PreNotification!.Value.Minutes,
					Seconds           = (int?)row.PreNotification!.Value.Seconds,
					Milliseconds      = (int?)row.PreNotification!.Value.Milliseconds,
					Ticks             = (long?)row.PreNotification!.Value.Ticks,
					TotalDays         = (double?)row.PreNotification!.Value.TotalDays,
					TotalHours        = (double?)row.PreNotification!.Value.TotalHours,
					TotalMinutes      = (double?)row.PreNotification!.Value.TotalMinutes,
					TotalSeconds      = (double?)row.PreNotification!.Value.TotalSeconds,
					TotalMilliseconds = (double?)row.PreNotification!.Value.TotalMilliseconds,
#if NET7_0_OR_GREATER
					Microseconds      = (int?)row.PreNotification!.Value.Microseconds,
					Nanoseconds       = (int?)row.PreNotification!.Value.Nanoseconds,
					TotalMicroseconds = (double?)row.PreNotification!.Value.TotalMicroseconds,
					TotalNanoseconds  = (double?)row.PreNotification!.Value.TotalNanoseconds,
#endif
				})
				.ToArray();

			results.Length.ShouldBe(3);
			var storageResolutionTicks = GetMappedTimeSpanResolutionTicks(configuration);
			AssertTimeSpanMembers(results[0], TimeSpan.FromTicks(results[0].Ticks!.Value), storageResolutionTicks);
			AssertTimeSpanMembers(results[1], TimeSpan.FromTicks(results[1].Ticks!.Value), storageResolutionTicks);
			results[2].Days.ShouldBeNull();
			results[2].Ticks.ShouldBeNull();
		}

		[Test]
		public void MySqlNativeTimeSpanMembers(
			[IncludeDataSources(TestProvName.AllMariaDB, TestProvName.AllMySql)] string configuration)
		{
			var value = new TimeSpan(1, 2, 3, 4, 5).Add(TimeSpan.FromTicks(60));
			var builder = new FluentMappingBuilder();
			builder.MappingSchema.AddScalarType(typeof(TimeSpan),  DataType.Time);
			builder.MappingSchema.AddScalarType(typeof(TimeSpan?), DataType.Time);
			var mappingSchema = builder
				.Entity<Test>()
					.HasTableName("Common_Topology_Locations")
					.Property(row => row.PreNotification).HasDuration(DurationUnit.Native)
				.Build()
				.MappingSchema;

			using var db = GetDataContext(configuration, mappingSchema);
			using var table = CreateTestTable(db, configuration, new[]
			{
				new Test { Id = 1, RequiredDateTime = new DateTime(2024, 1, 1), PreNotification = value },
				new Test { Id = 2, RequiredDateTime = new DateTime(2024, 1, 1), PreNotification = -value },
				new Test { Id = 3, RequiredDateTime = new DateTime(2024, 1, 1), PreNotification = null },
			});

			var stored = table.OrderBy(row => row.Id).ToArray();
			var results = table
				.OrderBy(row => row.Id)
				.Select(row => new TimeSpanResult
				{
					Id                = row.Id,
					Days              = (int?)row.PreNotification!.Value.Days,
					Hours             = (int?)row.PreNotification!.Value.Hours,
					Minutes           = (int?)row.PreNotification!.Value.Minutes,
					Seconds           = (int?)row.PreNotification!.Value.Seconds,
					Milliseconds      = (int?)row.PreNotification!.Value.Milliseconds,
					Ticks             = (long?)row.PreNotification!.Value.Ticks,
					TotalDays         = (double?)row.PreNotification!.Value.TotalDays,
					TotalHours        = (double?)row.PreNotification!.Value.TotalHours,
					TotalMinutes      = (double?)row.PreNotification!.Value.TotalMinutes,
					TotalSeconds      = (double?)row.PreNotification!.Value.TotalSeconds,
					TotalMilliseconds = (double?)row.PreNotification!.Value.TotalMilliseconds,
#if NET7_0_OR_GREATER
					Microseconds      = (int?)row.PreNotification!.Value.Microseconds,
					Nanoseconds       = (int?)row.PreNotification!.Value.Nanoseconds,
					TotalMicroseconds = (double?)row.PreNotification!.Value.TotalMicroseconds,
					TotalNanoseconds  = (double?)row.PreNotification!.Value.TotalNanoseconds,
#endif
				})
				.ToArray();

			for (var i = 0; i < 2; i++)
			{
				var expected = stored[i].PreNotification!.Value;
				AssertTimeSpanMembers(results[i], expected, 10);
			}

			results[2].Days.ShouldBeNull();
			results[2].Ticks.ShouldBeNull();
		}

		[Test]
		public void TimeSpanSignedBoundaryMatrix(
			[IncludeDataSources(
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
			long[] ticks =
			[
				0,
				1, -1,
				9, -9,
				10, -10,
				9_999, -9_999,
				10_000, -10_000,
				TimeSpan.TicksPerSecond - 1, -(TimeSpan.TicksPerSecond - 1),
				TimeSpan.TicksPerDay - 1, -(TimeSpan.TicksPerDay - 1),
				TimeSpan.TicksPerDay, -TimeSpan.TicksPerDay,
				TimeSpan.TicksPerDay + 1, -(TimeSpan.TicksPerDay + 1),
			];

			var date = new DateTime(2024, 1, 2);
			var data = ticks.Select((value, index) => new Test
			{
				Id               = index + 1,
				RequiredDateTime = date,
				PreNotification  = TimeSpan.FromTicks(value),
				RequiredInterval = TimeSpan.FromTicks(value),
			}).ToArray();

			using var db    = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, data);
			var actual = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					row.Id,
					Ticks        = row.PreNotification!.Value.Ticks,
					Days         = row.PreNotification.Value.Days,
					Hours        = row.PreNotification.Value.Hours,
					Minutes      = row.PreNotification.Value.Minutes,
					Seconds      = row.PreNotification.Value.Seconds,
					Milliseconds = row.PreNotification.Value.Milliseconds,
#if NET7_0_OR_GREATER
					Microseconds = row.PreNotification.Value.Microseconds,
					Nanoseconds  = row.PreNotification.Value.Nanoseconds,
#endif
				})
				.ToArray();

			actual.Length.ShouldBe(ticks.Length);
			for (var i = 0; i < actual.Length; i++)
			{
				var expected = TimeSpan.FromTicks(actual[i].Ticks);
				actual[i].Ticks.ShouldBe(expected.Ticks);
				actual[i].Days.ShouldBe(expected.Days);
				actual[i].Hours.ShouldBe(expected.Hours);
				actual[i].Minutes.ShouldBe(expected.Minutes);
				actual[i].Seconds.ShouldBe(expected.Seconds);
				actual[i].Milliseconds.ShouldBe(expected.Milliseconds);
#if NET7_0_OR_GREATER
				actual[i].Microseconds.ShouldBe(expected.Microseconds);
				actual[i].Nanoseconds.ShouldBe(expected.Nanoseconds);
#endif
			}
		}

		[Test]
		public void TimeSpanOperatorsMappedAsTicks(
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
			var start    = new DateTime(2024, 2, 3, 4, 5, 6, 789);
			var interval = new TimeSpan(1, 2, 3, 4, 5);
			var tolerance = GetDateTimeResolution(configuration);

			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, new[]
			{
				new Test
				{
					Id              = 1,
					StartDateTime   = start,
					EndDateTime     = start + interval,
					RequiredDateTime = start,
					PreNotification = interval,
					RequiredInterval = interval,
				}
			});

			var stored = table.Select(row => new
			{
				row.StartDateTime,
				row.EndDateTime,
				row.RequiredDateTime,
			}).Single();
			stored.StartDateTime.ShouldNotBeNull();
			stored.EndDateTime.ShouldNotBeNull();

			var storedStart    = stored.StartDateTime.Value;
			var storedEnd      = stored.EndDateTime.Value;
			var storedRequired = stored.RequiredDateTime;

			var result = table
				.Select(row => new
				{
					Added      = row.StartDateTime + row.PreNotification,
					AddedRequired = row.RequiredDateTime + row.RequiredInterval,
					AddedNullableDate = row.StartDateTime + row.RequiredInterval,
					AddedNullableInterval = row.RequiredDateTime + row.PreNotification,
					Subtracted = row.StartDateTime - row.PreNotification,
					Negated    = -row.PreNotification,
					Difference = row.EndDateTime - row.StartDateTime,
					DifferenceNullableLeft  = row.EndDateTime - row.RequiredDateTime,
					DifferenceNullableRight = row.RequiredDateTime - row.StartDateTime,
				})
				.Single();

			result.Added.ShouldNotBeNull();
			result.Subtracted.ShouldNotBeNull();
			result.Added.Value.ShouldBe(storedStart + interval, tolerance);
			result.AddedRequired.ShouldBe(storedRequired + interval, tolerance);
			result.AddedNullableDate.ShouldNotBeNull();
			result.AddedNullableInterval.ShouldNotBeNull();
			result.AddedNullableDate.Value.ShouldBe(storedStart + interval, tolerance);
			result.AddedNullableInterval.Value.ShouldBe(storedRequired + interval, tolerance);
			result.Subtracted.Value.ShouldBe(storedStart - interval, tolerance);
			result.Negated.ShouldBe(-interval);
			result.Difference.ShouldNotBeNull();
			result.Difference.Value.ShouldBe(storedEnd - storedStart, tolerance);
			result.DifferenceNullableLeft.ShouldNotBeNull();
			result.DifferenceNullableRight.ShouldNotBeNull();
			result.DifferenceNullableLeft.Value.ShouldBe(storedEnd - storedRequired, tolerance);
			result.DifferenceNullableRight.Value.ShouldBe(storedRequired - storedStart, tolerance);
		}

		[Test]
		public void TimeSpanOperatorsWithNullOperands(
			[IncludeDataSources(true,
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
			var start    = new DateTime(2024, 2, 3, 4, 5, 6);
			var interval = TimeSpan.FromMilliseconds(5);

			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, new[]
			{
				new Test
				{
					Id               = 1,
					StartDateTime    = null,
					EndDateTime      = null,
					RequiredDateTime = start,
					PreNotification  = null,
					RequiredInterval = interval,
				}
			});

			var result = table
				.Select(row => new
				{
					Added                   = row.StartDateTime + row.PreNotification,
					AddedRequired           = row.RequiredDateTime + row.RequiredInterval,
					AddedNullableDate       = row.StartDateTime + row.RequiredInterval,
					AddedNullableInterval   = row.RequiredDateTime + row.PreNotification,
					Subtracted              = row.StartDateTime - row.PreNotification,
					Negated                 = -row.PreNotification,
					Difference              = row.EndDateTime - row.StartDateTime,
					DifferenceNullableLeft  = row.EndDateTime - row.RequiredDateTime,
					DifferenceNullableRight = row.RequiredDateTime - row.StartDateTime,
				})
				.Single();

			result.Added.ShouldBeNull();
			result.AddedRequired.ShouldBe(start + interval, GetDateTimeResolution(configuration));
			result.AddedNullableDate.ShouldBeNull();
			result.AddedNullableInterval.ShouldBeNull();
			result.Subtracted.ShouldBeNull();
			result.Negated.ShouldBeNull();
			result.Difference.ShouldBeNull();
			result.DifferenceNullableLeft.ShouldBeNull();
			result.DifferenceNullableRight.ShouldBeNull();
		}

		[Test]
		public void DateTimeNullableOperatorMatrix(
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
			var start    = new DateTime(2024, 2, 3, 4, 5, 6, 789).AddTicks(1_234);
			var interval = TimeSpan.FromSeconds(1.234567);
			var data = new[]
			{
				new Test
				{
					Id                = 1,
					StartDateTime     = start,
					EndDateTime       = start + interval,
					RequiredDateTime  = start,
					PreNotification   = interval,
					RequiredInterval  = interval,
				},
				new Test
				{
					Id                = 2,
					StartDateTime     = null,
					EndDateTime       = null,
					RequiredDateTime  = start,
					PreNotification   = null,
					RequiredInterval  = interval,
				},
			};

			using var db    = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, data);

			var stored = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					row.RequiredDateTime,
					row.StartDateTime,
					row.EndDateTime,
					RequiredIntervalTicks = row.RequiredInterval.Ticks,
					NullableIntervalTicks = row.PreNotification == null
						? (long?)null
						: row.PreNotification.Value.Ticks,
				})
				.ToArray();
			var actual = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					AddRequiredRequired = row.RequiredDateTime + row.RequiredInterval,
					AddRequiredNullable = row.RequiredDateTime + row.PreNotification,
					AddNullableRequired = row.StartDateTime + row.RequiredInterval,
					AddNullableNullable = row.StartDateTime + row.PreNotification,
					SubtractRequiredRequired = row.RequiredDateTime - row.RequiredInterval,
					SubtractRequiredNullable = row.RequiredDateTime - row.PreNotification,
					SubtractNullableRequired = row.StartDateTime - row.RequiredInterval,
					SubtractNullableNullable = row.StartDateTime - row.PreNotification,
					DifferenceRequiredRequired = row.RequiredDateTime - row.RequiredDateTime,
					DifferenceRequiredNullable = row.RequiredDateTime - row.StartDateTime,
					DifferenceNullableRequired = row.EndDateTime - row.RequiredDateTime,
					DifferenceNullableNullable = row.EndDateTime - row.StartDateTime,
				})
				.ToArray();

			var resolution = GetDateTimeResolution(configuration);
			for (var i = 0; i < stored.Length; i++)
			{
				var requiredInterval = TimeSpan.FromTicks(stored[i].RequiredIntervalTicks);
				var nullableInterval = stored[i].NullableIntervalTicks == null
					? (TimeSpan?)null
					: TimeSpan.FromTicks(stored[i].NullableIntervalTicks.GetValueOrDefault());

				AssertDateTime(actual[i].AddRequiredRequired, stored[i].RequiredDateTime + requiredInterval, resolution);
				AssertDateTime(actual[i].AddRequiredNullable, stored[i].RequiredDateTime + nullableInterval, resolution);
				AssertDateTime(actual[i].AddNullableRequired, stored[i].StartDateTime + requiredInterval, resolution);
				AssertDateTime(actual[i].AddNullableNullable, stored[i].StartDateTime + nullableInterval, resolution);
				AssertDateTime(actual[i].SubtractRequiredRequired, stored[i].RequiredDateTime - requiredInterval, resolution);
				AssertDateTime(actual[i].SubtractRequiredNullable, stored[i].RequiredDateTime - nullableInterval, resolution);
				AssertDateTime(actual[i].SubtractNullableRequired, stored[i].StartDateTime - requiredInterval, resolution);
				AssertDateTime(actual[i].SubtractNullableNullable, stored[i].StartDateTime - nullableInterval, resolution);
				AssertTimeSpan(actual[i].DifferenceRequiredRequired, stored[i].RequiredDateTime - stored[i].RequiredDateTime, resolution);
				AssertTimeSpan(actual[i].DifferenceRequiredNullable, stored[i].RequiredDateTime - stored[i].StartDateTime, resolution);
				AssertTimeSpan(actual[i].DifferenceNullableRequired, stored[i].EndDateTime - stored[i].RequiredDateTime, resolution);
				AssertTimeSpan(actual[i].DifferenceNullableNullable, stored[i].EndDateTime - stored[i].StartDateTime, resolution);
			}
		}

		[Test]
		public void DateTimeExpressionShapeMatrix(
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
				TestProvName.AllYdb)] string configuration,
			[Values] bool inlineParameters)
		{
			var start            = new DateTime(2024, 2, 3, 4, 5, 6, 789).AddTicks(1_234);
			var columnInterval   = TimeSpan.FromSeconds(2.345678);
			var capturedInterval = TimeSpan.FromSeconds(1.234567);
			var capturedDate     = new DateTime(2020, 1, 2, 3, 4, 5, 678).AddTicks(9_876);

			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			db.InlineParameters = inlineParameters;
			using var table = CreateTestTable(db, configuration, new[]
			{
				new Test
				{
					Id               = 1,
					StartDateTime    = start,
					EndDateTime      = capturedDate,
					RequiredDateTime = start,
					RequiredInterval = columnInterval,
					PreNotification  = columnInterval,
				},
			});

			var stored = table.Select(row => new
			{
				row.StartDateTime,
				row.EndDateTime,
				row.RequiredDateTime,
				RequiredIntervalTicks = row.RequiredInterval.Ticks,
			}).Single();
			var actual = table.Select(row => new
			{
				ColumnPlusCaptured       = row.RequiredDateTime + capturedInterval,
				ColumnPlusInline         = row.RequiredDateTime + TimeSpan.FromTicks(1_234_567),
				ColumnMinusCaptured      = row.RequiredDateTime - capturedInterval,
				ColumnMinusInline        = row.RequiredDateTime - TimeSpan.FromTicks(1_234_567),
				ColumnMinusColumn        = row.EndDateTime - row.StartDateTime,
				CapturedDatePlusColumn   = capturedDate + row.RequiredInterval,
			}).Single();

			var resolution = GetDateTimeResolution(configuration);
			actual.ColumnPlusCaptured.ShouldBe(stored.RequiredDateTime + capturedInterval, resolution);
			actual.ColumnPlusInline.ShouldBe(stored.RequiredDateTime + TimeSpan.FromTicks(1_234_567), resolution);
			actual.ColumnMinusCaptured.ShouldBe(stored.RequiredDateTime - capturedInterval, resolution);
			actual.ColumnMinusInline.ShouldBe(stored.RequiredDateTime - TimeSpan.FromTicks(1_234_567), resolution);
			AssertTimeSpan(actual.ColumnMinusColumn, stored.EndDateTime - stored.StartDateTime, resolution);
			actual.CapturedDatePlusColumn.ShouldBe(stored.EndDateTime!.Value + TimeSpan.FromTicks(stored.RequiredIntervalTicks), resolution);
		}

		[Test]
		public void DateTimeOperatorRangeAndPrecisionMatrix(
			[IncludeDataSources(true,
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
			var start = new DateTime(2024, 2, 3, 4, 5, 6, 789).AddTicks(1_234);
			TimeSpan[] intervals =
			[
				TimeSpan.FromTicks(1),
				TimeSpan.FromTicks(9),
				TimeSpan.FromTicks(10),
				TimeSpan.FromTicks(9_999),
				TimeSpan.FromTicks(1_234_567),
				TimeSpan.FromSeconds(1.234567),
				TimeSpan.FromDays(25) + TimeSpan.FromTicks(1_234_567),
				TimeSpan.FromDays(-30) - TimeSpan.FromTicks(1_234_567),
				TimeSpan.FromDays(365) + TimeSpan.FromSeconds(1.234567),
				TimeSpan.FromDays(365 * 30) + TimeSpan.FromTicks(1_234_567),
			];

			var rows = intervals.Select((interval, index) => new Test
			{
				Id                = index + 1,
				StartDateTime     = start,
				EndDateTime       = start + interval,
				RequiredDateTime  = start,
				PreNotification   = interval,
				RequiredInterval  = interval,
			}).ToArray();

			using var db    = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = CreateTestTable(db, configuration, rows);

			var stored = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					row.StartDateTime,
					row.EndDateTime,
					IntervalTicks = row.PreNotification!.Value.Ticks,
				})
				.ToArray();
			var actual = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					Added      = row.StartDateTime + row.PreNotification,
					Subtracted = row.StartDateTime - row.PreNotification,
					Difference = row.EndDateTime - row.StartDateTime,
				})
				.ToArray();

			actual.Length.ShouldBe(intervals.Length);
			var tolerance = GetDateTimeResolution(configuration);
			for (var i = 0; i < actual.Length; i++)
			{
				var storedStart    = stored[i].StartDateTime!.Value;
				var storedEnd      = stored[i].EndDateTime!.Value;
				var storedInterval = TimeSpan.FromTicks(stored[i].IntervalTicks);

				actual[i].Added.ShouldNotBeNull();
				actual[i].Subtracted.ShouldNotBeNull();
				actual[i].Difference.ShouldNotBeNull();
				actual[i].Added!.Value.ShouldBe(storedStart + storedInterval, tolerance);
				actual[i].Subtracted!.Value.ShouldBe(storedStart - storedInterval, tolerance);
				actual[i].Difference!.Value.ShouldBe(storedEnd - storedStart, tolerance);
			}
		}

		[Test]
		public void DateTimeOffsetDifferenceUsesStoredInstants(
			[IncludeDataSources(false,
				TestProvName.AllClickHouse,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird4Plus,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2008Plus)] string configuration)
		{
			var instant = new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.Zero);
			var data = new[]
			{
				new DateTimeOffsetTest
				{
					Id            = 1,
					RequiredStart = instant,
					RequiredEnd   = instant.ToOffset(TimeSpan.FromHours(5)),
					NullableStart = instant,
					NullableEnd   = instant.ToOffset(TimeSpan.FromHours(5)),
					RequiredInterval = TimeSpan.FromMinutes(90),
					NullableInterval = TimeSpan.FromMinutes(90),
				},
				new DateTimeOffsetTest
				{
					Id            = 2,
					RequiredStart = instant,
					RequiredEnd   = new DateTimeOffset(2024, 3, 4, 5, 6, 7, TimeSpan.FromHours(5)),
					NullableStart = null,
					NullableEnd   = null,
					RequiredInterval = TimeSpan.FromMinutes(-90),
					NullableInterval = null,
				},
			};

			using var db    = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = db.CreateLocalTable(data);

			var stored = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					row.RequiredStart,
					row.RequiredEnd,
					row.NullableStart,
					row.NullableEnd,
					RequiredIntervalTicks = row.RequiredInterval.Ticks,
					NullableIntervalTicks = row.NullableInterval == null
						? (long?)null
						: row.NullableInterval.Value.Ticks,
				})
				.ToArray();
			var actual = table
				.OrderBy(row => row.Id)
				.Select(row => new
				{
					AddedRequiredRequired = row.RequiredStart + row.RequiredInterval,
					AddedRequiredNullable = row.RequiredStart + row.NullableInterval,
					AddedNullableRequired = row.NullableStart + row.RequiredInterval,
					AddedNullableNullable = row.NullableStart + row.NullableInterval,
					SubtractedRequiredRequired = row.RequiredStart - row.RequiredInterval,
					SubtractedRequiredNullable = row.RequiredStart - row.NullableInterval,
					SubtractedNullableRequired = row.NullableStart - row.RequiredInterval,
					SubtractedNullableNullable = row.NullableStart - row.NullableInterval,
					RequiredRequired = row.RequiredEnd - row.RequiredStart,
					NullableRequired = row.NullableEnd - row.RequiredStart,
					RequiredNullable = row.RequiredEnd - row.NullableStart,
					NullableNullable = row.NullableEnd - row.NullableStart,
				})
				.ToArray();

			for (var i = 0; i < stored.Length; i++)
			{
				var requiredInterval = TimeSpan.FromTicks(stored[i].RequiredIntervalTicks);
				var nullableInterval = stored[i].NullableIntervalTicks == null
					? (TimeSpan?)null
					: TimeSpan.FromTicks(stored[i].NullableIntervalTicks.GetValueOrDefault());

				actual[i].AddedRequiredRequired.ShouldBe(stored[i].RequiredStart + requiredInterval);
				actual[i].AddedRequiredNullable.ShouldBe(stored[i].RequiredStart + nullableInterval);
				actual[i].AddedNullableRequired.ShouldBe(stored[i].NullableStart + requiredInterval);
				actual[i].AddedNullableNullable.ShouldBe(stored[i].NullableStart + nullableInterval);
				actual[i].SubtractedRequiredRequired.ShouldBe(stored[i].RequiredStart - requiredInterval);
				actual[i].SubtractedRequiredNullable.ShouldBe(stored[i].RequiredStart - nullableInterval);
				actual[i].SubtractedNullableRequired.ShouldBe(stored[i].NullableStart - requiredInterval);
				actual[i].SubtractedNullableNullable.ShouldBe(stored[i].NullableStart - nullableInterval);
				actual[i].RequiredRequired.ShouldBe(stored[i].RequiredEnd - stored[i].RequiredStart);
				actual[i].NullableRequired.ShouldBe(stored[i].NullableEnd - stored[i].RequiredStart);
				actual[i].RequiredNullable.ShouldBe(stored[i].RequiredEnd - stored[i].NullableStart);
				actual[i].NullableNullable.ShouldBe(stored[i].NullableEnd - stored[i].NullableStart);
			}
		}

		[Test]
		public void DateTimeOffsetArithmeticIsRejectedWhenProviderLosesOffset(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllDB2,
				TestProvName.AllFirebirdLess4,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllSapHana,
				TestProvName.AllSqlCe,
				TestProvName.AllSybase,
				TestProvName.AllYdb)] string configuration)
		{
			using var db = GetDataContext(configuration);

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => row.RequiredEnd - row.RequiredStart)
				.ToSqlQuery());

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => Sql.DateDiffLong(Sql.DateParts.Tick, row.RequiredStart, row.RequiredEnd))
				.ToSqlQuery());
		}

		[Test]
		public void DateTimeOffsetArithmeticIsRejectedForOffsetLosingMappedType(
			[IncludeDataSources(false,
				TestProvName.AllClickHouse,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird4Plus,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2008Plus)] string configuration)
		{
			var offsetLosingType = configuration.Contains("ClickHouse", StringComparison.Ordinal)
				? DataType.Date
				: DataType.DateTime2;
			var mappingSchema = new FluentMappingBuilder()
				.Entity<DateTimeOffsetTest>()
					.Property(row => row.RequiredStart).HasDataType(DataType.DateTimeOffset)
					.Property(row => row.RequiredEnd).HasDataType(offsetLosingType)
					.Property(row => row.RequiredInterval).HasDataType(DataType.Int64).HasDuration(DurationUnit.Tick)
				.Build()
				.MappingSchema;
			var descriptor = mappingSchema.GetEntityDescriptor(typeof(DateTimeOffsetTest));
			descriptor.Columns.Single(column => column.MemberName == nameof(DateTimeOffsetTest.RequiredStart))
				.GetDbDataType(true).DataType.ShouldBe(DataType.DateTimeOffset);
			descriptor.Columns.Single(column => column.MemberName == nameof(DateTimeOffsetTest.RequiredEnd))
				.GetDbDataType(true).DataType.ShouldBe(offsetLosingType);

			using var db = GetDataContext(configuration, mappingSchema);
			db.GetTable<DateTimeOffsetTest>()
				.Select(row => row.RequiredStart + row.RequiredInterval)
				.ToSqlQuery()
				.Sql.ShouldNotBeNullOrWhiteSpace();

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => row.RequiredEnd + row.RequiredInterval)
				.ToSqlQuery());

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => row.RequiredEnd - row.RequiredStart)
				.ToSqlQuery());

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => row.RequiredStart - row.RequiredEnd)
				.ToSqlQuery());

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => Sql.DateDiffLong(Sql.DateParts.Tick, row.RequiredStart, row.RequiredEnd))
				.ToSqlQuery());

			Assert.Throws<LinqToDBException>(() => db.GetTable<DateTimeOffsetTest>()
				.Select(row => Sql.DateDiffLong(Sql.DateParts.Tick, row.RequiredEnd, row.RequiredStart))
				.ToSqlQuery());
		}

		[Test]
		public void SqlServerDateTimeOffsetArithmeticUsesUtc(
			[IncludeDataSources(false, TestProvName.AllSqlServer2008Plus)] string configuration)
		{
			using var db = GetDataContext(configuration);

			var sql = db.GetTable<DateTimeOffsetTest>()
				.Select(row => row.RequiredEnd - row.RequiredStart)
				.ToSqlQuery()
				.Sql;

			sql.ShouldContain("SWITCHOFFSET");
			sql.ShouldContain("DATEDIFF(nanosecond");
		}

		[Test]
		public void AccessScopedConvertersDoNotAffectOrdinaryMaterialization(
			[IncludeDataSources(TestProvName.AllAccess)] string configuration)
		{
			var source = new AccessMaterializationRow
			{
				Id           = 1,
				DoubleValue  = 1234.5,
				DecimalValue = 9876.25m,
				DateValue    = new DateTime(2024, 2, 3, 4, 5, 6, 789),
				TimeValue    = new TimeSpan(0, 12, 34, 56),
			};

			using var db    = GetDataContext(configuration);
			using var table = db.CreateLocalTable(new[] { source });
			var actual      = table.Single();

			actual.DoubleValue.ShouldBe(source.DoubleValue);
			actual.DecimalValue.ShouldBe(source.DecimalValue);
			var expectedDate = configuration.Contains("Odbc", StringComparison.Ordinal)
				? source.DateValue.AddTicks(-(source.DateValue.Ticks % TimeSpan.TicksPerSecond))
				: source.DateValue;
			actual.DateValue.ShouldBe(expectedDate);
			actual.TimeValue.ShouldBe(source.TimeValue);
		}

		[Test]
		public void InformixScopedConverterDoesNotAffectOrdinaryMaterialization(
			[IncludeDataSources(TestProvName.AllInformix)] string configuration)
		{
			var source = new InformixMaterializationRow
			{
				Id            = 1,
				StringValue   = "12 03:04:05.67890",
				DecimalValue  = 9876.25m,
				DateValue     = new DateTime(2024, 2, 3, 4, 5, 6, 789),
				IntervalValue = new TimeSpan(12, 3, 4, 5, 678).Add(TimeSpan.FromTicks(9_000)),
			};

			var mappingSchema = new FluentMappingBuilder()
				.Entity<InformixMaterializationRow>()
					.Property(row => row.IntervalValue)
						.HasDataType(DataType.Interval)
						.HasDbType("INTERVAL DAY(9) TO FRACTION(5)")
				.Build()
				.MappingSchema;

			using var db    = GetDataContext(configuration, mappingSchema);
			using var table = db.CreateLocalTable(new[] { source });
			var actual      = table.Select(row => new
			{
				row.StringValue,
				row.DecimalValue,
				row.DateValue,
				IntervalText = Sql.ConvertTo<string>.From(row.IntervalValue),
			}).Single();

			actual.StringValue.ShouldBe(source.StringValue);
			actual.DecimalValue.ShouldBe(source.DecimalValue);
			actual.DateValue.ShouldBe(source.DateValue, TimeSpan.FromTicks(100));
			ParseInformixInterval(actual.IntervalText).ShouldBe(source.IntervalValue, TimeSpan.FromTicks(100));
		}

		[Test]
		public void TimeSpanOperatorsSql(
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
			var mappingSchema = CreateMappingSchema(configuration);
			var expectedType  = configuration.Contains("Access", StringComparison.Ordinal)
				? DataType.Decimal
				: configuration.Contains("Informix", StringComparison.Ordinal)
					|| configuration.Contains("Oracle", StringComparison.Ordinal)
					|| configuration.Contains("PostgreSQL", StringComparison.Ordinal)
					|| configuration.Contains("Ydb", StringComparison.Ordinal)
						? DataType.Interval
						: DataType.Int64;
			var descriptor    = mappingSchema.GetEntityDescriptor(typeof(Test));

			descriptor.Columns.Single(column => column.MemberName == nameof(Test.PreNotification))
				.GetDbDataType(true).DataType.ShouldBe(expectedType);
			descriptor.Columns.Single(column => column.MemberName == nameof(Test.RequiredInterval))
				.GetDbDataType(true).DataType.ShouldBe(expectedType);

			using var db = GetDataContext(configuration, mappingSchema);

			var query = db.GetTable<Test>()
				.Select(row => new
				{
					Added                  = row.StartDateTime + row.PreNotification,
					AddedRequired          = row.RequiredDateTime + row.RequiredInterval,
					AddedNullableDate      = row.StartDateTime + row.RequiredInterval,
					AddedNullableInterval  = row.RequiredDateTime + row.PreNotification,
					Subtracted             = row.StartDateTime - row.PreNotification,
					Negated                = -row.PreNotification,
					Difference             = row.EndDateTime - row.StartDateTime,
					DifferenceNullableLeft = row.EndDateTime - row.RequiredDateTime,
					DifferenceNullableRight = row.RequiredDateTime - row.StartDateTime,
				});

			var sql = query.ToSqlQuery().Sql;
			sql.ShouldNotBeNullOrWhiteSpace();

			if (configuration.Contains("Access", StringComparison.Ordinal))
			{
				sql.ShouldContain("CDbl(DateAdd(");
				sql.ShouldContain("CVar(IIf(CDbl(");
				sql.ShouldContain("864000000000");
				sql.ShouldNotContain("CDate(");
				sql.ShouldNotContain("CDec(");
				sql.ShouldNotContain("DATEDIFF");
				sql.ShouldNotContain("BigInt");
			}
			else if (configuration.Contains("Firebird", StringComparison.Ordinal))
			{
				sql.ShouldContain("DateAdd(Millisecond");
				sql.ShouldNotContain("DateAdd(Tick");
			}
			else if (!configuration.Contains("ClickHouse", StringComparison.Ordinal)
				&& (configuration.Contains("MariaDB", StringComparison.Ordinal) || configuration.Contains("MySql", StringComparison.Ordinal)))
			{
				sql.ShouldContain("Microsecond");
				sql.ShouldNotContain("Millisecond");
			}
			else if (configuration.Contains("Oracle", StringComparison.Ordinal))
			{
				// Native interval columns are used directly, without an artificial cast.
				// If a cast is needed elsewhere, it must not claim a seventh fractional digit.
				sql.ShouldNotContain("SECOND(1,7)");
			}
			else if (configuration.Contains("Sybase", StringComparison.Ordinal))
			{
				sql.ShouldContain("DateDiff(day");
				sql.ShouldContain("BigInt");
			}
		}
	}
}
