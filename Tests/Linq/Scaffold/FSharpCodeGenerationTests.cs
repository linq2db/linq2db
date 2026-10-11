using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

using LinqToDB;
using LinqToDB.CodeModel;
using LinqToDB.Data;
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
		public void EquatableEntityRecord()
		{
			var options = FSharpOptions();
			options.DataModel.GenerateIEquatable = true;

			var source = GenerateSource(
				options,
				columns   : [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0), new Column("Name", null, new DatabaseType("nvarchar", 50, null, null), true, true, true, 1)],
				primaryKey: new PrimaryKey(null, ["Id"]));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("[<CustomEquality; NoComparison>]"));
				Assert.That(source, Does.Contain("static let _equalityComparer : IEqualityComparer<TestTable> = ComparerBuilder.GetEqualityComparer<TestTable>((fun c -> box (c.Id)))"));
				Assert.That(source, Does.Contain("interface IEquatable<TestTable> with"));
				Assert.That(source, Does.Contain("member this.Equals (other : TestTable | null) : bool = _equalityComparer.Equals(this, Unchecked.nonNull(other))"));
				Assert.That(source, Does.Contain("override this.GetHashCode () : int = _equalityComparer.GetHashCode(this)"));
				Assert.That(source, Does.Contain("override this.Equals (obj : obj | null) : bool = _equalityComparer.Equals(this, (match box obj with :? TestTable as value -> value | _ -> Unchecked.defaultof<TestTable>))"));
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

		[Test]
		public void DelegatingProviderMatchesFSharp([IncludeDataSources(TestProvName.SqlServer2022MS, ProviderName.SQLiteMS)] string context)
		{
			using var db = GetDataConnection(context);

			var metadata = context.IsAnyOf(TestProvName.AllSQLite) ? MetadataSource.FluentMapping : MetadataSource.Attributes;

			var expected = GenerateDatabaseSource(LanguageProviders.FSharp, db, DatabaseOptions(metadata));
			var actual   = GenerateDatabaseSource(new DelegatingLanguageProvider(LanguageProviders.FSharp), db, DatabaseOptions(metadata));

			Assert.That(actual, Is.EqualTo(expected));
		}

		static ScaffoldOptions DatabaseOptions(MetadataSource metadata)
		{
			var options = FSharpOptions();

			options.Schema.LoadedObjects               = SchemaObjects.Table | SchemaObjects.View | SchemaObjects.ForeignKey | SchemaObjects.StoredProcedure | SchemaObjects.ScalarFunction | SchemaObjects.TableFunction | SchemaObjects.AggregateFunction;
			options.Schema.LoadProceduresSchema        = true;
			options.DataModel.GenerateProcedureSync    = true;
			options.DataModel.GenerateProcedureAsync   = true;
			options.DataModel.GenerateIEquatable       = true;
			options.DataModel.Metadata                 = metadata;

			return options;
		}

		static string GenerateDatabaseSource(ILanguageProvider language, DataConnection db, ScaffoldOptions options)
		{
			var schema     = new LegacySchemaProvider(db, options.Schema, language);
			var scaffolder = new Scaffolder(language, HumanizerNameConverter.Instance, options, null);
			var dataModel  = scaffolder.LoadDataModel(schema, schema);
			var files      = scaffolder.GenerateCodeModel(
				db.DataProvider.CreateSqlBuilder(db.MappingSchema, db.Options),
				dataModel,
				MetadataBuilders.GetMetadataBuilder(scaffolder.Language, options.DataModel.Metadata),
				new ProviderSpecificStructsEqualityFixer(scaffolder.Language));

			return string.Join("\n", scaffolder.GenerateSourceCode(dataModel, files).Select(f => f.Code));
		}

		[Test]
		public void DefaultOptionsAdjustedToLanguage()
		{
			var options = ScaffoldOptions.Default();
			options.DataModel.ContextClassName = "TestDataContext";

			var files = GenerateFiles(options, columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(files, Has.Length.EqualTo(1));
				Assert.That(files[0], Does.Not.Contain("\t"));
				Assert.That(files[0], Does.Contain("type TestDataContext (options : DataOptions<TestDataContext>) ="));
				Assert.That(files[0], Does.Not.Contain("InitDataContext"));
			}
		}

		[Test]
		public void TabIndentReplaced()
		{
			var options = FSharpOptions();
			options.CodeGeneration.Indent = "\t\t";

			var source = GenerateSource(options, columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Not.Contain("\t"));
				Assert.That(source, Does.Contain("\n        inherit "));
			}
		}

		[Test]
		public void LanguageProviderFeatures()
		{
			using (Assert.EnterMultipleScope())
			{
				Assert.That(LanguageProviders.FSharp.Features, Is.EqualTo(LanguageFeatures.RecordEntities | LanguageFeatures.OptionTypes));
				Assert.That(LanguageProviders.CSharp.Features, Is.EqualTo(
					  LanguageFeatures.NestedTypes
					| LanguageFeatures.NullableFlowAnalysis
					| LanguageFeatures.LambdaReturnTypeInference
					| LanguageFeatures.ImplicitUpcast
					| LanguageFeatures.ImplicitInterfaceImplementation
					| LanguageFeatures.ImplicitDelegateConversion
					| LanguageFeatures.ConstructorPropertyInitialization
					| LanguageFeatures.PartialTypes
					| LanguageFeatures.PartialMethods
					| LanguageFeatures.MultipleConstructors
					| LanguageFeatures.TabIndentation
					| LanguageFeatures.MultipleFiles));
			}
		}

		static ScaffoldOptions FSharpOptions()
		{
			var options = ScaffoldOptions.Default();

			options.DataModel.ContextClassName   = "TestDataContext";
			options.DataModel.GenerateIEquatable = false;

			return options;
		}

		static string GenerateSource(ScaffoldOptions options, IReadOnlyCollection<Column>? columns = null, IReadOnlyCollection<StoredProcedure>? procedures = null, PrimaryKey? primaryKey = null)
		{
			return string.Join("\n", GenerateFiles(options, columns, procedures, primaryKey));
		}

		static string[] GenerateFiles(ScaffoldOptions options, IReadOnlyCollection<Column>? columns = null, IReadOnlyCollection<StoredProcedure>? procedures = null, PrimaryKey? primaryKey = null)
		{
			var scaffolder = new Scaffolder(LanguageProviders.FSharp, HumanizerNameConverter.Instance, options, null);
			var schema     = new TestSchemaProvider(columns ?? [], procedures ?? [], primaryKey);
			var dataModel  = scaffolder.LoadDataModel(schema, TestTypeMappingProvider.Instance);

			var provider   = SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);
			var sqlBuilder = provider.CreateSqlBuilder(provider.MappingSchema, new DataOptions());

			var files      = scaffolder.GenerateCodeModel(
				sqlBuilder,
				dataModel,
				MetadataBuilders.GetMetadataBuilder(scaffolder.Language, options.DataModel.Metadata),
				new ProviderSpecificStructsEqualityFixer(scaffolder.Language));

			return scaffolder.GenerateSourceCode(dataModel, files).Select(f => f.Code).ToArray();
		}

		sealed class TestSchemaProvider(IReadOnlyCollection<Column> columns, IReadOnlyCollection<StoredProcedure> procedures, PrimaryKey? primaryKey) : ISchemaProvider
		{
			IEnumerable<Table> ISchemaProvider.GetTables()
			{
				if (columns.Count > 0)
					yield return new Table(new SqlObjectName("TestTable", Schema: "dbo"), null, columns, null, primaryKey);
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

		// a language provider that is not the built-in instance but behaves exactly like the wrapped one
		sealed class DelegatingLanguageProvider(ILanguageProvider provider) : ILanguageProvider
		{
			IEqualityComparer<CodeIdentifier>              ILanguageProvider.IdentifierEqualityComparer     => provider.IdentifierEqualityComparer;
			IEqualityComparer<IEnumerable<CodeIdentifier>> ILanguageProvider.FullNameEqualityComparer       => provider.FullNameEqualityComparer;
			IComparer<IEnumerable<CodeIdentifier>>         ILanguageProvider.FullNameComparer               => provider.FullNameComparer;
			IEqualityComparer<string>                      ILanguageProvider.RawIdentifierEqualityComparer  => provider.RawIdentifierEqualityComparer;
			IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithNRT    => provider.TypeEqualityComparerWithNRT;
			IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithoutNRT => provider.TypeEqualityComparerWithoutNRT;
			ITypeParser                                    ILanguageProvider.TypeParser                     => provider.TypeParser;
			bool                                           ILanguageProvider.NRTSupported                   => provider.NRTSupported;
			LanguageFeatures                               ILanguageProvider.Features                       => provider.Features;
			string[]                                       ILanguageProvider.MissingXmlCommentWarnCodes     => provider.MissingXmlCommentWarnCodes;
			string                                         ILanguageProvider.FileExtension                  => provider.FileExtension;
			CodeBuilder                                    ILanguageProvider.ASTBuilder                     => provider.ASTBuilder;

			bool ILanguageProvider.IsValidIdentifierFirstChar   (string character, UnicodeCategory category) => provider.IsValidIdentifierFirstChar   (character, category);
			bool ILanguageProvider.IsValidIdentifierNonFirstChar(string character, UnicodeCategory category) => provider.IsValidIdentifierNonFirstChar(character, category);

			string?          ILanguageProvider.GetAlias(IType type)        => provider.GetAlias(type);
			CodeModelVisitor ILanguageProvider.GetIdentifiersNormalizer() => provider.GetIdentifiersNormalizer();

			CodeGenerationVisitor ILanguageProvider.GetCodeGenerator(
				string                                                                 newLine,
				string                                                                 indent,
				bool                                                                   useNRT,
				IReadOnlyDictionary<CodeIdentifier, ISet<IEnumerable<CodeIdentifier>>> knownTypes,
				IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedNames,
				IReadOnlyDictionary<IEnumerable<CodeIdentifier>, ISet<CodeIdentifier>> scopedTypes)
				=> provider.GetCodeGenerator(newLine, indent, useNRT, knownTypes, scopedNames, scopedTypes);
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
