using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB;
using LinqToDB.CodeModel;
using LinqToDB.DataModel;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;
using LinqToDB.Schema;
using LinqToDB.SqlQuery;

using NUnit.Framework;

namespace Tests.Scaffold
{
	// F# code generation over a predefined schema (#1553). Temporary text-level coverage until generated code can
	// be compiled and executed by tests.
	[TestFixture]
	public sealed class FSharpCodeGenerationTests : TestBase
	{
		[Test]
		public void EscapesOCamlReservedIdentifier()
		{
			var options = FSharpOptions();
			options.DataModel.EntityColumnPropertyNameOptions = new() { Casing = NameCasing.None };

			var source = GenerateSource(options, columns: [new Column("mod", null, new DatabaseType("int", null, null, null), false, true, true, 0)]);

			Assert.That(source, Does.Contain("``mod`` : int"));
		}

		[Test]
		public void FluentMappingInStaticInitializer()
		{
			var options = FSharpOptions();
			options.DataModel.Metadata = MetadataSource.FluentMapping;

			var source = GenerateSource(options, columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("static let _ContextSchema : MappingSchema = MappingSchema()"));
				Assert.That(source, Does.Contain("static do"));
				Assert.That(source, Does.Contain("FluentMappingBuilder(TestDataContext.ContextSchema)"));
				Assert.That(source, Does.Contain("static member ContextSchema with get() : MappingSchema = _ContextSchema"));
				Assert.That(source, Does.Not.Contain("static member val ContextSchema"));
			}
		}

		[Test]
		public void RejectsEntityBaseClass()
		{
			var options = FSharpOptions();
			options.DataModel.BaseEntityClass = "My.Namespace.BaseEntity";

			Assert.That(
				() => GenerateSource(options, columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]),
				Throws.TypeOf<NotSupportedException>());
		}

		[Test]
		public void UnwrapsOptionProcedureParameter()
		{
			var options = FSharpOptions();
			options.Schema.LoadedObjects |= SchemaObjects.StoredProcedure;

			var procedure = new StoredProcedure(
				new SqlObjectName("TestProcedure", Schema: "dbo"),
				null,
				[
					new Parameter("@nullableValue", null, new DatabaseType("int"     , null, null, null), true , LinqToDB.Schema.ParameterDirection.Input),
					new Parameter("@nullableText" , null, new DatabaseType("nvarchar", 50  , null, null), true , LinqToDB.Schema.ParameterDirection.Input),
					new Parameter("@value"        , null, new DatabaseType("int"     , null, null, null), false, LinqToDB.Schema.ParameterDirection.Input),
				],
				null,
				null,
				new VoidResult());

			var source = GenerateSource(options, procedures: [procedure]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("Option.toNullable(nullableValue)"));
				Assert.That(source, Does.Contain("Option.toObj(nullableText)"));
				Assert.That(source, Does.Contain("DataParameter(\"@value\", value,"));
			}
		}

		// mirrors the options the CLI forces for --target-language f#
		static ScaffoldOptions FSharpOptions()
		{
			var options = ScaffoldOptions.Default();

			options.CodeGeneration.ClassPerFile                  = false;
			options.CodeGeneration.Indent                        = "    ";
			options.DataModel.ContextClassName                   = "TestDataContext";
			options.DataModel.GenerateIEquatable                 = false;
			options.DataModel.GenerateAssociationExtensions      = false;
			options.DataModel.GenerateInitDataContextMethod      = false;
			options.DataModel.GenerateStaticInitDataContextMethod = false;
			options.DataModel.EntityClassIsPartial               = false;
			options.DataModel.HasDefaultConstructor              = false;
			options.DataModel.HasConfigurationConstructor        = false;
			options.DataModel.HasUntypedOptionsConstructor       = false;
			options.DataModel.HasTypedOptionsConstructor         = true;

			return options;
		}

		static string GenerateSource(ScaffoldOptions options, IReadOnlyCollection<Column>? columns = null, IReadOnlyCollection<StoredProcedure>? procedures = null)
		{
			var scaffolder = new Scaffolder(LanguageProviders.FSharp, HumanizerNameConverter.Instance, options, null);
			var schema     = new TestSchemaProvider(columns ?? [], procedures ?? []);
			var dataModel  = scaffolder.LoadDataModel(schema, TestTypeMappingProvider.Instance);

			var provider   = SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);
			var sqlBuilder = provider.CreateSqlBuilder(provider.MappingSchema, new DataOptions());

			var files      = scaffolder.GenerateCodeModel(
				sqlBuilder,
				dataModel,
				MetadataBuilders.GetMetadataBuilder(scaffolder.Language, options.DataModel.Metadata),
				new ProviderSpecificStructsEqualityFixer(scaffolder.Language));

			return string.Join("\n", scaffolder.GenerateSourceCode(dataModel, files).Select(f => f.Code));
		}

		sealed class TestSchemaProvider(IReadOnlyCollection<Column> columns, IReadOnlyCollection<StoredProcedure> procedures) : ISchemaProvider
		{
			IEnumerable<Table> ISchemaProvider.GetTables()
			{
				if (columns.Count > 0)
					yield return new Table(new SqlObjectName("TestTable", Schema: "dbo"), null, columns, null, null);
			}

			IEnumerable<View>              ISchemaProvider.GetViews()              => [];
			IEnumerable<ForeignKey>        ISchemaProvider.GetForeignKeys()        => [];
			IEnumerable<StoredProcedure>   ISchemaProvider.GetProcedures(bool withSchema, bool safeSchemaOnly) => procedures;
			IEnumerable<TableFunction>     ISchemaProvider.GetTableFunctions()     => [];
			IEnumerable<ScalarFunction>    ISchemaProvider.GetScalarFunctions()    => [];
			IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions() => [];
			ISet<string>                   ISchemaProvider.GetDefaultSchemas()     => new HashSet<string>(["dbo"], StringComparer.Ordinal);
			string?                        ISchemaProvider.DatabaseName            => null;
			string?                        ISchemaProvider.ServerVersion           => null;
			string?                        ISchemaProvider.DataSource              => null;
			DatabaseOptions                ISchemaProvider.DatabaseOptions         => SqlServerDatabaseOptions.Instance;
		}

		sealed class TestTypeMappingProvider : ITypeMappingProvider
		{
			public static readonly TestTypeMappingProvider Instance = new();

			LinqToDB.Schema.TypeMapping? ITypeMappingProvider.GetTypeMapping(DatabaseType databaseType)
			{
				return databaseType.Name switch
				{
					"int"      => new LinqToDB.Schema.TypeMapping(WellKnownTypes.System.Int32 , DataType.Int32),
					"nvarchar" => new LinqToDB.Schema.TypeMapping(WellKnownTypes.System.String, DataType.NVarChar),
					_          => null,
				};
			}
		}
	}
}
