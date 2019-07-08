using System.Threading;
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

		class FluentDerived : FluentBase
		{
			public string StringValue { get; set; }
		}

		[Table(nameof(AttributeBase), IsColumnAttributeRequired = false)]
		class AttributeBase
		{
			[Column(IsPrimaryKey = true)]
			public int Id { get; set; }
		}

		class AttributeDerived : AttributeBase
		{
			public string StringValue { get; set; }
		}

		MappingSchema SetFluentMappings()
		{
			var ms            = new MappingSchema();
			var tableName     = nameof(AttributeBase);
			var fluentBuilder = ms.GetFluentMappingBuilder();

			fluentBuilder.Entity<FluentBase>()
				.HasTableName(tableName)
				.Property(x => x.Id).IsColumn().IsNullable(false).HasColumnName("Id").IsPrimaryKey();

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

			Assert.AreEqual(ed1.TableName, ed2.TableName);
			Assert.AreEqual(ed3.TableName, ed4.TableName);
			Assert.AreEqual(ed1.TableName, ed4.TableName);
		}

		[Test]
		public void TestFluent([DataSources] string context)
		{
			var ms = SetFluentMappings();

			using (var db = GetDataContext(context, ms))
			using (db.CreateLocalTable<FluentBase>())
			{
				var res = db.Insert<FluentBase>(new FluentDerived { Id = 1 });
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
				Assert.AreEqual(1, res);
			}
		}
	}
}
