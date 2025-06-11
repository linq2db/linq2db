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
		}

		[Test]
		public void Test([DataSources] string context)
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
	}
}
