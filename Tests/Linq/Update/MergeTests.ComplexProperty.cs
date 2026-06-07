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

		// An explicit interface implementation's CLR member name itself contains dots
		// (e.g. "…IExplicitComplexProperty.Field"). The implicit-setter dot-path walk must NOT
		// treat that as a nested member path — it has to fall through to the leaf-member lookup,
		// otherwise MERGE query build throws InvalidOperationException. Regression for the #5543 fix.

		public interface IExplicitComplexProperty
		{
			bool Field { get; set; }
		}

		public sealed class ExplicitComplexPropertyTarget : IExplicitComplexProperty
		{
			public int Id { get; set; }

			[Column("Field")]
			bool IExplicitComplexProperty.Field { get; set; }
		}

		[Test]
		public void ExplicitInterfaceProperty_UpdateWhenMatched([MergeDataContextSource] string context)
		{
			var ms = new MappingSchema();

			new FluentMappingBuilder(ms)
				.Entity<ExplicitComplexPropertyTarget>()
					.HasTableName("ExplicitComplexPropertyTarget")
					.Property(o => o.Id).IsPrimaryKey().HasColumnName("Id")
				.Build();

			var target = new ExplicitComplexPropertyTarget { Id = 1 };
			((IExplicitComplexProperty)target).Field = false;

			var source = new ExplicitComplexPropertyTarget { Id = 1 };
			((IExplicitComplexProperty)source).Field = true;

			using var db    = GetDataContext(context, ms);
			using var table = db.CreateLocalTable<ExplicitComplexPropertyTarget>();

			db.Insert(target);

			table.Merge()
				.Using(new[] { source })
				.OnTargetKey()
				.UpdateWhenMatched()
				.Merge();

			var result = table.Single();

			Assert.That(((IExplicitComplexProperty)result).Field, Is.True);
		}

		// The inheritance discriminator can itself be mapped through a nested member path
		// (Property(o => o.Meta.Kind)). This PR routes the discriminator 'is'/OfType predicate sites
		// (ExpressionBuilder.SqlBuilder.cs / ExpressionBuildVisitor.cs) through the same helper, so the
		// nested path has to be dot-walked there the way the MERGE implicit setters are. Not a MERGE test,
		// but it lives here to keep the #5543 nested-mapping regressions together.

		public class NestedDiscriminatorMeta
		{
			public string? Kind { get; set; }
		}

		public class NestedDiscriminatorBase
		{
			public int                     Id   { get; set; }
			public NestedDiscriminatorMeta Meta { get; set; } = new();
		}

		public sealed class NestedDiscriminatorDog : NestedDiscriminatorBase
		{
			public string? DogName { get; set; }
		}

		public sealed class NestedDiscriminatorCat : NestedDiscriminatorBase
		{
			public string? CatName { get; set; }
		}

		static MappingSchema BuildNestedDiscriminatorMappingSchema()
		{
			var ms = new MappingSchema();

			var b = new FluentMappingBuilder(ms);

			b.Entity<NestedDiscriminatorBase>()
				.HasTableName("NestedDiscriminator")
				.Inheritance(o => o.Meta.Kind, "Dog", typeof(NestedDiscriminatorDog))
				.Inheritance(o => o.Meta.Kind, "Cat", typeof(NestedDiscriminatorCat))
				.Property(o => o.Id).IsPrimaryKey().HasColumnName("Id")
				.Property(o => o.Meta.Kind).IsDiscriminator().HasColumnName("Kind").HasLength(20);

			b.Entity<NestedDiscriminatorDog>()
				.HasTableName("NestedDiscriminator")
				.Property(o => o.DogName).HasColumnName("DogName").HasLength(40);

			b.Entity<NestedDiscriminatorCat>()
				.HasTableName("NestedDiscriminator")
				.Property(o => o.CatName).HasColumnName("CatName").HasLength(40);

			b.Build();

			return ms;
		}

		[Test]
		public void ComplexProperty_NestedDiscriminator([DataSources] string context)
		{
			using var db    = GetDataContext(context, BuildNestedDiscriminatorMappingSchema());
			using var table = db.CreateLocalTable<NestedDiscriminatorBase>();

			db.Insert(new NestedDiscriminatorDog { Id = 1, DogName = "Rex", Meta = new() { Kind = "Dog" } });
			db.Insert(new NestedDiscriminatorCat { Id = 2, CatName = "Tom", Meta = new() { Kind = "Cat" } });

			var dogs = table.OfType<NestedDiscriminatorDog>().OrderBy(d => d.Id).ToList();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(dogs,            Has.Count.EqualTo(1));
				Assert.That(dogs[0].Id,      Is.EqualTo(1));
				Assert.That(dogs[0].DogName, Is.EqualTo("Rex"));
			}
		}
	}
}
