using System;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue5930Tests : TestBase
	{
		[Table]
		sealed class TaskRow
		{
			[PrimaryKey] public int       Id              { get; set; }
			[Column]     public DateTime? StartDateTime   { get; set; }
			[Column]     public TimeSpan? PreNotification { get; set; }
		}

		static readonly DateTime Start = new DateTime(2026, 9, 15, 12, 0, 0);

		static TaskRow[] CreateData() =>
		[
			new TaskRow { Id = 1, StartDateTime = Start, PreNotification = TimeSpan.FromHours(2) },
			new TaskRow { Id = 2, StartDateTime = Start, PreNotification = null                  },
			new TaskRow { Id = 3, StartDateTime = null,  PreNotification = TimeSpan.FromHours(2) },
			new TaskRow { Id = 4, StartDateTime = null,  PreNotification = null                  },
		];

		static MappingSchema CreateMapping(DurationUnit unit = DurationUnit.Tick)
		{
			var schema = new MappingSchema();
			var builder = new FluentMappingBuilder(schema);
			builder.Entity<TaskRow>()
				.Property(r => r.PreNotification)
				.HasDataType(DataType.Int64)
				.HasDuration(unit)
				.IsNullable();

			builder.Build();
			return schema;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ReadTasksWithDeclaredDuration(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			var data = CreateData();
			using var db    = GetDataContext(context, CreateMapping());
			using var table = db.CreateLocalTable(data);

			var stored = table.OrderBy(r => r.Id).ToArray();
			stored.Select(r => r.StartDateTime).ShouldBe(data.Select(r => r.StartDateTime));
			stored.Select(r => r.PreNotification).ShouldBe(data.Select(r => r.PreNotification));

			var actual = table.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(r.StartDateTime - r.PreNotification))
				.ToArray();

			actual.ShouldBe(data.Select(r => r.StartDateTime - r.PreNotification));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void FilterTasksWithDeclaredDuration(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			using var db    = GetDataContext(context, CreateMapping());
			using var table = db.CreateLocalTable(CreateData());
			var cutoff = Start.AddMinutes(-30);

			var query = table.Select(r => new
			{
				r.Id,
				NotificationDateTime = r.StartDateTime - r.PreNotification,
			});

			var actual = query.Where(r => r.NotificationDateTime < cutoff).Select(r => r.Id).ToArray();

			actual.ShouldBe(new[] { 1 });
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ShiftDeclaredDurationInSql(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context,
			[Values(DurationUnit.Tick, DurationUnit.Second)] DurationUnit unit,
			[Values] bool subtract)
		{
			var durations = new[]
			{
				TimeSpan.Zero,
				TimeSpan.FromTicks(9999),
				TimeSpan.FromTicks(-9999),
				TimeSpan.FromTicks(15000),
				TimeSpan.FromTicks(-15000),
				TimeSpan.FromDays(2) + TimeSpan.FromMilliseconds(123),
				-TimeSpan.FromDays(2) - TimeSpan.FromMilliseconds(123),
				TimeSpan.FromDays(700000) + TimeSpan.FromMilliseconds(123),
				-TimeSpan.FromDays(700000) - TimeSpan.FromMilliseconds(123),
			};
			var storageUnit = unit == DurationUnit.Tick ? 1 : TimeSpan.TicksPerSecond;
			var data = CreateData().Concat(durations.Select((duration, index) => new TaskRow
			{
				Id              = index + 5,
				StartDateTime   = Start.AddMilliseconds(456),
				PreNotification = TimeSpan.FromTicks(duration.Ticks / storageUnit * storageUnit),
			})).ToArray();

			using var db    = GetDataContext(context, CreateMapping(unit));
			using var table = db.CreateLocalTable(data);

			var query = table.Select(r => new
			{
				r.Id,
				Shifted = Sql.AsSql(subtract ? r.StartDateTime - r.PreNotification : r.StartDateTime + r.PreNotification),
			});
			var expected = data.Select(r =>
			{
				var duration = r.PreNotification.HasValue
					? TimeSpan.FromTicks(r.PreNotification.Value.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond)
					: (TimeSpan?)null;
				return subtract ? r.StartDateTime - duration : r.StartDateTime + duration;
			}).ToArray();

			query.OrderBy(r => r.Id).Select(r => r.Shifted).ToArray().ShouldBe(expected);
			query.Where(r => r.Shifted == null).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(new[] { 2, 3, 4 });
		}

		[Table]
		sealed class OffsetTaskRow
		{
			[PrimaryKey] public int             Id              { get; set; }
			[Column]     public DateTimeOffset? StartDateTime   { get; set; }
			[Column(DataType = DataType.Int64), Duration(DurationUnit.Tick)]
			public TimeSpan? PreNotification { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		[SetCulture("de-DE")]
		public void ShiftDateTimeOffsetParameter(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			var start    = new DateTimeOffset(Start, TimeSpan.FromMinutes(345));
			var duration = TimeSpan.FromMinutes(90);
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(new[] { new OffsetTaskRow { Id = 1, PreNotification = duration } });

			var actual = table.Select(r => start + r.PreNotification).Single();
			actual.ShouldBe(start + duration);
			actual!.Value.Offset.ShouldBe(start.Offset);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		[SetCulture("de-DE")]
		public void DateTimeOffsetParameterPreservesTicks(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context)
		{
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345)).AddTicks(1234567);
			using var db = GetDataContext(context);
			var actual = db.Select(() => Sql.AsSql(value));

			actual.Ticks.ShouldBe(value.Ticks);
			actual.Offset.ShouldBe(value.Offset);
		}

		[Table]
		sealed class OffsetKeyRow
		{
			[PrimaryKey] public DateTimeOffset Id    { get; set; }
			[Column]     public int            Value { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		[SetCulture("de-DE")]
		public void DateTimeOffsetStorageMatchesInlineMode(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context,
			[Values] bool inline,
			[Values(0, 1230000, 1234567)] int ticks)
		{
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345)).AddTicks(ticks);
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<OffsetKeyRow>();

			db.InlineParameters = inline;
			db.Insert(new OffsetKeyRow { Id = value, Value = 1 });
			db.InlineParameters = !inline;
			db.InsertOrReplace(new OffsetKeyRow { Id = value, Value = 2 });

			table.Count().ShouldBe(1);
			table.Select(r => r.Value).Single().ShouldBe(2);
			var stored = db.Execute<string>("SELECT CAST([Id] AS TEXT) FROM [OffsetKeyRow]");
			var actual = DateTimeOffset.Parse(stored, CultureInfo.InvariantCulture);
			actual.Ticks.ShouldBe(value.Ticks);
			actual.Offset.ShouldBe(value.Offset);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ShiftPreservesDateTimeOffset(
			[IncludeDataSources(false, TestProvName.AllSQLiteBase)] string context,
			[Values(-330, 0, 345)] int offsetMinutes,
			[Values] bool subtract)
		{
			var start    = new DateTimeOffset(Start.AddMilliseconds(456), TimeSpan.FromMinutes(offsetMinutes));
			var duration = TimeSpan.FromDays(2) + TimeSpan.FromMilliseconds(123);
			var data = new[]
			{
				new OffsetTaskRow { Id = 1, StartDateTime = start, PreNotification = duration  },
				new OffsetTaskRow { Id = 2, StartDateTime = start, PreNotification = null      },
				new OffsetTaskRow { Id = 3, StartDateTime = null,  PreNotification = duration  },
				new OffsetTaskRow { Id = 4, StartDateTime = start, PreNotification = -duration },
			};

			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query = table.Select(r => new
			{
				r.Id,
				Shifted = Sql.AsSql(subtract ? r.StartDateTime - r.PreNotification : r.StartDateTime + r.PreNotification),
			});
			var actual   = query.OrderBy(r => r.Id).Select(r => r.Shifted).ToArray();
			var expected = data.Select(r => subtract ? r.StartDateTime - r.PreNotification : r.StartDateTime + r.PreNotification).ToArray();

			actual.ShouldBe(expected);
			actual.Select(d => d?.Offset).ShouldBe(expected.Select(d => d?.Offset));
			query.Where(r => r.Shifted == null).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(new[] { 2, 3 });
			var cutoff = expected[0]!.Value.AddMinutes(30).ToOffset(TimeSpan.FromHours(offsetMinutes < 0 ? 10 : -10));
			query.Where(r => r.Shifted < cutoff).OrderBy(r => r.Id).Select(r => r.Id).ToArray()
				.ShouldBe(data.Where((r, index) => expected[index] < cutoff).Select(r => r.Id));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void LegacyDateTimeOffsetParameterKey(
			[IncludeDataSources(false, TestProvName.AllSQLiteClassic)] string context,
			[Values("en-US", "")] string culture)
		{
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345));
			var legacyText = value.ToString(CultureInfo.GetCultureInfo(culture));
			VerifyLegacyKey(context, value, legacyText);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void LegacyDateTimeOffsetLiteralKey(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values(0, 1230000, 1234567)] int ticks)
		{
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345)).AddTicks(ticks);
			var legacyText = value.ToString("yyyy-MM-dd HH:mm:ss.fffzzz", CultureInfo.InvariantCulture);
			VerifyLegacyKey(context, value, legacyText);
		}

		void VerifyLegacyKey(string context, DateTimeOffset value, string legacyText)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<OffsetKeyRow>();
			// Seed the physical text written before the format change, without today's DateTimeOffset converter.
			db.Execute("INSERT INTO [OffsetKeyRow] ([Id], [Value]) VALUES (@key, 1)", new { key = legacyText });
			db.InsertOrReplace(new OffsetKeyRow { Id = value, Value = 2 });

			var currentText = value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
			if (legacyText == currentText)
			{
				table.Count().ShouldBe(1);
				table.Select(r => r.Value).Single().ShouldBe(2);
				return;
			}

			table.Count().ShouldBe(2);
			db.Query<string>("SELECT CAST([Id] AS TEXT) FROM [OffsetKeyRow] ORDER BY [Value]").ToArray()
				.ShouldBe(new[] { legacyText, currentText });
			table.OrderBy(r => r.Value).Select(r => r.Value).ToArray().ShouldBe(new[] { 1, 2 });
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ShiftSubMillisecondDateTimeOffset(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values(-330, 345)] int offsetMinutes,
			[Values] bool subtract)
		{
			var offset = TimeSpan.FromMinutes(offsetMinutes);
			var data = new[]
			{
				new OffsetTaskRow { Id = 1, StartDateTime = new DateTimeOffset(Start.AddTicks(1234000), offset), PreNotification = TimeSpan.Zero },
				new OffsetTaskRow { Id = 2, StartDateTime = new DateTimeOffset(Start.AddTicks(4567891), offset), PreNotification = TimeSpan.FromHours(1) },
			};
			var expected = new DateTimeOffset?[]
			{
				new DateTimeOffset(Start.AddMilliseconds(123), offset),
				new DateTimeOffset(Start.AddMilliseconds(457).AddHours(subtract ? -1 : 1), offset),
			};
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var actual = table.OrderBy(r => r.Id)
				.Select(r => subtract ? r.StartDateTime - r.PreNotification : r.StartDateTime + r.PreNotification).ToArray();
			var forced = table.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(subtract ? r.StartDateTime - r.PreNotification : r.StartDateTime + r.PreNotification)).ToArray();

			actual.ShouldBe(expected);
			forced.ShouldBe(expected);
			actual.Select(d => d?.Offset).ShouldBe(expected.Select(d => d?.Offset));
		}

		[Table]
		sealed class DateAddRow
		{
			[PrimaryKey] public int             Id     { get; set; }
			[Column]     public DateTimeOffset? Value  { get; set; }
			[Column]     public double?         Amount { get; set; }
		}

#if SUPPORTS_DATEONLY
		[Table]
		sealed class DateOnlyAddRow
		{
			[PrimaryKey] public int       Id     { get; set; }
			[Column]     public DateOnly? Value  { get; set; }
			[Column]     public double?   Amount { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void DateAddNullableDateOnly([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var start = new DateOnly(2026, 1, 1);
			var data = new[]
			{
				new DateOnlyAddRow { Id = 1, Value = start, Amount = 1 },
				new DateOnlyAddRow { Id = 2, Value = start, Amount = null },
				new DateOnlyAddRow { Id = 3, Value = null, Amount = 1 },
				new DateOnlyAddRow { Id = 4, Value = null, Amount = null },
			};
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query = table.Select(r => new { r.Id, Shifted = Sql.AsSql(Sql.DateAdd(Sql.DateParts.Day, r.Amount, r.Value)) });
			query.OrderBy(r => r.Id).Select(r => r.Shifted).ToArray().ShouldBe(new DateOnly?[] { start.AddDays(1), null, null, null });
			query.Where(r => r.Shifted == null).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(new[] { 2, 3, 4 });
		}
#endif

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void DateAddPreservesDateTimeOffset(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values(-330, 0, 345)] int offsetMinutes,
			[Values(Sql.DateParts.Year, Sql.DateParts.Quarter, Sql.DateParts.Month, Sql.DateParts.Day, Sql.DateParts.Week,
				Sql.DateParts.Hour, Sql.DateParts.Minute, Sql.DateParts.Second, Sql.DateParts.Millisecond)] Sql.DateParts part)
		{
			// UTC and local dates differ at this calendar boundary for a positive offset.
			var start = new DateTimeOffset(2026, 1, 1, 0, 30, 0, 123, TimeSpan.FromMinutes(offsetMinutes));
			var data = new[]
			{
				new DateAddRow { Id = 1, Value = start, Amount = 1 },
				new DateAddRow { Id = 2, Value = start, Amount = -1 },
				new DateAddRow { Id = 3, Value = start, Amount = 0 },
				new DateAddRow { Id = 4, Value = start, Amount = null },
				new DateAddRow { Id = 5, Value = null, Amount = 1 },
			};
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			var query = table.Select(r => new { r.Id, Shifted = Sql.AsSql(Sql.DateAdd(part, r.Amount, r.Value)) });
			var actual   = query.OrderBy(r => r.Id).Select(r => r.Shifted).ToArray();
			var expected = data.Select(r => Sql.DateAdd(part, r.Amount, r.Value)).ToArray();

			actual.ShouldBe(expected);
			actual.Select(d => d?.Offset).ShouldBe(expected.Select(d => d?.Offset));
			query.Where(r => r.Shifted == null).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(new[] { 4, 5 });
			var cutoff = expected[0]!.Value.AddMilliseconds(1).ToOffset(TimeSpan.FromHours(-10));
			query.Where(r => r.Shifted < cutoff).OrderBy(r => r.Id).Select(r => r.Id).ToArray()
				.ShouldBe(data.Where((r, i) => expected[i] < cutoff).Select(r => r.Id));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ShiftUtcText(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values("2026-09-15 12:00:00Z", "2026-09-15 12:00:00")] string text)
		{
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable<OffsetTaskRow>();
			db.Execute("INSERT INTO [OffsetTaskRow] ([Id], [StartDateTime], [PreNotification]) VALUES (1, @value, @ticks)",
				new { value = text, ticks = TimeSpan.TicksPerHour });
			var actual = table.Select(r => Sql.AsSql(r.StartDateTime + r.PreNotification)).Single();

			actual.ShouldBe(new DateTimeOffset(Start.AddHours(1), TimeSpan.Zero));
			actual!.Value.Offset.ShouldBe(TimeSpan.Zero);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void ShiftPredicateDoesNotReformatDateTime(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataContext(context, CreateMapping());
			using var table = db.CreateLocalTable(CreateData());
			var cutoff = Start.AddMinutes(-30);
			var query = table.Where(r => r.StartDateTime - r.PreNotification < cutoff).Select(r => r.Id);
			var sql = query.ToSqlQuery().Sql;

			sql.ShouldNotContain("strftime('%Y-%m-%d %H:%M:%f', strftime(");
			query.ToArray().ShouldBe(new[] { 1 });
		}
	}
}
