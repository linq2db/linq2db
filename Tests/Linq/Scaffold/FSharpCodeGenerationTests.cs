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
		public void RecordTemplate()
		{
			var source = GenerateSource(
				FSharpOptions(),
				columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0), new Column("Name", null, new DatabaseType("nvarchar", 50, null, null), true, true, true, 1)]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("static member Empty : TestTable ="));
				Assert.That(source, Does.Contain("Id = Unchecked.defaultof<_>"));
				Assert.That(source, Does.Contain("Name = None"));
			}
		}

		[Test]
		public void RecordTemplateNameConflict()
		{
			var options = FSharpOptions();
			options.DataModel.EntityColumnPropertyNameOptions = new() { Casing = NameCasing.None };

			var source = GenerateSource(options, columns: [new Column("Empty", null, new DatabaseType("int", null, null, null), false, true, true, 0)]);

			Assert.That(source, Does.Contain("static member Empty1 : TestTable ="));
		}

		[Test]
		public void LiftedTypeNameConflict()
		{
			var options = FSharpOptions();
			options.Schema.LoadedObjects |= SchemaObjects.StoredProcedure;

			var procedure = new StoredProcedure(
				new SqlObjectName("GetData", Schema: "dbo"),
				null,
				[],
				null,
				[[new ResultColumn("Value", new DatabaseType("nvarchar", 50, null, null), true)]],
				new VoidResult());

			// a table with primary key ahead of the conflicting one: its Find extension creates the extensions class
			// holding the procedure result class before the conflicting entity in the code model
			var source = GenerateSource(
				options,
				tableName  : "GetDataResult",
				columns    : [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)],
				primaryKey : new PrimaryKey(null, ["Id"]),
				procedures : [procedure],
				extraTables: [new Table(new SqlObjectName("Account", Schema: "dbo"), null, [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)], null, new PrimaryKey(null, ["Id"]))]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(System.Text.RegularExpressions.Regex.Matches(source, @"^type GetDataResult\b", System.Text.RegularExpressions.RegexOptions.Multiline), Has.Count.EqualTo(1));
				Assert.That(source, Does.Contain("type GetDataResult ="));
				Assert.That(source, Does.Contain("type GetDataResult1 () ="));
			}
		}

		[Test]
		public void ModuleExtensionsTypeNameConflict()
		{
			var options = FSharpOptions();
			options.DataModel.EntityClassNameOptions = new() { Casing = NameCasing.None, Pluralization = Pluralization.None };

			var source = GenerateSource(
				options,
				tableName  : "Extensions",
				tableSchema: "other",
				columns    : [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)],
				primaryKey : new PrimaryKey(null, ["Id"]));

			using (Assert.EnterMultipleScope())
			{
				Assert.That(System.Text.RegularExpressions.Regex.Matches(source, @"^\s*type Extensions\b", System.Text.RegularExpressions.RegexOptions.Multiline), Has.Count.EqualTo(1));
				Assert.That(source, Does.Contain("type Extensions1 ="));
			}
		}

		[Test]
		public void ModuleWithoutMethodsKeepsExtensionsName()
		{
			var options = FSharpOptions();
			options.DataModel.EntityClassNameOptions = new() { Casing = NameCasing.None, Pluralization = Pluralization.None };

			// no primary key: no Find extension, so the module gets no synthesized Extensions type
			var source = GenerateSource(
				options,
				tableName  : "Extensions",
				tableSchema: "other",
				columns    : [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("type Extensions ="));
				Assert.That(source, Does.Not.Contain("Extensions1"));
			}
		}

		// all generated types share one `namespace rec` scope, so references to framework types shadowed by entity names
		// (including names of namespace segments) must be qualified until unambiguous
		[Test]
		public void ShadowedTypeReferencesQualified()
		{
			var options = FSharpOptions();
			options.Schema.LoadedObjects |= SchemaObjects.StoredProcedure;
			options.DataModel.EntityClassNameOptions = new() { Casing = NameCasing.None, Pluralization = Pluralization.None };
			options.DataModel.GenerateProcedureAsync = true;

			var procedure = new StoredProcedure(
				new SqlObjectName("Proc", Schema: "dbo"),
				null,
				[new Parameter("@value", null, new DatabaseType("int", null, null, null), true, LinqToDB.Schema.ParameterDirection.Input)],
				null,
				[[new ResultColumn("Value", new DatabaseType("nvarchar", 50, null, null), true)]],
				new VoidResult());

			Table T(string name) => new(new SqlObjectName(name, Schema: "dbo"), null, [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0), new Column("Val", null, new DatabaseType("int", null, null, null), true, true, true, 1)], null, new PrimaryKey(null, ["Id"]));

			var source = GenerateSource(
				options,
				tableName  : "Task",
				columns    : [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)],
				primaryKey : new PrimaryKey(null, ["Id"]),
				procedures : [procedure],
				extraTables: [T("DataConnection"), T("Option"), T("DataOptions"), T("ITable"), T("Func"), T("DataParameter"), T("Extensions"), T("Sql"), T("Data"), T("Core"), T("Tasks"), T("LinqToDB"), T("System"), T("Microsoft")]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("type TestDataContext (options : LinqToDB.DataOptions<TestDataContext>) ="));
				Assert.That(source, Does.Contain("inherit LinqToDB.Data.DataConnection(options.Options)"));
				Assert.That(source, Does.Contain("LinqToDB.Data.DataParameter(\"@value\", FSharp.Core.Option.toNullable(value), DataType.Int32)"));
				Assert.That(source, Does.Contain(": Threading.Tasks.Task<IEnumerable<ProcResult>> ="));
				Assert.That(source, Does.Contain("(table : LinqToDB.ITable<Task>, id : int)"));
			}
		}

		[Test]
		public void RejectsMissingNamespace()
		{
			var options = FSharpOptions();
			options.CodeGeneration.Namespace = null;

			Assert.That(
				() => GenerateSource(options, columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]),
				Throws.TypeOf<NotSupportedException>().With.Message.Contains("namespace"));
		}

		[Test]
		public void RecordTemplateMemberNameConflict()
		{
			var options = FSharpOptions();
			options.DataModel.GenerateIEquatable = true;

			var source = GenerateSource(
				options,
				columns    : [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)],
				primaryKey : new PrimaryKey(null, ["Id"]),
				converters : [new RenameIdentifier("_equalityComparer", "Empty")]);

			using (Assert.EnterMultipleScope())
			{
				Assert.That(source, Does.Contain("static let Empty : IEqualityComparer<TestTable>"));
				Assert.That(source, Does.Contain("static member Empty1 : TestTable ="));
			}
		}

		[Test]
		public void NoPartialClassesWithoutPartialTypes()
		{
			var options = FSharpOptions();
			options.Schema.LoadedObjects |= SchemaObjects.StoredProcedure | SchemaObjects.ScalarFunction;

			var procedure = new StoredProcedure(
				new SqlObjectName("GetData", Schema: "dbo"),
				null,
				[],
				null,
				[[new ResultColumn("Value", new DatabaseType("nvarchar", 50, null, null), true)]],
				new VoidResult());

			var function = new ScalarFunction(
				new SqlObjectName("GetPair", Schema: "dbo"),
				null,
				[],
				new TupleResult([new ScalarResult("First", new DatabaseType("int", null, null, null), false), new ScalarResult("Second", new DatabaseType("nvarchar", 50, null, null), true)], false));

			// data context, extensions class (Find), procedure result class, function tuple class, schema wrapper and
			// schema context
			var schema = new TestSchemaProvider(
				[new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)],
				[procedure],
				new PrimaryKey(null, ["Id"]),
				"other",
				"OtherTable",
				[new Table(new SqlObjectName("Account", Schema: "dbo"), null, [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)], null, new PrimaryKey(null, ["Id"]))],
				[function]);

			var scaffolder = new Scaffolder(LanguageProviders.FSharp, HumanizerNameConverter.Instance, options, null);
			var dataModel  = scaffolder.LoadDataModel(schema, TestTypeMappingProvider.Instance);
			var provider   = SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);
			var files      = scaffolder.GenerateCodeModel(provider.CreateSqlBuilder(provider.MappingSchema, new DataOptions()), dataModel, MetadataBuilders.GetMetadataBuilder(scaffolder.Language, options.DataModel.Metadata));

			var classes = files
				.SelectMany(f => f.Items.OfType<CodeNamespace>())
				.SelectMany(ns => AllClasses(ns.Members))
				.ToList();

			using (Assert.EnterMultipleScope())
			{
				Assert.That(classes, Has.Count.GreaterThanOrEqualTo(6));
				Assert.That(classes.Where(static c => c.Attributes.HasFlag(Modifiers.Partial)).Select(static c => c.Name.Name), Is.Empty);
			}

			static IEnumerable<CodeClass> AllClasses(IEnumerable<IMemberGroup> groups)
			{
				foreach (var @class in groups.EnumerateMembers<ClassGroup, CodeClass>())
				{
					yield return @class;

					foreach (var nested in AllClasses(@class.Members))
						yield return nested;
				}
			}
		}

		[Test]
		public void CSharpGeneratorRejectsRecord()
		{
			var language = new DelegatingLanguageProvider(LanguageProviders.CSharp, LanguageProviders.CSharp.Features | LanguageFeatures.RecordEntities);

			Assert.That(
				() => GenerateFiles(FSharpOptions(), language: language, columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]),
				Throws.TypeOf<NotSupportedException>().With.Message.Contains("record"));
		}

		[Test]
		public void CSharpGeneratorRejectsModule()
		{
			var language = new DelegatingLanguageProvider(LanguageProviders.CSharp, LanguageProviders.CSharp.Features & ~LanguageFeatures.NestedTypes);

			Assert.That(
				() => GenerateFiles(FSharpOptions(), language: language, tableSchema: "other", columns: [new Column("Id", null, new DatabaseType("int", null, null, null), false, true, true, 0)]),
				Throws.TypeOf<NotSupportedException>().With.Message.Contains("module"));
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

		static string GenerateSource(ScaffoldOptions options, IReadOnlyCollection<Column>? columns = null, IReadOnlyCollection<StoredProcedure>? procedures = null, PrimaryKey? primaryKey = null, string tableName = "TestTable", string tableSchema = "dbo", IReadOnlyCollection<Table>? extraTables = null, ConvertCodeModelVisitor[]? converters = null)
		{
			return string.Join("\n", GenerateFiles(options, columns, procedures, primaryKey, tableSchema: tableSchema, tableName: tableName, extraTables: extraTables, converters: converters));
		}

		static string[] GenerateFiles(ScaffoldOptions options, IReadOnlyCollection<Column>? columns = null, IReadOnlyCollection<StoredProcedure>? procedures = null, PrimaryKey? primaryKey = null, ILanguageProvider? language = null, string tableSchema = "dbo", string tableName = "TestTable", IReadOnlyCollection<Table>? extraTables = null, ConvertCodeModelVisitor[]? converters = null)
		{
			var scaffolder = new Scaffolder(language ?? LanguageProviders.FSharp, HumanizerNameConverter.Instance, options, null);
			var schema     = new TestSchemaProvider(columns ?? [], procedures ?? [], primaryKey, tableSchema, tableName, extraTables ?? []);
			var dataModel  = scaffolder.LoadDataModel(schema, TestTypeMappingProvider.Instance);

			var provider   = SqlServerTools.GetDataProvider(SqlServerVersion.v2012, SqlServerProvider.MicrosoftDataSqlClient);
			var sqlBuilder = provider.CreateSqlBuilder(provider.MappingSchema, new DataOptions());

			var files      = scaffolder.GenerateCodeModel(
				sqlBuilder,
				dataModel,
				MetadataBuilders.GetMetadataBuilder(scaffolder.Language, options.DataModel.Metadata),
				[new ProviderSpecificStructsEqualityFixer(scaffolder.Language), .. converters ?? []]);

			return scaffolder.GenerateSourceCode(dataModel, files).Select(f => f.Code).ToArray();
		}

		// renames identifiers in the code model, standing in for user code model converters
		sealed class RenameIdentifier(string from, string to) : ConvertCodeModelVisitor
		{
			protected override ICodeElement Visit(CodeIdentifier identifier)
				=> string.Equals(identifier.Name, from, StringComparison.Ordinal) ? new CodeIdentifier(to, false) : identifier;
		}

		sealed class TestSchemaProvider(IReadOnlyCollection<Column> columns, IReadOnlyCollection<StoredProcedure> procedures, PrimaryKey? primaryKey, string tableSchema, string tableName, IReadOnlyCollection<Table> extraTables, IReadOnlyCollection<ScalarFunction>? scalarFunctions = null) : ISchemaProvider
		{
			IEnumerable<Table> ISchemaProvider.GetTables()
			{
				foreach (var table in extraTables)
					yield return table;

				if (columns.Count > 0)
					yield return new Table(new SqlObjectName(tableName, Schema: tableSchema), null, columns, null, primaryKey);
			}

			IEnumerable<View>              ISchemaProvider.GetViews()              => [];
			IEnumerable<ForeignKey>        ISchemaProvider.GetForeignKeys()        => [];
			IEnumerable<StoredProcedure>   ISchemaProvider.GetProcedures(bool withSchema, bool safeSchemaOnly) => procedures;
			IEnumerable<TableFunction>     ISchemaProvider.GetTableFunctions()     => [];
			IEnumerable<ScalarFunction>    ISchemaProvider.GetScalarFunctions()    => scalarFunctions ?? [];
			IEnumerable<AggregateFunction> ISchemaProvider.GetAggregateFunctions() => [];
			ISet<string>                   ISchemaProvider.GetDefaultSchemas()     => new HashSet<string>(["dbo"], StringComparer.Ordinal);
			string?                        ISchemaProvider.DatabaseName            => null;
			string?                        ISchemaProvider.ServerVersion           => null;
			string?                        ISchemaProvider.DataSource              => null;
			DatabaseOptions                ISchemaProvider.DatabaseOptions         => SqlServerDatabaseOptions.Instance;
		}

		// a language provider that is not the built-in instance but behaves exactly like the wrapped one
		sealed class DelegatingLanguageProvider(ILanguageProvider provider, LanguageFeatures? features = null) : ILanguageProvider
		{
			IEqualityComparer<CodeIdentifier>              ILanguageProvider.IdentifierEqualityComparer     => provider.IdentifierEqualityComparer;
			IEqualityComparer<IEnumerable<CodeIdentifier>> ILanguageProvider.FullNameEqualityComparer       => provider.FullNameEqualityComparer;
			IComparer<IEnumerable<CodeIdentifier>>         ILanguageProvider.FullNameComparer               => provider.FullNameComparer;
			IEqualityComparer<string>                      ILanguageProvider.RawIdentifierEqualityComparer  => provider.RawIdentifierEqualityComparer;
			IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithNRT    => provider.TypeEqualityComparerWithNRT;
			IEqualityComparer<IType>                       ILanguageProvider.TypeEqualityComparerWithoutNRT => provider.TypeEqualityComparerWithoutNRT;
			ITypeParser                                    ILanguageProvider.TypeParser                     => provider.TypeParser;
			bool                                           ILanguageProvider.NRTSupported                   => provider.NRTSupported;
			LanguageFeatures                               ILanguageProvider.Features                       => features ?? provider.Features;
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
