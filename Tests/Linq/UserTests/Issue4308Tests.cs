using System;
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

		static MappingSchema CreateMappingSchema(string configuration)
		{
			var builder = new FluentMappingBuilder();

			if (configuration.Contains("Access", StringComparison.Ordinal))
			{
				builder.MappingSchema.AddScalarType(typeof(TimeSpan),  new SqlDataType(DataType.Decimal, typeof(TimeSpan),  18, 0));
				builder.MappingSchema.AddScalarType(typeof(TimeSpan?), new SqlDataType(DataType.Decimal, typeof(TimeSpan?), 18, 0));
			}
			else
			{
				builder.MappingSchema.AddScalarType(typeof(TimeSpan),  DataType.Int64);
				builder.MappingSchema.AddScalarType(typeof(TimeSpan?), DataType.Int64);
			}

			return builder
				.Entity<Test>()
					.HasTableName("Common_Topology_Locations")
				.Build()
				.MappingSchema;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4308")]
		public void TimeSpanPropertyAccessIsPreserved(
			[IncludeDataSources(true, TestProvName.AllSqlServer2016Plus)] string configuration)
		{
			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = db.CreateLocalTable(new[]
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
			[IncludeDataSources(false, TestProvName.AllSqlServer2016Plus)] string configuration)
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
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird3Plus,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2016Plus,
				TestProvName.AllSybase)] string configuration)
		{
			var value = new TimeSpan(1, 2, 3, 4, 5).Add(TimeSpan.FromTicks(67));

			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = db.CreateLocalTable(new[]
			{
				new Test { Id = 1, PreNotification = value },
				new Test { Id = 2, PreNotification = -value },
				new Test { Id = 3, PreNotification = null }
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
			AssertTimeSpan(results[0], value);
			AssertTimeSpan(results[1], -value);
			results[2].Days.ShouldBeNull();
			results[2].Ticks.ShouldBeNull();

			static void AssertTimeSpan(TimeSpanResult result, TimeSpan expected)
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
				result.TotalDays.Value.ShouldBe(expected.TotalDays, 0.000000001D);
				result.TotalHours.Value.ShouldBe(expected.TotalHours, 0.000000001D);
				result.TotalMinutes.Value.ShouldBe(expected.TotalMinutes, 0.000000001D);
				result.TotalSeconds.Value.ShouldBe(expected.TotalSeconds, 0.000000001D);
				result.TotalMilliseconds.Value.ShouldBe(expected.TotalMilliseconds, 0.000001D);
#if NET7_0_OR_GREATER
				result.Microseconds.ShouldBe(expected.Microseconds);
				result.Nanoseconds.ShouldBe(expected.Nanoseconds);
				result.TotalMicroseconds.ShouldNotBeNull();
				result.TotalNanoseconds.ShouldNotBeNull();
				result.TotalMicroseconds.Value.ShouldBe(expected.TotalMicroseconds, 0.001D);
				result.TotalNanoseconds.Value.ShouldBe(expected.TotalNanoseconds, 1D);
#endif
			}
		}

		[Test]
		public void TimeSpanOperatorsMappedAsTicks(
			[IncludeDataSources(true,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird3Plus,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2016Plus,
				TestProvName.AllSybase)] string configuration)
		{
			var start    = new DateTime(2024, 2, 3, 4, 5, 6, 789);
			var interval = new TimeSpan(1, 2, 3, 4, 5);
			var tolerance = configuration.Contains("Sybase", StringComparison.Ordinal)
				? TimeSpan.FromMilliseconds(4)
				: TimeSpan.FromMilliseconds(1);

			using var db = GetDataContext(configuration, CreateMappingSchema(configuration));
			using var table = db.CreateLocalTable(new[]
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
			result.Added.Value.ShouldBe(start + interval, tolerance);
			result.AddedRequired.ShouldBe(start + interval, tolerance);
			result.AddedNullableDate.ShouldNotBeNull();
			result.AddedNullableInterval.ShouldNotBeNull();
			result.AddedNullableDate.Value.ShouldBe(start + interval, tolerance);
			result.AddedNullableInterval.Value.ShouldBe(start + interval, tolerance);
			result.Subtracted.Value.ShouldBe(start - interval, tolerance);
			result.Negated.ShouldBe(-interval);
			result.Difference.ShouldNotBeNull();
			result.Difference.Value.ShouldBe(interval, tolerance);
			result.DifferenceNullableLeft.ShouldNotBeNull();
			result.DifferenceNullableRight.ShouldNotBeNull();
			result.DifferenceNullableLeft.Value.ShouldBe(interval, tolerance);
			result.DifferenceNullableRight.Value.ShouldBe(TimeSpan.Zero, tolerance);
		}

		[Test]
		public void TimeSpanOperatorsSql(
			[IncludeDataSources(false,
				TestProvName.AllAccess,
				TestProvName.AllClickHouse,
				TestProvName.AllDuckDB,
				TestProvName.AllFirebird3Plus,
				TestProvName.AllInformix,
				TestProvName.AllMariaDB,
				TestProvName.AllMySql,
				TestProvName.AllOracle,
				TestProvName.AllPostgreSQL,
				TestProvName.AllSapHana,
				TestProvName.AllSQLite,
				TestProvName.AllSqlServer2016Plus,
				TestProvName.AllSybase)] string configuration)
		{
			var mappingSchema = CreateMappingSchema(configuration);
			var expectedType  = configuration.Contains("Access", StringComparison.Ordinal) ? DataType.Decimal : DataType.Int64;
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
				sql.ShouldContain("CDec(DATEDIFF");
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
				sql.ShouldContain("SECOND(1,7)");
			}
			else if (configuration.Contains("Sybase", StringComparison.Ordinal))
			{
				sql.ShouldContain("CONVERT(BIGINT, DATEDIFF(millisecond");
			}
		}
	}
}
