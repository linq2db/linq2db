using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using FluentAssertions;

using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.EntityFrameworkCore.Tests.Models.ForMapping;
using LinqToDB.Mapping;

using Microsoft.EntityFrameworkCore;

using NUnit.Framework;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests
{
	public class ForMappingTests : ContextTestBase<ForMappingContextBase>
	{
		protected override ForMappingContextBase CreateProviderContext(string provider, DbContextOptions<ForMappingContextBase> options)
		{
			return provider switch
			{
				_ when provider.IsAnyOf(TestProvName.AllPostgreSQL) => new PostgreSQL.Models.ForMapping.ForMappingContext(options),
				_ when provider.IsAnyOf(TestProvName.AllMySql) => new Pomelo.Models.ForMapping.ForMappingContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSQLite) => new SQLite.Models.ForMapping.ForMappingContext(options),
				_ when provider.IsAnyOf(TestProvName.AllSqlServer) => new SqlServer.Models.ForMapping.ForMappingContext(options),
				_ => throw new InvalidOperationException($"{nameof(CreateProviderContext)} is not implemented for provider {provider}")
			};
		}

		[Test]
		public virtual void TestIdentityMapping([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
			using var connection = context.CreateLinqToDBConnection();

			var ed = connection.MappingSchema.GetEntityDescriptor(typeof(WithIdentity));
			var pk = ed.Columns.Single(c => c.IsPrimaryKey);

			pk.IsIdentity.Should().BeTrue();
		}

		[Test]
		public virtual void TestNoIdentityMapping([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
			using var connection = context.CreateLinqToDBConnection();

			var ed = connection.MappingSchema.GetEntityDescriptor(typeof(NoIdentity));
			var pk = ed.Columns.Single(c => c.IsPrimaryKey);

			pk.IsIdentity.Should().BeFalse();
		}

		[Test]
		public virtual void TestTableCreation([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
			using var connection = context.CreateLinqToDBConnection();

			using var t1 = connection.CreateTempTable<WithIdentity>();
			using var t2 = connection.CreateTempTable<NoIdentity>();
		}

		[Test]
		public virtual void TestBulkCopyNoIdentity([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
			using var connection = context.CreateLinqToDBConnection();

			using var t = connection.CreateTempTable<NoIdentity>();

			var items = new List<NoIdentity>()
			{
				new() {Id = TestData.Guid1, Name = "John Doe"},
				new() {Id = TestData.Guid2, Name = "Jane Doe"}
			};

			t.BulkCopy(items);

			items.Should().BeEquivalentTo(t);
		}

		// postgres: cannot create such identity table
		[Test]
		public virtual void TestBulkCopyWithIdentity([EFDataSources(TestProvName.AllPostgreSQL)] string provider)
		{
			using var context = CreateContext(provider);
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
		public virtual async Task TestUIntTable([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
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
		public virtual void TestAmbiguousProperties([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);

			FluentActions.Invoking(() =>  context.WithDuplicateProperties.Where(x => x.Value == 1)
				.ToArray()).Should().NotThrow();
		}

		[Test]
		public virtual void TestMappingSchemaCached([EFDataSources] string provider)
		{
			using var context1 = CreateContext(provider);
			using var context2 = CreateContext(provider);
			using var connection1 = context1.CreateLinqToDBConnection();
			using var connection2 = context2.CreateLinqToDBConnection();

			Assert.That(connection2.MappingSchema, Is.EqualTo(connection1.MappingSchema));
		}

		sealed class TestEntity
		{
			public int Field { get; set; }
		}

		[Test]
		public virtual void TestMappingSchemaCachedWithCustomSchema([EFDataSources] string provider)
		{
			var ms = new MappingSchema("Test");
			new FluentMappingBuilder(ms)
				.Entity<TestEntity>()
				.HasPrimaryKey(e => e.Field)
				.Build();

			using var context1 = CreateContext(provider, o => o.UseMappingSchema(ms));
			using var context2 = CreateContext(provider, o => o.UseMappingSchema(ms));
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
		public virtual async Task TestInheritance([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
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

		[Test]
		public void TestStringMappings([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using (var db = CreateContext(provider))
			{
				var ms = LinqToDBForEFTools.GetMappingSchema(db.Model, db, null);
				var ed = ms.GetEntityDescriptor(typeof(StringTypes));

				ed.Columns.First(c => c.MemberName == nameof(StringTypes.AsciiString)).DataType.Should()
					.Be(DataType.VarChar);

				ed.Columns.First(c => c.MemberName == nameof(StringTypes.UnicodeString)).DataType.Should()
					.Be(DataType.NVarChar);
			}
		}

		[Test(Description = "https://github.com/linq2db/linq2db.EntityFrameworkCore/issues/349")]
		public void TestColumnLengthMappings([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using (var db = CreateContext(provider))
			{
				var ms = LinqToDBForEFTools.GetMappingSchema(db.Model, db, null);
				var ed = ms.GetEntityDescriptor(typeof(TypesTable));

				ed.Columns.First(c => c.MemberName == nameof(TypesTable.DateTime)).Length.Should().BeNull();
				ed.Columns.First(c => c.MemberName == nameof(TypesTable.String)).Length.Should().Be(100);
			}
		}

		[Test]
		public void TestDialectUse([EFIncludeDataSources(TestProvName.AllSqlServer)] string provider)
		{
			using var db = CreateContext(provider, o => o.UseSqlServer(SqlServerVersion.v2005));
			using var dc = db.CreateLinqToDBConnectionDetached();
			Assert.That(dc.MappingSchema.DisplayID, Does.Contain("2005"));
		}

		[Test]
		public virtual async Task TestSkipModes([EFDataSources] string provider)
		{
			using var context = CreateContext(provider);
			using var connection = context.CreateLinqToDBConnection();

			var entityEF = new SkipModesTable() { Id = 1, InsertOnly = 2, UpdateOnly = 3, ReadOnly = 4 };
			var entityL2D = new SkipModesTable() { Id = 2, InsertOnly = 2, UpdateOnly = 3, ReadOnly = 4 };
			context.SkipModes.Add(entityEF);
			await context.SaveChangesAsync();
			connection.Insert(entityL2D);

			var result = context.GetTable<SkipModesTable>().OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].InsertOnly, Is.EqualTo(2));
				Assert.That(result[0].UpdateOnly, Is.Null);
				Assert.That(result[0].ReadOnly, Is.Null);
				Assert.That(result[1].InsertOnly, Is.EqualTo(2));
				Assert.That(result[1].UpdateOnly, Is.Null);
				Assert.That(result[1].ReadOnly, Is.Null);
			});

			entityEF.InsertOnly = 11;
			entityEF.UpdateOnly = 12;
			entityEF.ReadOnly = 13;
			entityL2D.InsertOnly = 11;
			entityL2D.UpdateOnly = 12;
			entityL2D.ReadOnly = 13;

			await context.SaveChangesAsync();
			connection.Update(entityL2D);

			result = context.GetTable<SkipModesTable>().OrderBy(r => r.Id).ToArray();

			Assert.That(result, Has.Length.EqualTo(2));
			Assert.Multiple(() =>
			{
				Assert.That(result[0].InsertOnly, Is.EqualTo(2));
				Assert.That(result[0].UpdateOnly, Is.EqualTo(12));
				Assert.That(result[0].ReadOnly, Is.Null);
				Assert.That(result[1].InsertOnly, Is.EqualTo(2));
				Assert.That(result[1].UpdateOnly, Is.EqualTo(12));
				Assert.That(result[1].ReadOnly, Is.Null);
			});
		}
	}
}
