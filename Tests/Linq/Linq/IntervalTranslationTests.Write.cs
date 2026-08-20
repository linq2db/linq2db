using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Data;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{
		/// <summary>
		/// A duration that may be absent survives a write in both states, and in both directions between them.
		/// </summary>
		/// <remarks>
		/// The conversion a declared unit derives is written for a value, and absence is not one. Storing it asks
		/// the write path to leave the conversion alone and put nothing in the column; storing a value again
		/// afterwards asks it to pick the conversion back up. A converter applied to an absent value would fail
		/// here rather than quietly, which is the point of asking both directions in one test.
		/// <para>
		/// The absent value also travels as a parameter with nothing to take its type from, which is where a
		/// provider that needs the type stated refuses. That is the same seam the comparison tests reach from the
		/// other side, and it is worth having on the write path too - the value goes into a column there rather
		/// than into a predicate, and the two are not the same code.
		/// </para>
		/// <para>
		/// The required column rides along unchanged. If both came back absent the test would still fail, which is
		/// what stops "everything is null" from reading as success.
		/// </para>
		/// </remarks>
		[Test]
		public void AnOptionalDurationRoundTripsIncludingItsAbsence([DataSources] string context)
		{
			var       value = TimeSpan.FromSeconds(4567);
			TimeSpan? none  = null;

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<OptionalDurationRow>();

			db.Insert(new OptionalDurationRow { Id = 1, Grace = value, Required = value });
			db.Insert(new OptionalDurationRow { Id = 2, Grace = none,  Required = value });

			var inserted = t.OrderBy(r => r.Id).ToList();

			inserted.Count.ShouldBe(2);
			inserted[0].Grace.ShouldBe(value);
			inserted[1].Grace.ShouldBeNull();
			inserted.Select(r => r.Required).ShouldBe([value, value]);

			// Both directions, so neither "a value never becomes absent" nor "absence never takes a value" can
			// pass unnoticed.
			t.Where(r => r.Id == 1).Set(r => r.Grace, none).Update();
			t.Where(r => r.Id == 2).Set(r => r.Grace, value).Update();

			var updated = t.OrderBy(r => r.Id).ToList();

			updated[0].Grace.ShouldBeNull();
			updated[1].Grace.ShouldBe(value);
			updated.Select(r => r.Required).ShouldBe([value, value]);
		}

		[Test]
		public void BulkCopyRoundTripsTheDeclaredUnit(
			[DataSources(false)] string context,
			[Values(BulkCopyType.RowByRow, BulkCopyType.MultipleRows, BulkCopyType.ProviderSpecific)] BulkCopyType copyType)
		{
			var value = TimeSpan.FromSeconds(4567);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			db.BulkCopy(
				new BulkCopyOptions().WithBulkCopyType(copyType),
				[
					new DurationRow
					{
						Id                = 1,
						InSeconds         = value,
						InTicks           = value,
						Undeclared        = value,
						UndeclaredSeconds = value,
					}
				]);

			var row = t.Single();

			row.InSeconds.ShouldBe(value);
			row.InTicks.ShouldBe(value);
			row.Undeclared.ShouldBe(value);
			row.UndeclaredSeconds.ShouldBe(value);
		}

		[Test]
		public void UpdateRoundTripsTheDeclaredUnit([DataSources] string context)
		{
			var value   = TimeSpan.FromSeconds(4567);
			var updated = TimeSpan.FromMinutes(321);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			// Through the entity, which goes by the descriptor...
			var row = t.Single();

			row.InSeconds = updated;
			row.InTicks   = updated;
			db.Update(row);

			var afterEntity = t.Single();

			afterEntity.InSeconds.ShouldBe(updated);
			afterEntity.InTicks.ShouldBe(updated);

			// ...and through a set expression, which builds its own assignment.
			t
				.Where(r => r.Id == 1)
				.Set(r => r.InSeconds, value)
				.Set(r => r.InTicks,   value)
				.Update();

			var afterSet = t.Single();

			afterSet.InSeconds.ShouldBe(value);
			afterSet.InTicks.ShouldBe(value);
		}

		[Test]
		public void UpsertRoundTripsTheDeclaredUnit(
			[DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			// The Upsert extension, which resolves to a native ON CONFLICT or a synthesised MERGE depending on the
			// provider - a different way of building the write than InsertOrUpdate below, so both are covered.
			// The two providers excluded resolve it to InsertOrUpdate, which they do not support either.
			var inserted = TimeSpan.FromSeconds(4567);
			var updated  = TimeSpan.FromMinutes(321);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			// The insert branch first, on an empty table, then the update branch over the same key.
			foreach (var value in new[] { inserted, updated })
			{
				t.Upsert(new DurationRow
				{
					Id                = 1,
					InSeconds         = value,
					InTicks           = value,
					Undeclared        = value,
					UndeclaredSeconds = value,
				});

				var row = t.Single();

				row.InSeconds.ShouldBe(value);
				row.InTicks.ShouldBe(value);
				row.Undeclared.ShouldBe(value);
				row.UndeclaredSeconds.ShouldBe(value);
			}
		}

		[Test]
		public void UpsertBuilderRoundTripsTheDeclaredUnit(
			[DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			// The builder is where a derived conversion is easiest to lose: Set writes a value into a duration
			// column and builds its own assignment rather than going through the entity's column list. The match
			// is on the key, so what this pins is the Set half.
			//
			// Column-to-column assignment is deliberately not exercised here: that copies stored numbers, and two
			// duration columns declared in different units hold different numbers for the same duration. Whether
			// such an assignment should convert is a question about differing converters in general, not about
			// this feature.
			var seed  = TimeSpan.FromSeconds(4567);
			var extra = TimeSpan.FromMinutes(30);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, seed);

			var payload = new DurationRow
			{
				Id                = 1,
				InSeconds         = seed,
				InTicks           = seed,
				Undeclared        = seed,
				UndeclaredSeconds = seed,
			};

			t.Upsert(payload, u => u
				.Match((target, source) => target.Id == source.Id)
				.Set(target => target.InTicks, () => extra));

			var row = t.Single();

			// Matched on the key, so no second row appeared, and the value written through Set comes back as the
			// duration it was.
			row.InTicks.ShouldBe(extra);
			row.InSeconds.ShouldBe(seed);
		}

		[Test]
		public void InsertOrUpdateRoundTripsTheDeclaredUnit(
			[DataSources(TestProvName.AllClickHouse, TestProvName.AllYdb)] string context)
		{
			// ClickHouse and YDB have no InsertOrUpdate at all, which is a provider capability rather than
			// anything about durations.
			var inserted = TimeSpan.FromSeconds(4567);
			var updated  = TimeSpan.FromMinutes(321);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();

			// The insert branch first, on an empty table, then the update branch over the same key.
			foreach (var value in new[] { inserted, updated })
			{
				t
					.InsertOrUpdate(
						() => new DurationRow
						{
							Id                = 1,
							InSeconds         = value,
							InTicks           = value,
							Undeclared        = value,
							UndeclaredSeconds = value,
						},
						r => new DurationRow
						{
							InSeconds = value,
							InTicks   = value,
						});

				var row = t.Single();

				row.InSeconds.ShouldBe(value);
				row.InTicks.ShouldBe(value);
			}
		}
	}
}
