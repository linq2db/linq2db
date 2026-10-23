using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// The coercion is emitted whatever type the operand reports, except over a literal.
		/// </summary>
		/// <remarks>
		/// A reported <c>DateTime64</c> is no evidence: <see cref="UndeclaredColumnOverServerDateTimeIsCoerced"/> is a
		/// column that reports one over a server <c>DateTime</c>.
		/// </remarks>
		[Test]
		public void TheCoercionIsEmittedWhateverTheOperandReports([IncludeDataSources(false, TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataConnection(context);
			using var t  = db.CreateLocalTable<EventRow>();

			// EventRow declares nothing on ClickHouse, so the table is created DateTime64(7) - and coerced anyway.
			_ = t
				.Select(r => (r.FinishedOn - r.StartedOn).TotalHours)
				.ToList();

			db.LastQuery!.ShouldContain("toUnixTimestamp64Nano(toDateTime64(r.FinishedOn, 7))");

			// A literal is the exception: it is written as toDateTime64('...', 7) already.
			var from = new DateTime(2026, 6, 1, 10, 0, 0);

			_ = t
				.Select(r => (r.FinishedOn - from).TotalHours)
				.ToList();

			db.LastQuery!.ShouldContain("toUnixTimestamp64Nano(toDateTime64('");
			db.LastQuery!.ShouldNotContain("toDateTime64(toDateTime64(");

			// now() is a whole-second timestamp whatever the mapping schema makes of a CLR DateTime.
			_ = t
				.Select(r => (Sql.CurrentTimestamp - r.StartedOn).TotalHours)
				.ToList();

			db.LastQuery!.ShouldContain("toDateTime64(now()");

			// makeDateTime answers a whole-second timestamp too.
			_ = t
				.Select(r => (r.FinishedOn - Sql.MakeDateTime(2026, 6, 1, 10, 0, 0)!.Value).TotalHours)
				.ToList();

			db.LastQuery!.ShouldContain("toDateTime64(makeDateTime(");

			using var coarse = db.CreateLocalTable<CoarseEventRow>();

			// A declared DataType.DateTime column is a 32-bit whole-second timestamp on the server.
			_ = coarse
				.Select(r => (r.FinishedOn - r.StartedOn).TotalHours)
				.ToList();

			db.LastQuery!.ShouldContain("toDateTime64(");
		}

		[Table]
		sealed class UndeclaredServerDateTimeRow
		{
			[Column] public int      Id { get; set; }
			[Column] public DateTime At { get; set; }
		}

		// No attributes at all: every public member is a column, typed by the mapping schema.
		sealed class UnattributedServerDateTimeRow
		{
			public int      Id { get; set; }
			public DateTime At { get; set; }
		}

		public enum UndeclaredOperandShape
		{
			TimeOfDay,
			ShiftedTimeOfDay,
			Difference,
			MillisecondPart,
			MillisecondShift,
			ShiftByClientDifference,
			Contains,
			UnattributedTimeOfDay,
		}

		/// <summary>
		/// A column that declares no type reports the mapping schema's default for a CLR <see cref="DateTime"/>, which
		/// says nothing about the server column - here a whole-second <c>DateTime</c> created outside LinqToDB.
		/// </summary>
		[Test]
		public void UndeclaredColumnOverServerDateTimeIsCoerced([IncludeDataSources(false, TestProvName.AllClickHouse)] string context, [Values] UndeclaredOperandShape shape)
		{
			using var db = GetDataConnection(context);

			// The case exists only while the schema default disagrees with the server column.
			var defaultType = db.MappingSchema.GetDbDataType(typeof(DateTime)).DataType;

			defaultType.ShouldBe(DataType.DateTime64);

			db.DropTable<UndeclaredServerDateTimeRow>(throwExceptionIfNotExists: false);
			db.Execute("CREATE TABLE UndeclaredServerDateTimeRow (Id Int32, At DateTime) ENGINE = Memory");

			try
			{
				db.Execute("INSERT INTO UndeclaredServerDateTimeRow VALUES (1, '2026-06-01 10:00:00')");

				var t = db.GetTable<UndeclaredServerDateTimeRow>();

				switch (shape)
				{
					case UndeclaredOperandShape.TimeOfDay:
					{
						var timesOfDay = t
							.Select(r => Sql.AsSql(r.At.TimeOfDay))
							.ToArray();

						timesOfDay.ShouldBe([CoarseStart.TimeOfDay]);
						break;
					}

					// addHours over a DateTime answers a DateTime, while the translation types it as its operand.
					case UndeclaredOperandShape.ShiftedTimeOfDay:
					{
						var timesOfDay = t
							.Select(r => Sql.AsSql(r.At.AddHours(1).TimeOfDay))
							.ToArray();

						timesOfDay.ShouldBe([CoarseStart.AddHours(1).TimeOfDay]);
						break;
					}

					case UndeclaredOperandShape.Difference:
					{
						var elapsed = t
							.Select(r => Sql.AsSql((r.At - r.At.Date).TotalMilliseconds))
							.ToArray();

						elapsed.ShouldBe([10 * 3600_000d]);
						break;
					}

					case UndeclaredOperandShape.MillisecondPart:
					{
						var milliseconds = t
							.Select(r => Sql.AsSql(Sql.DatePart(Sql.DateParts.Millisecond, r.At)))
							.ToArray();

						milliseconds.ShouldBe([0]);
						break;
					}

					case UndeclaredOperandShape.MillisecondShift:
					{
						var shifted = t
							.Select(r => Sql.AsSql(r.At.AddMilliseconds(500)))
							.ToArray();

						shifted.ShouldBe([CoarseStart.AddMilliseconds(500)]);
						break;
					}

					case UndeclaredOperandShape.ShiftByClientDifference:
					{
						var earlier = CoarseStart;
						var later   = CoarseStart.AddMilliseconds(500);

						var shifted = t
							.Select(r => Sql.AsSql(r.At + (later - earlier)))
							.ToArray();

						shifted.ShouldBe([CoarseStart.AddMilliseconds(500)]);
						break;
					}

					case UndeclaredOperandShape.Contains:
					{
						var values = new[] { CoarseStart, CoarseStart.AddHours(1) };

						var matched = t.Count(r => values.Contains(r.At));

						matched.ShouldBe(1);
						db.LastQuery!.ShouldNotContain("toDateTime64(toDateTime64(");
						break;
					}

					case UndeclaredOperandShape.UnattributedTimeOfDay:
					{
						var timesOfDay = db.GetTable<UnattributedServerDateTimeRow>()
							.TableName("UndeclaredServerDateTimeRow")
							.Select(r => Sql.AsSql(r.At.TimeOfDay))
							.ToArray();

						timesOfDay.ShouldBe([CoarseStart.TimeOfDay]);
						break;
					}

					default:
						throw new InvalidOperationException($"Unexpected shape {shape}");
				}
			}
			finally
			{
				db.DropTable<UndeclaredServerDateTimeRow>(throwExceptionIfNotExists: false);
			}
		}

		[Table]
		sealed class MillisecondRow
		{
			[Column]                                                public int      Id { get; set; }
			[Column(DataType = DataType.DateTime64, Precision = 3)] public DateTime At { get; set; }
		}

		/// <summary>
		/// A value finer than a declared <c>DateTime64(3)</c> column is compared at the column's precision, in a list as
		/// with <c>==</c>.
		/// </summary>
		[Test]
		public void FinerValueBesideAMillisecondColumnMatchesInEveryForm([IncludeDataSources(true, TestProvName.AllClickHouse)] string context)
		{
			var stored = new DateTime(2026, 6, 1, 10, 0, 0, 100);
			var finer  = stored.AddTicks(4000);

			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable([new MillisecondRow { Id = 1, At = stored }]);

			var values = new[] { finer };

			t.Count(r => r.At == finer).ShouldBe(1);
			t.Count(r => new[] { finer, finer.AddHours(1) }.Contains(r.At)).ShouldBe(1);
			t.Count(r => values.Contains(r.At)).ShouldBe(1);
		}

		[Test]
		public void CoalesceOverASecondPrecisionColumnIsCoerced([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			var timesOfDay = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql((r.FinishedOn ?? r.StartedOn).TimeOfDay))
				.ToArray();

			timesOfDay.ShouldBe([CoarseStart.TimeOfDay, CoarseStart.AddHours(3).TimeOfDay]);

			var elapsed = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(((r.FinishedOn ?? r.StartedOn) - r.StartedOn).TotalMilliseconds))
				.ToArray();

			elapsed.ShouldBe([0d, 2 * 3600_000d]);
		}

		[Table]
		sealed class MixedCoarseEventRow
		{
			[PrimaryKey]                           public int       Id         { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime  StartedOn  { get; set; }
			[Column(DataType = DataType.DateTime)] public DateTime? FinishedOn { get; set; }
			[Column(DataType = DataType.Date)]     public DateTime  OpenedOn   { get; set; }
		}

		/// <summary>
		/// A COALESCE whose operands disagree on their coarse type reports no type of its own, and is coerced too.
		/// </summary>
		[Test]
		public void CoalesceOfDisagreeingCoarseTypesIsCoerced([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = db.CreateLocalTable(
			[
				new MixedCoarseEventRow { Id = 1, StartedOn = CoarseStart, FinishedOn = null,                    OpenedOn = CoarseStart.Date },
				new MixedCoarseEventRow { Id = 2, StartedOn = CoarseStart, FinishedOn = CoarseStart.AddHours(3), OpenedOn = CoarseStart.Date },
			]);

			var elapsed = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql(((r.FinishedOn ?? r.OpenedOn) - r.StartedOn).TotalHours))
				.ToArray();

			elapsed.ShouldBe([-10d, 3d]);
		}

		[Test]
		public void ValueWindowFunctionOverASecondPrecisionColumnIsCoerced([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			var bounds = t
				.Select(r => new
				{
					r.Id,
					First = Sql.AsSql(Sql.Window.FirstValue(r.StartedOn, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded).TimeOfDay),
					Last  = Sql.AsSql(Sql.Window.LastValue(r.StartedOn, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded).TimeOfDay),
				})
				.OrderBy(r => r.Id)
				.Select(r => new { r.First, r.Last })
				.ToArray();

			bounds.ShouldAllBe(r => r.First == CoarseStart.TimeOfDay && r.Last == CoarseStart.AddHours(1).TimeOfDay);

			var lags = t
				.Select(r => new
				{
					r.Id,
					Lag       = Sql.AsSql((r.StartedOn - Sql.Window.Lag(r.StartedOn, w => w.OrderBy(r.Id))).TotalMilliseconds),
					LagOffset = Sql.AsSql((r.StartedOn - Sql.Window.Lag(r.StartedOn, 1, r.StartedOn, w => w.OrderBy(r.Id))).TotalMilliseconds),
					Lead      = Sql.AsSql((Sql.Window.Lead(r.StartedOn, 1, r.StartedOn, w => w.OrderBy(r.Id)) - r.StartedOn).TotalMilliseconds),
					Nth       = Sql.AsSql((Sql.Window.NthValue(r.StartedOn, 2L, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded) - r.StartedOn).TotalMilliseconds),
				})
				.ToArray()
				.OrderBy(r => r.Id)
				.ToArray();

			// The first row has no predecessor, and ClickHouse answers its type's default rather than NULL.
			lags[1].Lag.ShouldBe(3600_000d);

			var lagOffsets = lags.Select(r => r.LagOffset);

			lagOffsets.ShouldBe([0d, 3600_000d]);

			var leads = lags.Select(r => r.Lead);

			leads.ShouldBe([3600_000d, 0d]);

			var nths = lags.Select(r => r.Nth);

			nths.ShouldBe([3600_000d, 0d]);
		}

		/// <summary>
		/// The same coercion for an analytic function built from a <see cref="Sql.ExtensionAttribute"/>.
		/// </summary>
		[Test]
		public void ExtensionAnalyticFunctionOverASecondPrecisionColumnIsCoerced([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			var hours = t
				.OrderBy(r => r.Id)
				.Select(r => Sql.AsSql((Sql.Ext.Max(r.StartedOn).Over().ToValue() - r.StartedOn).TotalHours))
				.ToArray();

			hours.ShouldBe([1d, 0d]);
		}

		/// <summary>
		/// A parameter compared with a value computed from a whole-second column takes the column's type, as it does
		/// beside the column itself, so its sub-second part is lost.
		/// </summary>
		[ActiveIssue(5997, Configuration = TestProvName.AllClickHouse, ErrorTypeName = "Shouldly.ShouldAssertException", ErrorMessage = "should be{0}2{1}but was")]
		[Test]
		public void ComputedSecondPrecisionValueComparedWithSubSecondParameter([IncludeDataSources(TestProvName.AllClickHouse)] string context)
		{
			using var db = GetDataContext(context);
			using var t  = SeedCoarseNullable(db);

			var windowed = t
				.Select(r => new
				{
					First = Sql.Window.FirstValue(r.StartedOn, w => w.OrderBy(r.Id).RowsBetween.Unbounded.And.Unbounded),
					Lag   = Sql.Window.Lag(r.StartedOn, 1, r.StartedOn, w => w.OrderBy(r.Id)),
				})
				.AsSubQuery();

			var boundary = CoarseSubSecond;

			var lagBelow = windowed.Count(r => r.Lag < boundary);

			lagBelow.ShouldBe(2);

			var firstBelow = windowed.Count(r => r.First < boundary);

			firstBelow.ShouldBe(2);
		}
	}
}
