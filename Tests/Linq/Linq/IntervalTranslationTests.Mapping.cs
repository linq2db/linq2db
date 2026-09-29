using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

using Shouldly;

namespace Tests.Linq
{
	public partial class IntervalTranslationTests
	{

		/// <summary>
		/// A duration reached through a nested object rather than as a property of the entity itself.
		/// </summary>
		/// <remarks>
		/// The unit is declared on the path to the member, not on a property of the mapped type, so this asks whether
		/// the declaration is found where the column is defined rather than where the entity is. The control column
		/// beside it is the same nested shape carrying a hand-written converter, so a broken probe and a broken
		/// feature cannot be confused: if both go wrong the nesting is at fault, if only the declared one does the
		/// unit is.
		/// </remarks>
		[Table]
		sealed class NestedDurationRow
		{
			[PrimaryKey] public int Id { get; set; }

			// Named and typed fluently below rather than here: the attribute on this property describes the
			// object, and what needs describing is the column the member inside it becomes.
			public DeclaredNested  Declared  { get; set; } = new();
			public ConvertedNested Converted { get; set; } = new();

			// Two types rather than one used twice: a fluent declaration is keyed by the member it names, and both
			// paths would reach the same member of a shared type - the unit landing on the column the control was
			// meant to leave alone, which the mapping refuses outright.
			internal sealed class DeclaredNested
			{
				public TimeSpan Elapsed { get; set; }
			}

			internal sealed class ConvertedNested
			{
				public TimeSpan Elapsed { get; set; }
			}
		}

		[Test]
		public void ANestedColumnCarriesItsDeclaredUnit([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			// Access has no 64-bit integer, which is why the attributed models in this fixture declare Money for it.
			// Said here because the mapping is built in the test rather than on the type.
			var storage = context.IsAnyOf(TestProvName.AllAccess) ? DataType.Money : DataType.Int64;

			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<NestedDurationRow>()
					.Property(e => e.Declared.Elapsed)
						.HasColumnName("Declared")
						.HasDataType(storage)
						.HasDuration(DurationUnit.Second)
					.Property(e => e.Converted.Elapsed)
						.HasColumnName("Converted")
						.HasDataType(storage)
						.HasConversion(ts => ts.Ticks / TimeSpan.TicksPerSecond, v => TimeSpan.FromTicks(v * TimeSpan.TicksPerSecond))
				.Build();

			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable<NestedDurationRow>();

			db.Insert(new NestedDurationRow
			{
				Id        = 1,
				Declared  = new NestedDurationRow.DeclaredNested  { Elapsed = value },
				Converted = new NestedDurationRow.ConvertedNested { Elapsed = value },
			});

			var stored = t.Single();

			stored.Declared.Elapsed.ShouldBe(value);
			stored.Converted.Elapsed.ShouldBe(value);

			// Asked in SQL so a member computed in .NET cannot answer for the declaration: what is being pinned is
			// that the unit reached the translation, not that the value came back.
			var minutes = t.Select(r => Sql.AsSql(r.Declared.Elapsed.TotalMinutes)).Single();

			minutes.ShouldBe(value.TotalMinutes, Tolerance(value.TotalMinutes));
		}

		/// <summary>
		/// A duration held in a dynamic column, where the member does not exist on the type at all.
		/// </summary>
		/// <remarks>
		/// The other shape a new column handling tends to miss: the property is not on the entity, so the
		/// declaration is the only thing that says the column exists, what it holds and what unit it is in.
		/// </remarks>
		[Table]
		sealed class DynamicDurationRow
		{
			[PrimaryKey] public int Id { get; set; }

			[DynamicColumnsStore]
			public IDictionary<string, object> Values { get; set; } = new Dictionary<string, object>();
		}

		[Test]
		public void ADynamicColumnCarriesItsDeclaredUnit([DataSources] string context)
		{
			var value = TimeSpan.FromMinutes(90);

			var storage = context.IsAnyOf(TestProvName.AllAccess) ? DataType.Money : DataType.Int64;

			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<DynamicDurationRow>()
					.Property(e => Sql.Property<TimeSpan>(e, "Elapsed"))
						.HasDataType(storage)
						.HasDuration(DurationUnit.Second)
				.Build();

			using var db = GetDataContext(context, ms);
			using var t  = db.CreateLocalTable<DynamicDurationRow>();

			var row = new DynamicDurationRow { Id = 1 };
			row.Values["Elapsed"] = value;

			db.Insert(row);

			var stored = t.Single();

			((TimeSpan)stored.Values["Elapsed"]).ShouldBe(value);

			var minutes = t.Select(r => Sql.AsSql(Sql.Property<TimeSpan>(r, "Elapsed").TotalMinutes)).Single();

			minutes.ShouldBe(value.TotalMinutes, Tolerance(value.TotalMinutes));
		}

		[Test]
		public void UnitIsTakenFromTheDeclarationNotTheStorageType([DataSources] string context)
		{
			// Both columns are Int64 and hold the same duration, but in different units. If the unit were
			// inferred from the storage type instead of the declaration, one of these would be wrong.
			var value = TimeSpan.FromHours(3);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t
				.Select(r => new
				{
					Seconds = r.InSeconds.TotalHours,
					Ticks   = r.InTicks.TotalHours,
				})
				.Single();

			row.Seconds.ShouldBe(3d);
			row.Ticks.ShouldBe(3d);
		}

		[Test]
		public void ValueRoundTripsThroughTheDeclaredUnit([DataSources] string context)
		{
			// No HasConversion anywhere in this fixture for the declared columns - the conversion is derived from
			// the unit, so writing and reading back has to work on the declaration alone.
			var value = TimeSpan.FromSeconds(4567);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			var row = t.Single();

			row.InSeconds.ShouldBe(value);
			row.InTicks.ShouldBe(value);
		}

		[Test]
		public void NegatedConvertedColumnKeepsItsConverter([DataSources] string context)
		{
			// Undeclared, so no interval node is involved - this exercises the plain converted-column path and
			// answers whether GetColumnDescriptor's unary branch is fixing a pre-existing defect or only serving
			// the interval path. Whatever the query does, negating must not change which converter applies.
			var value = TimeSpan.FromMinutes(90);

			using var db = GetDataContext(context, BuildSchema());
			using var t  = db.CreateLocalTable<DurationRow>();
			Seed(db, value);

			t
				.Select(r => Sql.AsSql(-r.UndeclaredSeconds))
				.Single()
				.ShouldBe(-value);
		}
	}
}
