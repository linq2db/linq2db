using System;
using System.Linq;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class NullabilityTests
	{
		static TestSchema CreateSchema()
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable(
				"Item",
				[
					Col("Id"              , "int"),
					Col("NullableInt"     , "int"     , nullable: true),
					Col("NotNullText"     , "text"),
					Col("NullableText"    , "text"    , nullable: true),
					Col("NullableDate"    , "datetime", nullable: true),
					Col("NullableBlob"    , "blob"    , nullable: true),
				],
				primaryKey: ["Id"]);

			return schema;
		}

		// zero warnings are enforced by compiler, so nullability annotation regressions (CS86xx, FS3261) fail here
		[Test]
		public void RoundTrip([Values] ScaffoldLanguage language, [Values] bool nrt)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(NullabilityTests)}_{nrt}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(),
				options => options.CodeGeneration.EnableNullableReferenceTypes = nrt);

			using var db = new SqliteTestDb();

			ModelSmoke.Run(model, db.Options).Rows.ShouldBe(ModelSmoke.RowCount);

			var entity = model.GetType("Item");

			if (language == ScaffoldLanguage.FSharp)
			{
				entity.GetProperty("NullableInt")!.PropertyType.ShouldBe(typeof(Microsoft.FSharp.Core.FSharpOption<int>));
				entity.GetProperty("NullableText")!.PropertyType.ShouldBe(typeof(Microsoft.FSharp.Core.FSharpOption<string>));
				entity.GetProperty("NotNullText")!.PropertyType.ShouldBe(typeof(string));
			}
			else
			{
				entity.GetProperty("NullableInt")!.PropertyType.ShouldBe(typeof(int?));

				var context = new System.Reflection.NullabilityInfoContext();

				context.Create(entity.GetProperty("NullableText")!).ReadState.ShouldBe(nrt ? System.Reflection.NullabilityState.Nullable    : System.Reflection.NullabilityState.Unknown);
				context.Create(entity.GetProperty("NotNullText" )!).ReadState.ShouldBe(nrt ? System.Reflection.NullabilityState.NotNull     : System.Reflection.NullabilityState.Unknown);
			}
		}
	}
}
