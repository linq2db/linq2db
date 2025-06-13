using System;
using System.Linq;

using JetBrains.Annotations;

using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.Mapping
{
	public class UseMappingSchemaTests : TestBase
	{
		[UsedImplicitly]
		class UseMappingSchemaTestTable
		{
			public int Field1 { get; set; }
			public int Field2 { get; set; }
			[Column(Name = "Column3")]
			public int Field3 { get; set; }
		}

		[Test]
		public void Test1([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			using (var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>())
			{
				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));
			}

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<UseMappingSchemaTestTable>()
					.Property(e => e.Field1)
						.HasColumnName("Column1")
				.Build()
				.MappingSchema))
			using (var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>())
			{
				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Column1"));
			}

			using (var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>())
			{
				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));
			}
		}

		[Test]
		public void Test2([DataSources] string context)
		{
			using var db  = GetDataContext(context);

			using var tmp = db.CreateLocalTable<UseMappingSchemaTestTable>();

			_ = tmp.ToList();

			Assert.That(LastQuery, Does.Contain("Field1"));

			using (db.UseMappingSchema(new FluentMappingBuilder(new())
				.Entity<UseMappingSchemaTestTable>()
					.HasTableName("UseMappingSchemaTestTable1")
					.Property(e => e.Field1)
						.HasColumnName("Column1")
				.Build()
				.MappingSchema))
			{
				using var tmp2 = db.CreateLocalTable<UseMappingSchemaTestTable>();

				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));

				_ = tmp2.ToList();

				Assert.That(LastQuery, Does.Contain("Column1"));

				_ = tmp.ToList();

				Assert.That(LastQuery, Does.Contain("Field1"));
			}

			using var tmp3 = db.CreateLocalTable<UseMappingSchemaTestTable>();

			_ = tmp.ToList();

			Assert.That(LastQuery, Does.Contain("Field1"));
		}
	}
}
