using System;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SQLite;
using LinqToDB.Internal.Options;
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

		[Table]
		sealed class OffsetContainsRow
		{
			[PrimaryKey] public int             Id     { get; set; }
			[Column]     public DateTimeOffset? Value  { get; set; }
			[Column]     public int             Marker { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5931#discussion_r4243965373")]
		public void LegacyTextIsNotNullInContains(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values] bool mixed,
			[Values] bool negate)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<OffsetContainsRow>();
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345));
			db.Execute("INSERT INTO [OffsetContainsRow] ([Id], [Value], [Marker]) VALUES (1, @legacy, 0), (2, NULL, 0), (3, @current, 0)",
				new { legacy = "9/15/2026 12:00:00 PM +05:45", current = value.ToString("yyyy-MM-dd HH:mm:ss.fffzzz", CultureInfo.InvariantCulture) });
			var values = mixed ? new DateTimeOffset?[] { null, value } : new DateTimeOffset?[] { null };
			var expected = negate ? (mixed ? new[] { 1 } : new[] { 1, 3 }) : (mixed ? new[] { 2, 3 } : new[] { 2 });
			var query = table.Where(r => negate ? !values.Contains(r.Value) : values.Contains(r.Value));
			query.OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(expected);
			query.Set(r => r.Marker, 1).Update().ShouldBe(expected.Length);
			table.Where(r => r.Marker == 1).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(expected);
			query.Delete().ShouldBe(expected.Length);
			table.OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(new[] { 1, 2, 3 }.Except(expected));
		}

		[Table]
		sealed class TimeContainsRow
		{
			[PrimaryKey] public int       Id       { get; set; }
			[Column]     public TimeSpan  Value    { get; set; }
			[Column]     public TimeSpan? Nullable { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/pull/5931#discussion_r4243965379")]
		public void TimeSpanContainsPreservesRawValues([IncludeDataSources(false, ProviderName.SQLiteMS)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<TimeContainsRow>();
			var values = new[] { TimeSpan.FromHours(10), TimeSpan.FromHours(10).Add(TimeSpan.FromTicks(1234567)), TimeSpan.FromHours(30), TimeSpan.FromHours(-2) };
			for (var i = 0; i < values.Length; i++)
				db.Insert(new TimeContainsRow { Id = i + 1, Value = values[i], Nullable = values[i] });
			db.BulkCopy(values.Select((v, i) => new TimeContainsRow { Id = i + 11, Value = v, Nullable = v }));
			var all = new[] { 1, 2, 3, 4, 11, 12, 13, 14 };
			var nullable = values.Select(v => (TimeSpan?)v).ToArray();
			table.Where(r => nullable.Contains(r.Nullable)).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(all);
			for (var i = 0; i < values.Length; i++)
			{
				var selectedId = i + 1;
				var one = new TimeSpan?[] { values[i] };
				var expected = new[] { selectedId, selectedId + 10 };
				table.Where(r => one.Contains(r.Nullable)).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(expected);
				table.Where(r => !one.Contains(r.Nullable)).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(all.Except(expected));
				var inner = table.Where(r => r.Id == selectedId).Select(r => r.Value);
				table.Where(r => inner.Contains(r.Value)).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(expected);
				table.Where(r => !inner.Contains(r.Value)).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(all.Except(expected));
			}
		}

#if SUPPORTS_DATEONLY
		[Table]
		sealed class TimeOnlyContainsRow
		{
			[PrimaryKey] public int      Id    { get; set; }
			[Column]     public TimeOnly Value { get; set; }
		}

		[Test]
		public void TimeOnlyContainsPreservesTicks([IncludeDataSources(false, ProviderName.SQLiteMS)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable<TimeOnlyContainsRow>();
			var value = new TimeOnly(10, 0).Add(TimeSpan.FromTicks(1234567));
			db.Execute("INSERT INTO [TimeOnlyContainsRow] ([Id], [Value]) VALUES (1, @first), (2, @second)",
				new { first = value.ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture), second = value.Add(TimeSpan.FromTicks(1)).ToString("HH:mm:ss.fffffff", CultureInfo.InvariantCulture) });
			var values = new[] { value };
			table.Where(r => values.Contains(r.Value)).Select(r => r.Id).ToArray().ShouldBe(new[] { 1 });
			table.Where(r => !values.Contains(r.Value)).Select(r => r.Id).ToArray().ShouldBe(new[] { 2 });
			var inner = table.Where(r => r.Id == 1).Select(r => r.Value);
			table.Where(r => inner.Contains(r.Value)).Select(r => r.Id).ToArray().ShouldBe(new[] { 1 });
		}
#endif

		[Test]
		public void DateTimeContainsKeepsRawColumn([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			using var db    = GetDataConnection(context);
			using var table = db.CreateLocalTable(new[] { new TaskRow { Id = 1, StartDateTime = Start } });
			var values = new DateTime?[] { Start };
			var query = table.Where(r => values.Contains(r.StartDateTime));
			query.Select(r => r.Id).ToArray().ShouldBe(new[] { 1 });
			query.ToSqlQuery().Sql.ShouldNotContain("strftime");
		}

		[Table]
		sealed class DateTypedRow
		{
			[PrimaryKey] public int Id { get; set; }
			[Column(DataType = DataType.Date)] public DateTime Date   { get; set; }
			[Column(DataType = DataType.Date)] public DateTime Target { get; set; }
			[Column] public double? Amount { get; set; }
			[Column(DataType = DataType.Int64), Duration(DurationUnit.Tick)]
			public TimeSpan? Duration { get; set; }
		}

		sealed class DateShiftResult
		{
			public int       Id      { get; set; }
			public DateTime  Target  { get; set; }
			public DateTime? Shifted { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void DateTypedShiftComparisons(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values(0, 1, 2, 3)] int shiftKind,
			[Values(ExpressionType.Equal, ExpressionType.NotEqual, ExpressionType.LessThan, ExpressionType.LessThanOrEqual,
				ExpressionType.GreaterThan, ExpressionType.GreaterThanOrEqual)] ExpressionType comparison,
			[Values] bool reverse,
			[Values] bool parameter)
		{
			var target = new DateTime(2026, 1, 2);
			var duration = TimeSpan.FromDays(shiftKind == 3 ? -1 : 1);
			var data = new[]
			{
				new DateTypedRow { Id = 1, Date = target.AddDays(-2), Target = target, Amount = 1, Duration = duration },
				new DateTypedRow { Id = 2, Date = target.AddDays(-1), Target = target, Amount = 1, Duration = duration },
				new DateTypedRow { Id = 3, Date = target,             Target = target, Amount = 1, Duration = duration },
				new DateTypedRow { Id = 4, Date = target,             Target = target, Amount = null, Duration = null },
			};
			using var db    = GetDataContext(context);
			using var table = db.CreateLocalTable(data);
			Expression<Func<DateTypedRow, DateShiftResult>> projection = shiftKind switch
			{
				0 => r => new DateShiftResult { Id = r.Id, Target = r.Target, Shifted = Sql.DateAdd(Sql.DateParts.Day, r.Amount, r.Date) },
				1 => r => new DateShiftResult { Id = r.Id, Target = r.Target, Shifted = r.Date.AddDays(r.Amount!.Value) },
				2 => r => new DateShiftResult { Id = r.Id, Target = r.Target, Shifted = r.Date + r.Duration },
				_ => r => new DateShiftResult { Id = r.Id, Target = r.Target, Shifted = r.Date - r.Duration },
			};
			var row = Expression.Parameter(typeof(DateShiftResult), "r");
			Expression shifted = Expression.Property(row, nameof(DateShiftResult.Shifted));
			Expression<Func<DateTime?>> bound = () => target;
			Expression other = parameter
				? bound.Body
				: Expression.Convert(Expression.Property(row, nameof(DateShiftResult.Target)), typeof(DateTime?));
			var predicate = Expression.Lambda<Func<DateShiftResult, bool>>(
				Expression.MakeBinary(comparison, reverse ? other : shifted, reverse ? shifted : other), row);

			// AddDays is non-nullable in the CLR; exercise its null SQL operand separately below.
			var expected = data.Where(r => shiftKind != 1 || r.Amount.HasValue)
				.Select(projection.Compile()).Where(predicate.Compile()).Select(r => r.Id).ToArray();
			var query = table.Select(projection);
			if (shiftKind == 1)
				query = query.Where(r => r.Id != 4);
			query.Where(predicate).OrderBy(r => r.Id).Select(r => r.Id).ToArray().ShouldBe(expected);
			table.Select(projection).Where(r => r.Shifted == null).Select(r => r.Id).ToArray().ShouldBe(new[] { 4 });
		}

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
			using var db    = GetDataContext(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = true }));
			using var table = db.CreateLocalTable(new[] { new OffsetTaskRow { Id = 1, PreNotification = duration } });

			var actual = table.Select(r => start + r.PreNotification).Single();
			actual.ShouldBe(start + duration);
			actual!.Value.Offset.ShouldBe(start.Offset);
		}

		[Table]
		sealed class OffsetKeyRow
		{
			[PrimaryKey] public DateTimeOffset Id    { get; set; }
			[Column]     public int            Value { get; set; }
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

			using var db    = GetDataContext(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = true }));
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
		[SetCulture("de-DE")]
		public void DateTimeOffsetParameterFormatIsOptIn([IncludeDataSources(false, TestProvName.AllSQLite)] string context)
		{
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345)).AddTicks(1234567);
			var classic = context.IsAnyOf(TestProvName.AllSQLiteClassic);
			// Reuse the same Insert/InsertOrReplace shapes across contexts with different option IDs.
			foreach (var enabled in new bool?[] { null, false, true, false })
			{
				using var db = GetDataConnection(context, o => enabled == null ? o : o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = enabled.Value }));
				using var table = db.CreateLocalTable<OffsetKeyRow>();
				db.Insert(new OffsetKeyRow { Id = value, Value = 1 });
				var expected = classic
					? enabled == true ? value.ToString("yyyy-MM-dd HH:mm:ss.fffzzz", CultureInfo.InvariantCulture) : value.ToString(CultureInfo.CurrentCulture)
					: value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture);
				var text = db.Execute<string>("SELECT CAST([Id] AS TEXT) FROM [OffsetKeyRow]");
				text.ShouldBe(expected);
				db.Execute<int>("SELECT COUNT(*) FROM [OffsetKeyRow] WHERE [Id] IN (@value)", new { value }).ShouldBe(1);
				db.InsertOrReplace(new OffsetKeyRow { Id = value, Value = 2 });
				table.Count().ShouldBe(1);
				table.Select(r => r.Value).Single().ShouldBe(2);
				if (enabled == true)
				{
					var stored = DateTimeOffset.Parse(text, CultureInfo.InvariantCulture);
					stored.Ticks.ShouldBe(classic ? value.Ticks / TimeSpan.TicksPerMillisecond * TimeSpan.TicksPerMillisecond : value.Ticks);
					stored.Offset.ShouldBe(value.Offset);
				}
			}
		}

		[Test]
		public void DateTimeOffsetOptionCopiesAndIdentifiesConfiguration()
		{
			var original = new SQLiteOptions();
			var enabled = original with { UseInvariantDateTimeOffsetFormat = true };
			var copy = enabled with { AlwaysCheckDbNull = false };
			original.UseInvariantDateTimeOffsetFormat.ShouldBeFalse();
			copy.UseInvariantDateTimeOffsetFormat.ShouldBeTrue();
			enabled.ShouldNotBe(original);
			((IOptionSet)enabled).ConfigurationID.ShouldNotBe(((IOptionSet)original).ConfigurationID);
			(copy with { AlwaysCheckDbNull = true }).ShouldBe(enabled);
			// Keep the existing constructor and deconstruction API.
			var (bulkCopy, checkNull) = original;
			bulkCopy.ShouldBe(BulkCopyType.MultipleRows);
			checkNull.ShouldBeTrue();
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		public void DateTimeOffsetLiteralsKeepMilliseconds(
			[IncludeDataSources(false, TestProvName.AllSQLite)] string context,
			[Values] bool inline,
			[Values] bool invariantParameters)
		{
			using var db = GetDataConnection(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = invariantParameters }));
			using var table = db.CreateLocalTable<OffsetKeyRow>();
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345)).AddTicks(1234567);
			if (inline)
			{
				db.InlineParameters = true;
				db.Insert(new OffsetKeyRow { Id = value });
			}
			else
				db.BulkCopy(new[] { new OffsetKeyRow { Id = value } });
			db.Execute<string>("SELECT CAST([Id] AS TEXT) FROM [OffsetKeyRow]")
				.ShouldBe(value.ToString("yyyy-MM-dd HH:mm:ss.fffzzz", CultureInfo.InvariantCulture));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/5930")]
		[SetCulture("en-US")]
		public void LegacyDateTimeOffsetParameterKeyRequiresMigration(
			[IncludeDataSources(false, TestProvName.AllSQLiteClassic)] string context,
			[Values] bool invariantParameters)
		{
			using var db = GetDataConnection(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = invariantParameters }));
			using var table = db.CreateLocalTable<OffsetKeyRow>();
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345));
			var legacy = value.ToString(CultureInfo.CurrentCulture);
			db.Execute("INSERT INTO [OffsetKeyRow] ([Id], [Value]) VALUES (@key, 1)", new { key = legacy });
			db.InsertOrReplace(new OffsetKeyRow { Id = value, Value = 2 });
			table.Count().ShouldBe(invariantParameters ? 2 : 1);
			db.Execute<int>("SELECT [Value] FROM [OffsetKeyRow] WHERE [Id] = @key", new { key = legacy }).ShouldBe(invariantParameters ? 1 : 2);
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/3766")]
		[SetCulture("de-DE")]
		public void InvariantDateTimeOffsetParametersSupportQueries([IncludeDataSources(false, TestProvName.AllSQLiteClassic)] string context)
		{
			using var db = GetDataConnection(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = true }));
			using var table = db.CreateLocalTable<OffsetKeyRow>();
			var value = new DateTimeOffset(Start, TimeSpan.FromMinutes(345));
			db.Insert(new OffsetKeyRow { Id = value, Value = 1 });
			db.Update(new OffsetKeyRow { Id = value, Value = 2 }).ShouldBe(1);
			table.Count(r => r.Id == value).ShouldBe(1);
			table.Count(r => r.Id < value.AddMinutes(1)).ShouldBe(1);
			var otherOffset = value.ToOffset(TimeSpan.FromHours(-4));
			table.Select(r => Sql.AsSql((r.Id - otherOffset).TotalHours)).Single().ShouldBe(0d);
			table.Select(r => Sql.AsSql((r.Id - otherOffset.AddHours(-2)).TotalHours)).Single().ShouldBe(2d);
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
			using var db    = GetDataContext(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = true }));
			using var table = db.CreateLocalTable<OffsetTaskRow>();
			foreach (var row in data)
				db.Execute("INSERT INTO [OffsetTaskRow] ([Id], [StartDateTime], [PreNotification]) VALUES (@id, @date, @ticks)",
					new { id = row.Id, date = row.StartDateTime!.Value.ToString("yyyy-MM-dd HH:mm:ss.FFFFFFFzzz", CultureInfo.InvariantCulture), ticks = row.PreNotification!.Value.Ticks });
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
			using var db    = GetDataContext(context, o => o.WithOptions<SQLiteOptions>(s => s with { UseInvariantDateTimeOffsetFormat = true }));
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
