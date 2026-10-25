using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class AssociationTests
	{
		static ModelSmoke.Report Run(string name, ScaffoldLanguage language, TestSchema schema, Action<LinqToDB.Scaffold.ScaffoldOptions>? configure = null, params string[] attached)
		{
			var model = ScaffoldHarness.Build($"{nameof(AssociationTests)}_{name}", language, ProviderProfile.SQLite, schema, configure);

			using var db = new SqliteTestDb(attached);

			return ModelSmoke.Run(model, db.Options);
		}

		static TestSchema ParentChild(bool nullableForeignKey)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("Parent", [Col("Id", "int"), Col("Name", "text")], primaryKey: ["Id"]);
			schema.AddTable("Child" , [Col("Id", "int"), Col("ParentId", "int", nullable: nullableForeignKey)], primaryKey: ["Id"]);
			schema.AddForeignKey("FK_Child_Parent", new SqlObjectName("Child"), new SqlObjectName("Parent"), ("ParentId", "Id"));

			return schema;
		}

		[Test]
		public void OneToMany([Values] ScaffoldLanguage language)
		{
			// many-to-one and backreference
			Run(nameof(OneToMany), language, ParentChild(false)).Associations.ShouldBe(2);
		}

		[Test]
		public void NullableForeignKey([Values] ScaffoldLanguage language)
		{
			Run(nameof(NullableForeignKey), language, ParentChild(true)).Associations.ShouldBe(2);
		}

		[Test]
		public void OneToOne([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("Person" , [Col("Id", "int"), Col("Name" , "text")], primaryKey: ["Id"]);
			schema.AddTable("Profile", [Col("Id", "int"), Col("Email", "text")], primaryKey: ["Id"]);
			schema.AddForeignKey("FK_Profile_Person", new SqlObjectName("Profile"), new SqlObjectName("Person"), ("Id", "Id"));

			Run(nameof(OneToOne), language, schema).Associations.ShouldBe(2);
		}

		[Test]
		public void SelfReference([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("Node", [Col("Id", "int"), Col("ParentId", "int")], primaryKey: ["Id"]);
			schema.AddForeignKey("FK_Node_Node", new SqlObjectName("Node"), new SqlObjectName("Node"), ("ParentId", "Id"));

			Run(nameof(SelfReference), language, schema).Associations.ShouldBe(2);
		}

		[Test]
		public void TwoForeignKeysToSameTable([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("User"   , [Col("Id", "int"), Col("Name", "text")], primaryKey: ["Id"]);
			schema.AddTable("Message", [Col("Id", "int"), Col("FromId", "int"), Col("ToId", "int")], primaryKey: ["Id"]);
			schema.AddForeignKey("FK_Message_From", new SqlObjectName("Message"), new SqlObjectName("User"), ("FromId", "Id"));
			schema.AddForeignKey("FK_Message_To"  , new SqlObjectName("Message"), new SqlObjectName("User"), ("ToId"  , "Id"));

			Run(nameof(TwoForeignKeysToSameTable), language, schema).Associations.ShouldBe(4);
		}

		[Test]
		public void CompositeForeignKey([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable("Header", [Col("K1", "int"), Col("K2", "text"), Col("Name", "text")], primaryKey: ["K1", "K2"]);
			schema.AddTable("Line"  , [Col("Id", "int"), Col("HeaderK1", "int"), Col("HeaderK2", "text")], primaryKey: ["Id"]);
			schema.AddForeignKey("FK_Line_Header", new SqlObjectName("Line"), new SqlObjectName("Header"), ("HeaderK1", "K1"), ("HeaderK2", "K2"));

			Run(nameof(CompositeForeignKey), language, schema).Associations.ShouldBe(2);
		}

		[Test]
		public void CrossSchemaForeignKey([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			var child = new SqlObjectName("Child", Database: "s2", Schema: "s2");

			schema.AddTable("Parent", [Col("Id", "int"), Col("Name", "text")], primaryKey: ["Id"]);
			schema.AddTable(child   , [Col("Id", "int"), Col("ParentId", "int")], primaryKey: ["Id"]);
			schema.AddForeignKey("FK_Child_Parent", child, new SqlObjectName("Parent"), ("ParentId", "Id"));

			Run(nameof(CrossSchemaForeignKey), language, schema, options => options.DataModel.IncludeDatabaseName = true, "s2").Associations.ShouldBe(2);
		}

		static IEnumerable<TestCaseData> CollectionTypes()
		{
			foreach (var language in Enum.GetValues<ScaffoldLanguage>())
			{
				yield return new TestCaseData(language, true , null                                  , typeof(Array)).SetArgDisplayNames(language.ToString(), "array");
				yield return new TestCaseData(language, false, "System.Collections.Generic.List<>"   , typeof(List<>)).SetArgDisplayNames(language.ToString(), "List");
			}
		}

		[TestCaseSource(nameof(CollectionTypes))]
		public void CollectionType(ScaffoldLanguage language, bool asArray, string? collectionType, Type expected)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(CollectionType)}_{asArray}",
				language,
				ProviderProfile.SQLite,
				ParentChild(false),
				options =>
				{
					options.DataModel.AssociationCollectionAsArray = asArray;
					options.DataModel.AssociationCollectionType    = collectionType;
				});

			var children = model.GetType("Parent").GetProperties().Single(static p => p.Name.StartsWith("Child", StringComparison.Ordinal)).PropertyType;

			if (expected == typeof(Array))
				children.IsArray.ShouldBeTrue(children.ToString());
			else
				children.GetGenericTypeDefinition().ShouldBe(expected);

			using var db = new SqliteTestDb();

			ModelSmoke.Run(model, db.Options).Associations.ShouldBe(2);
		}
	}
}
