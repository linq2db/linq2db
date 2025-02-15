using LinqToDB;
using LinqToDB.Mapping;

using NUnit.Framework;

namespace Tests.UserTests
{
	[TestFixture]
	public class Issue1128Tests : TestBase
	{
		class FluentBase
		{
			public int Id { get; set; }
		}

		sealed class FluentDerived : FluentBase
		{
			public string? StringValue { get; set; }
		}

		[Table(nameof(AttributeBase), IsColumnAttributeRequired = false)]
		class AttributeBase
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }
		}

		sealed class AttributeDerived : AttributeBase
		{
			public string? StringValue { get; set; }
		}

		MappingSchema SetFluentMappings()
		{
			var ms            = new MappingSchema();
			var tableName     = nameof(AttributeBase);
			var fluentBuilder = new FluentMappingBuilder(ms);

			fluentBuilder.Entity<FluentBase>()
				.HasTableName(tableName)
				.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey()
				.Build();

			return ms;
		}

		[Test]
		public void TestEntityDescriptor()
		{
			var ms = SetFluentMappings();

			var ed1 = ms.GetEntityDescriptor(typeof(FluentBase));
			var ed2 = ms.GetEntityDescriptor(typeof(FluentDerived));
			var ed3 = ms.GetEntityDescriptor(typeof(AttributeBase));
			var ed4 = ms.GetEntityDescriptor(typeof(AttributeBase));

			Assert.Multiple(() =>
			{
				Assert.That(ed2.Name.Name, Is.EqualTo(ed1.Name.Name));
				Assert.That(ed4.Name.Name, Is.EqualTo(ed3.Name.Name));
			});
			Assert.That(ed4.Name.Name, Is.EqualTo(ed1.Name.Name));
		}

		[Test]
		public void TestFluent([DataSources] string context)
		{
			var ms = SetFluentMappings();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<FluentBase>())
			{
				var res = db.Insert<FluentBase>(new FluentDerived { Id = 1 });
				if (!context.IsAnyOf(TestProvName.AllClickHouse))
					Assert.That(res, Is.EqualTo(1));
			}
		}

		[Test]
		public void TestAttribute([DataSources] string context)
		{
			var ms = SetFluentMappings();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<AttributeBase>())
			{
				var res = db.Insert<AttributeBase>(new AttributeDerived { Id = 1 });
				if (!context.IsAnyOf(TestProvName.AllClickHouse))
					Assert.That(res, Is.EqualTo(1));
			}
		}
	}
}
