using System;
using System.Linq;

using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.xUpdate
{
	public partial class MergeTests
	{
		// Exercises FluentMappingBuilder's nested-member column mapping
		// (e.g. `o => o.Nested.Field`), where the column's CLR path crosses an
		// intermediate object on the entity graph. The implicit-setter branches of
		// MergeBuilder.UpdateWhenMatched / UpdateWhenMatchedThenDelete must walk the
		// full path (Target -> Nested -> Field) rather than look up the leaf member
		// by name on the entity root.

		public sealed class ComplexPropertyTarget
		{
			public int                   Id     { get; set; }
			public string?               Code   { get; set; }
			public ComplexPropertyNested Nested { get; set; } = new();
		}

		public sealed class ComplexPropertyNested
		{
			public bool Field { get; set; }
		}

		static MappingSchema BuildComplexPropertyMappingSchema()
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ComplexPropertyTarget>()
					.HasTableName("ComplexPropertyTarget")
					.Property(o => o.Id).IsPrimaryKey().HasColumnName("Id")
					.Property(o => o.Code).HasColumnName("Code").HasLength(50)
					.Property(o => o.Nested.Field).HasColumnName("Field")
				.Build();

			return ms;
		}

		[Test]
		public void ComplexProperty_UpdateWhenMatched([MergeDataContextSource] string context)
		{
			using var db    = GetDataContext(context, BuildComplexPropertyMappingSchema());
			using var table = db.CreateLocalTable<ComplexPropertyTarget>();

			db.Insert(new ComplexPropertyTarget { Id = 1, Code = "first",   Nested = new ComplexPropertyNested { Field = false } });
			db.Insert(new ComplexPropertyTarget { Id = 2, Code = "skipped", Nested = new ComplexPropertyNested { Field = false } });

			var source = new[]
			{
				new ComplexPropertyTarget { Id = 1, Code = "first-updated", Nested = new ComplexPropertyNested { Field = true } },
			};

			var rows = table.Merge()
				.Using(source)
				.OnTargetKey()
				.UpdateWhenMatched()
				.Merge();

			var result = table.OrderBy(r => r.Id).ToList();

			using (Assert.EnterMultipleScope())
			{
				AssertRowCount(1, rows, context);
				Assert.That(result,                 Has.Count.EqualTo(2));
				Assert.That(result[0].Id,           Is.EqualTo(1));
				Assert.That(result[0].Code,         Is.EqualTo("first-updated"));
				Assert.That(result[0].Nested.Field, Is.True);
				Assert.That(result[1].Id,           Is.EqualTo(2));
				Assert.That(result[1].Code,         Is.EqualTo("skipped"));
				Assert.That(result[1].Nested.Field, Is.False);
			}
		}

		[Test]
		public void ComplexProperty_InsertUpdate([MergeDataContextSource] string context)
		{
			using var db    = GetDataContext(context, BuildComplexPropertyMappingSchema());
			using var table = db.CreateLocalTable<ComplexPropertyTarget>();

			db.Insert(new ComplexPropertyTarget { Id = 1, Code = "alpha", Nested = new ComplexPropertyNested { Field = false } });
			db.Insert(new ComplexPropertyTarget { Id = 2, Code = "beta",  Nested = new ComplexPropertyNested { Field = false } });

			var rows = table.Merge()
				.Using(new[]
				{
					new ComplexPropertyTarget { Id = 1, Code = "alpha-new", Nested = new ComplexPropertyNested { Field = true } },
					new ComplexPropertyTarget { Id = 3, Code = "gamma",     Nested = new ComplexPropertyNested { Field = true } },
				})
				.OnTargetKey()
				.UpdateWhenMatched()
				.InsertWhenNotMatched()
				.Merge();

			var result = table.OrderBy(r => r.Id).ToList();

			using (Assert.EnterMultipleScope())
			{
				AssertRowCount(2, rows, context);
				Assert.That(result,                  Has.Count.EqualTo(3));
				Assert.That(result[0].Id,            Is.EqualTo(1));
				Assert.That(result[0].Code,          Is.EqualTo("alpha-new"));
				Assert.That(result[0].Nested.Field,  Is.True);
				Assert.That(result[1].Id,            Is.EqualTo(2));
				Assert.That(result[1].Code,          Is.EqualTo("beta"));
				Assert.That(result[1].Nested.Field,  Is.False);
				Assert.That(result[2].Id,            Is.EqualTo(3));
				Assert.That(result[2].Code,          Is.EqualTo("gamma"));
				Assert.That(result[2].Nested.Field,  Is.True);
			}
		}

		[Test]
		public void ComplexProperty_UpdateWithDelete([IncludeDataSources(true, TestProvName.AllOracle)] string context)
		{
			using var db    = GetDataContext(context, BuildComplexPropertyMappingSchema());
			using var table = db.CreateLocalTable<ComplexPropertyTarget>();

			db.Insert(new ComplexPropertyTarget { Id = 1, Code = "keep",   Nested = new ComplexPropertyNested { Field = false } });
			db.Insert(new ComplexPropertyTarget { Id = 2, Code = "remove", Nested = new ComplexPropertyNested { Field = false } });

			var source = new[]
			{
				new ComplexPropertyTarget { Id = 1, Code = "keep-updated",   Nested = new ComplexPropertyNested { Field = true } },
				new ComplexPropertyTarget { Id = 2, Code = "remove-updated", Nested = new ComplexPropertyNested { Field = true } },
			};

			table.Merge()
				.Using(source)
				.OnTargetKey()
				.UpdateWhenMatchedAndThenDelete((t, s) => true, (t, s) => s.Code == "remove-updated")
				.Merge();

			var result = table.OrderBy(r => r.Id).ToList();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(result,                 Has.Count.EqualTo(1));
				Assert.That(result[0].Id,           Is.EqualTo(1));
				Assert.That(result[0].Code,         Is.EqualTo("keep-updated"));
				Assert.That(result[0].Nested.Field, Is.True);
			}
		}
	}
}
