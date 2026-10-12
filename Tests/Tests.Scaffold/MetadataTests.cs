using System;
using System.Linq;

using LinqToDB.Metadata;

using NUnit.Framework;

using Shouldly;

using Tests.Scaffold.Harness;

using static Tests.Scaffold.Harness.TestSchema;

namespace Tests.Scaffold
{
	[TestFixture]
	public sealed class MetadataTests
	{
		static TestSchema CreateSchema()
		{
			var schema = ProviderProfile.SQLite.CreateSchema();

			schema.AddTable(
				"person_info",
				[
					Col("person_id" , "int"),
					Col("first_name", "nvarchar", length: 50),
					Col("salary"    , "decimal" , nullable: true, precision: 18, scale: 4),
					Col("code"      , "varchar" , nullable: true, length: 10),
				],
				primaryKey: ["person_id"],
				identity  : "person_id");

			return schema;
		}

		static string Describe(ScaffoldedModel model)
		{
			using var sqlite = new SqliteTestDb();
			using var db     = model.CreateContext(sqlite.Options);

			var entity     = model.GetType("PersonInfo");
			var descriptor = db.MappingSchema.GetEntityDescriptor(entity);

			return string.Join(
				"\n",
				new[] { $"table: {descriptor.Name}" }
					.Concat(descriptor.Columns
						.OrderBy(static c => c.MemberName, StringComparer.Ordinal)
						.Select(static c => $"{c.MemberName}: {c.ColumnName} pk={c.IsPrimaryKey} identity={c.IsIdentity} null={c.CanBeNull} type={c.DataType} length={c.Length} precision={c.Precision} scale={c.Scale}")));
		}

		[Test]
		public void FluentEqualsAttributes([Values] ScaffoldLanguage language)
		{
			ScaffoldedModel Build(MetadataSource metadata) => ScaffoldHarness.Build(
				$"{nameof(FluentEqualsAttributes)}_{metadata}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(),
				options =>
				{
					options.DataModel.Metadata          = metadata;
					options.DataModel.GenerateDataType  = true;
					options.DataModel.GenerateLength    = true;
					options.DataModel.GeneratePrecision = true;
					options.DataModel.GenerateScale     = true;
				});

			var attributes = Describe(Build(MetadataSource.Attributes));
			var fluent     = Describe(Build(MetadataSource.FluentMapping));

			fluent.ShouldBe(attributes);

			// guards against both builders producing the same empty/default mapping
			attributes.ShouldContain("FirstName: first_name pk=False identity=False null=False type=NVarChar length=50");
			attributes.ShouldContain("PersonId: person_id pk=True identity=True");
			attributes.ShouldContain("Salary: salary pk=False identity=False null=True type=Decimal length= precision=18 scale=4");
		}

		[Test]
		public void ColumnNameMappingExecutes([Values] ScaffoldLanguage language, [Values(MetadataSource.Attributes, MetadataSource.FluentMapping)] MetadataSource metadata)
		{
			var model = ScaffoldHarness.Build(
				$"{nameof(ColumnNameMappingExecutes)}_{metadata}",
				language,
				ProviderProfile.SQLite,
				CreateSchema(),
				options => options.DataModel.Metadata = metadata);

			model.GetType("PersonInfo").GetProperty("FirstName").ShouldNotBeNull();

			using var db = new SqliteTestDb();

			ModelSmoke.Run(model, db.Options).Rows.ShouldBe(ModelSmoke.RowCount);

			using var command = db.Connection.CreateCommand();
			command.CommandText = "SELECT COUNT(*) FROM person_info WHERE first_name IS NOT NULL";
			Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture).ShouldBe(ModelSmoke.RowCount);
		}
	}
}
