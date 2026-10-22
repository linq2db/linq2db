using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class IdentifierTests
	{
		public sealed record NameCase(string Name, string Table, params string[] Columns)
		{
			public override string ToString() => Name;
		}

		static IEnumerable<TestCaseData> NameCases()
		{
			NameCase[] cases =
			[
				new("CSharpKeywords"              , "class"   , "event", "namespace"),
				new("FSharpKeywords"              , "type"    , "mod", "val", "end", "base"),
				new("NameWithSpace"               , "my table", "my col"),
				new("LeadingDigit"                , "Data"    , "1st"),
				new("NonAscii"                    , "Data"    , "Größe"),
				new("ColumnNamedAsEntity"         , "Person"  , "Person"),
				new("ColumnNamedEmpty"            , "Data"    , "Empty"),
				new("ColumnNamedAsContext"        , "Data"    , "TestDataContext"),
			];

			foreach (var language in Enum.GetValues<ScaffoldLanguage>())
				foreach (var nameCase in cases)
					yield return new TestCaseData(language, nameCase).SetArgDisplayNames(language.ToString(), nameCase.Name);
		}

		static void RawNames(ScaffoldOptions options)
		{
			options.DataModel.EntityClassNameOptions           = new() { Casing = NameCasing.None, Pluralization = Pluralization.None };
			options.DataModel.EntityColumnPropertyNameOptions  = new() { Casing = NameCasing.None };
			options.DataModel.EntityContextPropertyNameOptions = new() { Casing = NameCasing.None, Pluralization = Pluralization.None };
		}

		[TestCaseSource(nameof(NameCases))]
		public void RoundTrip(ScaffoldLanguage language, NameCase nameCase)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddTable(nameCase.Table, [Col("Id", "int"), .. nameCase.Columns.Select(static c => Col(c, "text"))], primaryKey: ["Id"]);

			var model = ScaffoldHarness.Build($"{nameof(IdentifierTests)}_{nameCase.Name}", language, ProviderProfile.SQLite, schema, RawNames);

			using var db = new SqliteTestDb();

			var report = ModelSmoke.Run(model, db.Options);

			report.Tables.ShouldBe(1);
			report.Rows.ShouldBe(ModelSmoke.RowCount);
		}

		[Test]
		public void KeywordSchema([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddTable(new SqlObjectName("Data", Database: "class", Schema: "class"), [Col("Id", "int"), Col("Value", "text")], primaryKey: ["Id"]);

			var model = ScaffoldHarness.Build(
				nameof(KeywordSchema),
				language,
				ProviderProfile.SQLite,
				schema,
				options =>
				{
					RawNames(options);
					options.DataModel.IncludeDatabaseName = true;
				});

			using var db = new SqliteTestDb("class");

			ModelSmoke.Run(model, db.Options).Rows.ShouldBe(ModelSmoke.RowCount);
		}

		[Test]
		public void TablesNormalizingToSameName([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddTable("user_info", [Col("Id", "int"), Col("Value", "text")], primaryKey: ["Id"]);
			schema.AddTable("UserInfo" , [Col("Id", "int"), Col("Code" , "int" )], primaryKey: ["Id"]);

			var model = ScaffoldHarness.Build(nameof(TablesNormalizingToSameName), language, ProviderProfile.SQLite, schema);

			using var db = new SqliteTestDb();

			var report = ModelSmoke.Run(model, db.Options);

			report.Tables.ShouldBe(2);
			model.Types.Count(static t => t.Name.StartsWith("UserInfo", StringComparison.Ordinal)).ShouldBe(2);
		}

		[Test]
		[ActiveIssue(4831, Arguments = "CSharp", ErrorMessage = "CS0246")]
		public void ViewNamedTable_Issue4831([Values] ScaffoldLanguage language)
		{
			var schema = ProviderProfile.SQLite.CreateSchema();
			schema.AddView(new SqlObjectName("Table"), [Col("Id", "int"), Col("Value", "text", nullable: true)]);

			var model = ScaffoldHarness.Build(nameof(ViewNamedTable_Issue4831), language, ProviderProfile.SQLite, schema);

			using var db = new SqliteTestDb();

			ModelSmoke.Run(model, db.Options).Rows.ShouldBe(ModelSmoke.RowCount);
		}
	}
}
