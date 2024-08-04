using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using LinqToDB.Data;
using LinqToDB.EntityFrameworkCore.BaseTests.Models.ForMapping;
using LinqToDB.Mapping;
using NUnit.Framework;

namespace LinqToDB.EntityFrameworkCore.BaseTests
{
	public abstract class ForMappingTestsBase : TestsBase
	{
		protected abstract ForMappingContextBase CreateContext(Func<DataOptions, DataOptions>? optionsSetter = null);

		[Test]
		public virtual void TestIdentityMapping()
		{
			using var context = CreateContext();
			using var connection = context.CreateLinqToDBConnection();

			var ed = connection.MappingSchema.GetEntityDescriptor(typeof(WithIdentity));
			var pk = ed.Columns.Single(c => c.IsPrimaryKey);

			pk.IsIdentity.Should().BeTrue();
		}

		[Test]
		public virtual void TestNoIdentityMapping()
		{
			using var context = CreateContext();
			using var connection = context.CreateLinqToDBConnection();

			var ed = connection.MappingSchema.GetEntityDescriptor(typeof(NoIdentity));
			var pk = ed.Columns.Single(c => c.IsPrimaryKey);

			pk.IsIdentity.Should().BeFalse();
		}

		[Test]
		public virtual void TestTableCreation()
		{
			using var context = CreateContext();
			using var connection = context.CreateLinqToDBConnection();

			using var t1 = connection.CreateTempTable<WithIdentity>();
			using var t2 = connection.CreateTempTable<NoIdentity>();
		}


		[Test]
		public virtual void TestBulkCopyNoIdentity()
		{
			using var context = CreateContext();
			using var connection = context.CreateLinqToDBConnection();

			using var t = connection.CreateTempTable<NoIdentity>();

			var items = new List<NoIdentity>()
			{
				new() {Id = Guid.NewGuid(), Name = "John Doe"},
				new() {Id = Guid.NewGuid(), Name = "Jane Doe"}
			};

			t.BulkCopy(items);

			items.Should().BeEquivalentTo(t);
		}

		[Test]
		public virtual void TestBulkCopyWithIdentity()
		{
			using var context = CreateContext();
			using var connection = context.CreateLinqToDBConnection();

			using var t = connection.CreateTempTable<WithIdentity>();

			var items = new List<WithIdentity>()
			{
				new() {Id = 1, Name = "John Doe"},
				new() {Id = 2, Name = "Jane Doe"}
			};

			t.BulkCopy(items);


			t.Should().HaveCount(items.Count);
		}

		[Test]
		public virtual async Task TestUIntTable()
		{
			using var context = CreateContext();
			context.UIntTable.Add(new UIntTable
			{
				Field16 = 1,
				Field16N = 2,
				Field32 = 3,
				Field32N = 4,
				Field64 = 5,
				Field64N = 6
			});

			await context.SaveChangesAsync();

			ulong field64 = 5;
			var item = await context.UIntTable.FirstOrDefaultAsyncLinqToDB(e => e.Field64 == field64);
		}

		[Test]
		public virtual void TestAmbiguousProperties()
		{
			using var context = CreateContext();

			FluentActions.Invoking(() =>  context.WithDuplicateProperties.Where(x => x.Value == 1)
				.ToArray()).Should().NotThrow();
		}

		[Test]
		public virtual void TestMappingSchemaCached()
		{
			using var context1 = CreateContext();
			using var context2 = CreateContext();
			using var connection1 = context1.CreateLinqToDBConnection();
			using var connection2 = context2.CreateLinqToDBConnection();

			Assert.That(connection2.MappingSchema, Is.EqualTo(connection1.MappingSchema));
		}

		protected sealed class TestEntity
		{
			public int Field { get; set; }
		}

		[Test]
		public virtual void TestMappingSchemaCachedWithCustomSchema()
		{
			var ms = new MappingSchema("Test");
			new FluentMappingBuilder(ms)
				.Entity<TestEntity>()
				.HasPrimaryKey(e => e.Field)
				.Build();

			using var context1 = CreateContext(o => o.UseMappingSchema(ms));
			using var context2 = CreateContext(o => o.UseMappingSchema(ms));
			using var connection1 = context1.CreateLinqToDBConnection();
			using var connection2 = context2.CreateLinqToDBConnection();

			Assert.That(connection2.MappingSchema, Is.EqualTo(connection1.MappingSchema));

			// check EF mapping is in place
			var ed = connection1.MappingSchema.GetEntityDescriptor(typeof(WithIdentity));
			var pk = ed.Columns.Single(c => c.IsPrimaryKey);
			pk.IsIdentity.Should().BeTrue();

			// check additional mapping also used
			ed = connection1.MappingSchema.GetEntityDescriptor(typeof(TestEntity));
			pk = ed.Columns.Single(c => c.IsPrimaryKey);
			pk.IsIdentity.Should().BeFalse();
			Assert.That(pk.ColumnName, Is.EqualTo("Field"));
		}

		[Test]
		public virtual async Task TestInheritance()
		{
			using var context = CreateContext();
			using var connection = context.CreateLinqToDBConnection();
			
			context.WithInheritance.AddRange(new List<WithInheritanceA>() { new() { } });
			context.WithInheritance.AddRange(new List<WithInheritanceA1>() { new() { }, new() { } });
			context.WithInheritance.AddRange(new List<WithInheritanceA2>() { new() { }, new() { } });
			await context.SaveChangesAsync();

			var result = context.GetTable<WithInheritanceA>().ToList();
			
			result.OfType<WithInheritance>().Should().HaveCount(5);
			result.OfType<WithInheritanceA1>().Should().HaveCount(2);
			result.OfType<WithInheritanceA2>().Should().HaveCount(2);
		}
	}
}
