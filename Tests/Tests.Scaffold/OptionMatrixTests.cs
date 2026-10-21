using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.DataModel;
using LinqToDB.Extensions;
using LinqToDB.Metadata;
using LinqToDB.SqlQuery;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class OptionMatrixTests
	{
		public sealed record OptionSet(string Name, bool Nrt, MetadataSource Metadata, bool AllFindMethods, bool Equatable, bool SchemaAsType)
		{
			public override string ToString() => Name;
		}

		const FindTypes AllFindTypes =
			  FindTypes.FindByPkOnTable     | FindTypes.FindAsyncByPkOnTable     | FindTypes.FindQueryByPkOnTable
			| FindTypes.FindByRecordOnTable | FindTypes.FindAsyncByRecordOnTable | FindTypes.FindQueryByRecordOnTable
			| FindTypes.FindByPkOnContext   | FindTypes.FindAsyncByPkOnContext   | FindTypes.FindQueryByPkOnContext
			| FindTypes.FindByRecordOnContext | FindTypes.FindAsyncByRecordOnContext | FindTypes.FindQueryByRecordOnContext;

		static IEnumerable<TestCaseData> Combinations()
		{
			OptionSet[] combinations =
			[
				new("C1", Nrt: true , MetadataSource.Attributes   , AllFindMethods: false, Equatable: false, SchemaAsType: true ),
				new("C2", Nrt: false, MetadataSource.Attributes   , AllFindMethods: true , Equatable: true , SchemaAsType: false),
				new("C3", Nrt: true , MetadataSource.FluentMapping, AllFindMethods: true , Equatable: false, SchemaAsType: false),
				new("C4", Nrt: false, MetadataSource.FluentMapping, AllFindMethods: false, Equatable: true , SchemaAsType: true ),
				new("C5", Nrt: true , MetadataSource.None         , AllFindMethods: false, Equatable: true , SchemaAsType: false),
				new("C6", Nrt: false, MetadataSource.None         , AllFindMethods: true , Equatable: false, SchemaAsType: true ),
			];

			foreach (var language in Enum.GetValues<ScaffoldLanguage>())
				foreach (var combination in combinations)
					yield return new TestCaseData(language, combination).SetArgDisplayNames(language.ToString(), combination.Name);
		}

		static TestSchema CreateSchema()
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable(
				"Parent",
				[
					Col("Id"     , "int"),
					Col("Name"   , "text"            , nullable: true),
					Col("Created", "datetime"),
					Col("Amount" , "decimal"         , nullable: true, precision: 10, scale: 2),
					Col("Flag"   , "bit"),
					Col("Data"   , "blob"            , nullable: true),
					Col("Uid"    , "uniqueidentifier"),
				],
				primaryKey: ["Id"],
				identity  : "Id");

			schema.AddTable(
				"Child",
				[
					Col("Id"      , "int"),
					Col("ParentId", "int" , nullable: true),
					Col("Value"   , "text"),
				],
				primaryKey: ["Id"]);

			schema.AddForeignKey("FK_Child_Parent", new SqlObjectName("Child"), new SqlObjectName("Parent"), ("ParentId", "Id"));

			schema.AddTable(new SqlObjectName("Item", Database: "s2", Schema: "s2"), [Col("Id", "int"), Col("Code", "text")], primaryKey: ["Id"]);

			return schema;
		}

		[TestCaseSource(nameof(Combinations))]
		public void Combination(ScaffoldLanguage language, OptionSet combination)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(OptionMatrixTests)}_{combination.Name}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(),
				options =>
				{
					options.CodeGeneration.EnableNullableReferenceTypes = combination.Nrt;
					options.DataModel.Metadata                          = combination.Metadata;
					options.DataModel.GenerateIEquatable                = combination.Equatable;
					options.DataModel.GenerateSchemaAsType              = combination.SchemaAsType;
					options.DataModel.IncludeDatabaseName               = true;

					if (combination.AllFindMethods)
						options.DataModel.GenerateFindExtensions = AllFindTypes;
				});

			var parent = model.GetType("Parent");

			if (combination.Equatable)
			{
				parent.GetInterfaces().ShouldContain(typeof(IEquatable<>).MakeGenericType(parent));

				if (language == ScaffoldLanguage.FSharp)
					parent.GetAttributes<Microsoft.FSharp.Core.CustomEqualityAttribute>(false).ShouldNotBeEmpty();
			}

			if (combination.Metadata == MetadataSource.None)
				return;

			using var db = new SqliteTestDb("s2");

			var report = ModelSmoke.Run(model, db.Options);

			report.Tables.ShouldBe(3);
			report.Associations.ShouldBe(2);
			report.FindMethods.Count.ShouldBe(combination.AllFindMethods ? 3 * 12 : 3 * 2, string.Join("\n", report.FindMethods));

			if (combination.Equatable && language == ScaffoldLanguage.CSharp)
			{
				using var context = model.CreateContext(db.Options);

				var first  = ModelSmoke.CreateRow(context, parent, 0);
				var second = ModelSmoke.CreateRow(context, parent, 0);

				ReferenceEquals(first, second).ShouldBeFalse();
				first.Equals(second).ShouldBeTrue();
			}
		}
	}
}
