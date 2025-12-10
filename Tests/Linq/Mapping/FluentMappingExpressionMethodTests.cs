using System.Linq;

using LinqToDB;
using LinqToDB.Internal.SqlQuery;
using LinqToDB.Mapping;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Mapping
{
	[TestFixture]
	public class FluentMappingExpressionMethodTests : TestBase
	{
		sealed class InstanceClass
		{
			[PrimaryKey]
			public int    Id       { get; set; }
			public int    Value    { get; set; }

			public string EntityValue => Id.ToString() + Value;
			public string? EntityMaterialized { get; set; }
		}

		MappingSchema CreateMappingSchema()
		{
			var schema = new MappingSchema();
			var fluent = new FluentMappingBuilder(schema);

			fluent.Entity<InstanceClass>().IsColumnRequired()
				.IsColumnRequired()
				.Property(e => e.Id)
				.Property(e => e.Value)
				.Member(e => e.EntityValue).IsExpression(e => e.Id.ToString() + e.Value.ToString())
				.Member(e => e.EntityMaterialized).IsExpression(e => "M" + e.Id.ToString(), true)
				.Build();

			return fluent.MappingSchema;
		}

		InstanceClass[] GenerateData()
		{
			return Enumerable.Range(1, 20)
				.Select(i => new InstanceClass { Id = i, Value = 100 + i }).ToArray();
		}

		[ActiveIssue(
			Details = "https://github.com/linq2db/linq2db/issues/4987",
			SkipForLinqService = true,
			Configurations = [
				TestProvName.AllSQLite,
				TestProvName.AllSapHana,
				TestProvName.AllPostgreSQL,
				TestProvName.AllOracle,
				TestProvName.AllDB2,
				TestProvName.AllInformix,
				TestProvName.AllClickHouse,
				TestProvName.AllAccess,
				TestProvName.AllMySql])]
		[Test]
		public void ExpressionMethodOnProperty([DataSources] string context)
		{
			using var db = GetDataContext(context, CreateMappingSchema());
			var testData = GenerateData();
			using var table = db.CreateLocalTable(testData);
			var query = table.Where(t => Sql.AsNotNull(t.EntityValue) == t.Id.ToString() + t.Value);

			Assert.That(query.Count(), Is.EqualTo(testData.Length));

			if (!context.IsRemote())
			{
				var where = query.GetSelectQuery().Select.Where;

				if (!where.IsEmpty)
				{
					Assert.That(where.SearchCondition.Predicates, Has.Count.EqualTo(1));
					Assert.That(where.SearchCondition.Predicates[0], Is.InstanceOf<SqlPredicate.TruePredicate>());
				}
			}
		}

		[Test]
		public void ExpressionMethodAsColumn([DataSources] string context)
		{
			using var db = GetDataContext(context, CreateMappingSchema());
			var testData = GenerateData();

			using var table = db.CreateLocalTable(testData);
			var meterialized = table.ToArray();
			var expected     = meterialized.Select(e => new InstanceClass
			{ Id = e.Id, Value = e.Value, EntityMaterialized = "M" + e.Id.ToString() });

			AreEqualWithComparer(expected, meterialized);
		}
	}
}
