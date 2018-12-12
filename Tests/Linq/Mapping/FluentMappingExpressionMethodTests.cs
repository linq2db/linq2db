using System.Linq;
using LinqToDB;
using LinqToDB.Mapping;
using NUnit.Framework;
using Tests.Tools;

namespace Tests.Playground
{
	[TestFixture]
	public class FluentMappingExpressionMethodTests : TestBase
	{
		class InstanceClass
		{
			public int    Id       { get; set; }
			public int    Value    { get; set; }

			public string EntityValue => Id.ToString() + Value;
			public string EntityMaterialized { get; set; }
		}

		MappingSchema CreateMappingSchema()
		{
			var schema = new MappingSchema();
			var fluent = schema.GetFluentMappingBuilder();

			fluent.Entity<InstanceClass>().IsColumnRequired()
				.IsColumnRequired()
				.Property(e => e.Id)
				.Property(e => e.Value)
				.Member(e => e.EntityValue).IsExpression(e => e.Id.ToString() + e.Value.ToString())
				.Member(e => e.EntityMaterialized).IsExpression(e => "M" + e.Id.ToString(), true);

			return fluent.MappingSchema;
		}

		InstanceClass[] GenerateData()
		{
			return Enumerable.Range(1, 20)
				.Select(i => new InstanceClass { Id = i, Value = 100 + i }).ToArray();
		}

		[Test]
		public void ExpressionMethodOnProperty([DataSources] string context)
		{
			using (var db = GetDataContext(context, CreateMappingSchema()))
			{
				var testData = GenerateData();
				using (var table = db.CreateLocalTable(testData))
				{
					Assert.AreEqual(testData.Length,
						table.Where(t => Sql.AsNotNull(t.EntityValue) == t.Id.ToString() + t.Value).Count());
				}
			}
		}

		[Test]
		public void ExpressionMethodAsColumn([DataSources] string context)
		{
			using (var db = GetDataContext(context, CreateMappingSchema()))
			{
				var testData = GenerateData();
				using (var table = db.CreateLocalTable(testData))
				{
					var meterialized = table.ToArray();
					var expected = meterialized.Select(e => new InstanceClass
						{ Id = e.Id, Value = e.Value, EntityMaterialized = "M" + e.Id.ToString() });
					
					AreEqual(expected, meterialized, ComparerBuilder<InstanceClass>.GetEqualityComparer());
				}
			}
		}
	}
}
