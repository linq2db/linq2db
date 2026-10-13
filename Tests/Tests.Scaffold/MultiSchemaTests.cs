using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	// SQLite has no schemas: additional schemas are attached databases, mapped through database name.
	[TestFixture]
	public sealed class MultiSchemaTests
	{
		static TestSchema CreateSchema(ProviderProfile profile, bool sqlite)
		{
			var schema = profile.CreateSchema();

			schema.AddTable(new SqlObjectName("T", Schema: sqlite ? null : profile.DefaultSchema), [Col("Id", "int"), Col("Code", "text")], primaryKey: ["Id"]);
			schema.AddTable(new SqlObjectName("T", Database: sqlite ? "s2" : null, Schema: "s2"), [Col("Id", "int"), Col("Code", "text")], primaryKey: ["Id"]);

			return schema;
		}

		[Test]
		public void SameTableNameTwoSchemas([Values] ScaffoldLanguage language, [Values] bool schemaAsType)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(SameTableNameTwoSchemas)}_{schemaAsType}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(ProviderProfile.SQLite, true),
				options =>
				{
					options.DataModel.GenerateSchemaAsType = schemaAsType;
					options.DataModel.IncludeDatabaseName  = true;
				});

			using var sqlite = new SqliteTestDb("s2");

			ModelSmoke.Run(model, sqlite.Options).Tables.ShouldBe(2);

			// extra row only in attached database: each entity must read its own table
			sqlite.Execute("INSERT INTO \"s2\".\"T\" (\"Id\", \"Code\") VALUES (99, 'x')");

			using var db = model.CreateContext(sqlite.Options);

			var counts = model.GetTables(db)
				.Select(static t => ((IEnumerable)t.GetTable()).Cast<object>().Count())
				.OrderBy(static c => c)
				.ToList();

			counts.ShouldBe([ModelSmoke.RowCount, ModelSmoke.RowCount + 1]);

			var sqls = model.GetTables(db).Select(t => SqlShape.GetSql((IQueryable)t.GetTable())).ToList();

			sqls.Count(static s => s.Contains("\"s2\".", StringComparison.Ordinal) || s.Contains("[s2].", StringComparison.Ordinal)).ShouldBe(1, string.Join("\n", sqls));
		}

		[Test]
		public void SchemaMap([Values] ScaffoldLanguage language)
		{
			var model = ScaffoldHarness.Build(
				nameof(SchemaMap),
				language,
				ProviderProfile.SQLite,
				CreateSchema(ProviderProfile.SQLite, true),
				options =>
				{
					options.DataModel.GenerateSchemaAsType = true;
					options.DataModel.IncludeDatabaseName  = true;
					options.DataModel.SchemaMap["s2"]      = "Second";
				});

			model.Types.ShouldContain(static t => t.Name.StartsWith("Second", StringComparison.Ordinal), string.Join(", ", model.Types.Select(static t => t.Name)));

			using var sqlite = new SqliteTestDb("s2");

			ModelSmoke.Run(model, sqlite.Options).Tables.ShouldBe(2);
		}

		[Test]
		public void FSharpModulePerSchema()
		{
			var model = ScaffoldHarness.Build(
				nameof(FSharpModulePerSchema),
				ScaffoldLanguage.FSharp,
				ProviderProfile.SQLite,
				CreateSchema(ProviderProfile.SQLite, true),
				options =>
				{
					options.DataModel.GenerateSchemaAsType = true;
					options.DataModel.IncludeDatabaseName  = true;
				});

			// F# module is compiled to abstract sealed class
			model.Types.ShouldContain(static t => t.IsAbstract && t.IsSealed && t.Name.StartsWith("S2", StringComparison.Ordinal), string.Join(", ", model.Types.Select(static t => t.Name)));
		}

		// generation-only assertion: SQL Server and PostgreSQL are not available to this project
		[Test]
		public void SchemaQualifiedRendering([Values] ScaffoldLanguage language, [Values("SqlServer", "PostgreSQL")] string provider)
		{
			var profile = provider == "SqlServer" ? ProviderProfile.SqlServer : ProviderProfile.PostgreSQL;

			var model = ScaffoldHarness.Build(
				$"{nameof(SchemaQualifiedRendering)}_{provider}",
				language,
				profile,
				CreateSchema(profile, false),
				options => options.DataModel.GenerateSchemaAsType = true);

			using var db = model.CreateContext(profile.CreateOfflineOptions());

			var sqls = model.GetTables(db).Select(t => SqlShape.GetSql((IQueryable)t.GetTable())).ToList();

			var qualified = provider == "SqlServer" ? "[s2].[T]" : "s2.\"T\"";

			sqls.Count(s => s.Contains(qualified, StringComparison.Ordinal)).ShouldBe(1, string.Join("\n", sqls));
		}
	}
}
