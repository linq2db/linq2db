using NUnit.Framework;

namespace Tests.UserTests
{
	using LinqToDB;
	using LinqToDB.Mapping;

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

			var ed1 = ms.GetEntityDescriptor(typeof(FluentBase), null);
			var ed2 = ms.GetEntityDescriptor(typeof(FluentDerived), null);
			var ed3 = ms.GetEntityDescriptor(typeof(AttributeBase), null);
			var ed4 = ms.GetEntityDescriptor(typeof(AttributeBase), null);

			Assert.AreEqual(ed1.Name.Name, ed2.Name.Name);
			Assert.AreEqual(ed3.Name.Name, ed4.Name.Name);
			Assert.AreEqual(ed1.Name.Name, ed4.Name.Name);
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
					Assert.AreEqual(1, res);
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
					Assert.AreEqual(1, res);
			}
		}
	}
}
