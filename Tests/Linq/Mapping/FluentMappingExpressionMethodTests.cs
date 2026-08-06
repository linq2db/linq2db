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

		#region IsExpression on a nested/complex property (regression for #4770)

		sealed class NestedExpr
		{
			public int             Id   { get; set; }
			public NestedExprPart? Part { get; set; }
		}

		sealed class NestedExprPart
		{
			public string? Source { get; set; }
			public string? Upper  { get; set; }
		}

		// Fluent IsExpression on a nested-property path (Part.Upper): the expression parameter is rebased
		// from the entity (NestedExpr) to the owner type (NestedExprPart) so Sql.Property(p, "Source")
		// resolves against the complex type. materialized = expose it as a calculated column.
		static MappingSchema BuildNestedExprSchema(bool materialized)
		{
			var ms = new MappingSchema();
			var fb = new FluentMappingBuilder(ms);

			var upper = fb.Entity<NestedExpr>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.Part!.Source)
				.Property(e => e.Part!.Upper).IsExpression(p => Sql.Upper(Sql.Property<string>(p, "Source")), materialized);

			if (materialized)
				upper.IsColumn();

			fb.Build();

			return ms;
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4770")]
		public void ExpressionMethodOnNestedProperty([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, BuildNestedExprSchema(materialized: true));
			using var tb = db.CreateLocalTable<NestedExpr>();

			db.Insert(new NestedExpr { Id = 1, Part = new NestedExprPart { Source = "abc" } });

			var row = tb.Single();

			Assert.That(row.Part, Is.Not.Null);
			Assert.That(row.Part!.Upper, Is.EqualTo("ABC"));
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4770")]
		public void ExpressionMethodOnNestedProperty_InFilter([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			using var db = GetDataContext(context, BuildNestedExprSchema(materialized: false));
			using var tb = db.CreateLocalTable<NestedExpr>();

			db.Insert(new NestedExpr { Id = 1, Part = new NestedExprPart { Source = "abc" } });
			db.Insert(new NestedExpr { Id = 2, Part = new NestedExprPart { Source = "xyz" } });

			var ids = tb.Where(e => e.Part!.Upper == "ABC").Select(e => e.Id).ToList();

			Assert.That(ids, Is.EqualTo(new[] { 1 }));
		}

		sealed class DeepNestedExpr
		{
			public int               Id  { get; set; }
			public DeepNestedExprMid? Mid { get; set; }
		}

		sealed class DeepNestedExprMid
		{
			public DeepNestedExprLeaf? Leaf { get; set; }
		}

		sealed class DeepNestedExprLeaf
		{
			public string? Source { get; set; }
			public string? Upper  { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4770")]
		public void ExpressionMethodOnDeeplyNestedProperty([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			var fb = new FluentMappingBuilder(ms);

			fb.Entity<DeepNestedExpr>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.Mid!.Leaf!.Source)
				.Property(e => e.Mid!.Leaf!.Upper).IsExpression(l => Sql.Upper(Sql.Property<string>(l, "Source")), true).IsColumn()
				.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<DeepNestedExpr>();

			db.Insert(new DeepNestedExpr { Id = 1, Mid = new DeepNestedExprMid { Leaf = new DeepNestedExprLeaf { Source = "abc" } } });

			var row = tb.Single();

			Assert.That(row.Mid?.Leaf, Is.Not.Null);
			Assert.That(row.Mid!.Leaf!.Upper, Is.EqualTo("ABC"));
		}

		sealed class MixedExpr
		{
			public int            Id        { get; set; }
			public string?        TopSource { get; set; }
			public string?        TopUpper  { get; set; }
			public MixedExprPart? Part      { get; set; }
		}

		sealed class MixedExprPart
		{
			public string? Source { get; set; }
			public string? Upper  { get; set; }
		}

		[Test(Description = "https://github.com/linq2db/linq2db/issues/4770")]
		public void ExpressionMethodOnNestedAndNonNestedProperty([IncludeDataSources(TestProvName.AllSQLite)] string context)
		{
			var ms = new MappingSchema();
			var fb = new FluentMappingBuilder(ms);

			fb.Entity<MixedExpr>()
				.Property(e => e.Id).IsPrimaryKey()
				.Property(e => e.TopSource)
				.Property(e => e.TopUpper).IsExpression(e => Sql.Upper(Sql.Property<string>(e, "TopSource")), true).IsColumn()
				.Property(e => e.Part!.Source)
				.Property(e => e.Part!.Upper).IsExpression(p => Sql.Upper(Sql.Property<string>(p, "Source")), true).IsColumn()
				.Build();

			using var db = GetDataContext(context, ms);
			using var tb = db.CreateLocalTable<MixedExpr>();

			db.Insert(new MixedExpr { Id = 1, TopSource = "abc", Part = new MixedExprPart { Source = "xyz" } });

			var row = tb.Single();

			Assert.That(row.TopUpper,    Is.EqualTo("ABC"));
			Assert.That(row.Part,        Is.Not.Null);
			Assert.That(row.Part!.Upper, Is.EqualTo("XYZ"));
		}
		#endregion
	}
}
