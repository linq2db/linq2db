using System.Data;
using LinqToDB.Scaffold;

namespace LinqToDB.CLI
{
	internal sealed partial class ScaffoldCommand : CliCommand
	{
		private static readonly OptionCategory _generalOptions = new (1, "General", "basic options", "general");
		private static readonly OptionCategory _schemaOptions = new (2, "Database Schema", "database schema load", "schema");
		private static readonly OptionCategory _dataModelOptions = new (3, "Data Model", "data model configuration", "dataModel");
		private static readonly OptionCategory _codeGenerationOptions = new (4, "Code Generation", "code-generation options", "code");

		public static CliCommand Instance { get; } = new ScaffoldCommand();

		private enum DatabaseType
		{
			Access,
			DB2,
			Firebird,
			Informix,
			SQLServer,
			MySQL,
			Oracle,
			PostgreSQL,
			SqlCe,
			SQLite,
			Sybase,
			SapHana
		}

		private ScaffoldCommand()
			: base(
				  "scaffold",
				  "<options>",
				  "generate database data model classes from database schema",
				  new CommandExample[]
				  {
					  new (
						  "dotnet linq2db scaffold -o c:\\my_project\\model -p SqlServer -c \"Server=MySqlServer;Database=MyDatabase;User Id=scaffold_user;Password=secret;\"",
						  "generates database model classes in specified folder for SQL Server database, pointed by specified connection string"),
					  new (
						  "dotnet linq2db scaffold -i path\\to\\my_scaffold_options.json -c \"Server=MySqlServer;Database=MyDatabase;User Id=scaffold_user;Password=secret;\"",
						  "generates database model classes using options, specified in json file, except database connection string, passed explicitly"),
				  })
		{
			// use current defaults to configure options defaults
			var defaultOptions = ScaffoldOptions.Default();

			#region General options
			AddOption(
				_generalOptions,
				new StringCliOption(
					"import",
					'i',
					false,
					false,
					"path to JSON file with scaffold options",
					@"When both command line and JSON file contains same option, value from command line will override value from file for single-value options and will combine values for multi-value options.
JSON property type depends on option type: bool for boolean properties, integer number for numeric properties and string for other properties.
If property could have multiple values, it should use array as property type.
JSON file example:
	{
	  ""general"": {
	    ""output"": ""c:\my\project\datamodel"",
	    ""overwrite"": true,
	    ""provider"": ""SQLServer"",
	  },
	  ""code"": {
	    ""nrt"": false
	  }
	}",
					null,
					null,
					null));

			AddOption(
				_generalOptions,
				new StringCliOption(
					"output",
					'o',
					false,
					false,
					"relative or full path to folder to put generated files",
					"If folder doesn't exists, it will be created. When not specified, current folder used.",
					null,
					null,
					null));

			AddOption(
				_generalOptions,
				new BooleanCliOption(
					"overwrite",
					'f',
					false,
					"overwrite existing files",
					null,
					null,
					null,
					false));

			AddOption(
				_generalOptions,
				new StringEnumCliOption(
					"provider",
					'p',
					true,
					false,
					"database provider (database)",
					null,
					null,
					null,
					new StringEnumOption[]
					{
						// TODO: implement provider discovery for access
						new (false, DatabaseType.Access.ToString(), "MS Access (requires OLE DB or/and ODBC provider installed)"),
						new (false, DatabaseType.DB2.ToString(), "IBM DB2 LUW"),
						new (false, DatabaseType.Firebird.ToString(), "Firebird"),
						new (false, DatabaseType.Informix.ToString(), "IBM Informix"),
						new (false, DatabaseType.SQLServer.ToString(), "MS SQL Server (including Azure SQL Server)"),
						new (false, DatabaseType.MySQL.ToString(), "MySQL/MariaDB"),
						new (false, DatabaseType.Oracle.ToString(), "Oracle Database"),
						new (false, DatabaseType.PostgreSQL.ToString(), "PostgreSQL"),
						new (false, DatabaseType.SqlCe.ToString(), "MS SQL Server Compact"),
						new (false, DatabaseType.SQLite.ToString(), "SQLite"),
						new (false, DatabaseType.Sybase.ToString(), "SAP/Sybase ASE"),
						new (false, DatabaseType.SapHana.ToString(), "SAP HANA"),
					}));

			AddOption(
				_generalOptions,
				new StringCliOption(
					"connection",
					'c',
					true,
					false,
					"database connection string",
					null,
					null,
					null,
					null));

			AddOption(
				_generalOptions,
				new StringEnumCliOption(
					"architecture",
					'a',
					false,
					false,
					"process architecture for utility",
					@"By default utility runs AnyCPU build, which could result in error in multi-arch environment when platform-specific database provider used and provider's architecture doesn't match process architecture. For such provider you could specify process architecture explicitly.
Example of platform-specific providers:
 - OLE DB providers
 - ODBC providers
 - thin wrappers over native provider (e.g. IBM.Data.DB2 providers)",
					null,
					null,
					new StringEnumOption[]
					{
						// TODO: implement provider discovery for access
						new (false, "x86", "x86 architecture"),
						new (false, "x64", "x64 architecture"),
					}));
			#endregion

			#region Code generation options
			AddOption(
				_codeGenerationOptions,
				new BooleanCliOption(
					"nrt",
					null,
					false,
					"enable generation of nullable reference type annotations",
					null,
					null,
					null,
					defaultOptions.CodeGeneration.EnableNullableReferenceTypes));

			AddOption(
				_codeGenerationOptions,
				new StringCliOption(
					"indent",
					null,
					false,
					false,
					"code indent string",
					null,
					null,
					null,
					new[] { "\\t" }));

			AddOption(
				_codeGenerationOptions,
				new StringCliOption(
					"new-line",
					null,
					false,
					false,
					"new line sequence, used by generated code",
					"it is recommended to use json for this option, as passing new line characters in command line could be tricky",
					new[] { "--new-line \"\\r\\n\"" },
					new[] { @"{ ""code"": { ""new-line"": ""
"" }" },
					new[] { "value of Environment.NewLine" }));

			AddOption(
				_codeGenerationOptions,
				new BooleanCliOption(
					"no-xmldoc-warn",
					null,
					false,
					"suppress missing xml-doc warnings in generated code",
					null,
					null,
					null,
					defaultOptions.CodeGeneration.SuppressMissingXmlDocWarnings));

			AddOption(
				_codeGenerationOptions,
				new BooleanCliOption(
					"autogenerated",
					null,
					false,
					"marks code with <auto-generated /> comment",
					null,
					null,
					null,
					defaultOptions.CodeGeneration.MarkAsAutoGenerated));

			AddOption(
				_codeGenerationOptions,
				new BooleanCliOption(
					"single-file",
					null,
					false,
					"generates all code in single file",
					null,
					null,
					null,
					!defaultOptions.CodeGeneration.ClassPerFile));

			AddOption(
				_codeGenerationOptions,
				new StringCliOption(
					"conflicts",
					null,
					false,
					true,
					"specify namespaces and/or types that conflict with generated code",
					@"If generated code has name conflict with other namespaces or types you define or reference in your code, you can pass those names to this method. Code generator will use this information to generate non-conflicting names.",
					new[] { "--conflicts My.Namespace.SomeType+ConflictingNestedType,Some.ConflictingNamespace.Or.Type" },
					new[] { @"{ ""code"": { ""conflicts"": [""My.Namespace.SomeType+ConflictingNestedType"", ""Some.ConflictingNamespace.Or.Type""] }" },
					null));

			AddOption(
				_codeGenerationOptions,
				new StringCliOption(
					"header",
					null,
					false,
					false,
					"specify custom text for <auto-generated /> file header comment (when enabled)",
					@"When not specified, uses default linq2db header text.",
					null,
					null,
					null));

			AddOption(
				_codeGenerationOptions,
				new StringCliOption(
					"namespace",
					'n',
					false,
					false,
					"namespace name for generated code",
					null,
					null,
					null,
					defaultOptions.CodeGeneration.Namespace != null ? new[] { defaultOptions.CodeGeneration.Namespace } : null));
			#endregion

			#region Data model options
			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-db-name",
					null,
					false,
					"include database name into generated mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.IncludeDatabaseName));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-default-schema-name",
					null,
					false,
					"include default schema(s) name into generated mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateDefaultSchema));

			AddOption(
				_dataModelOptions,
				new StringCliOption(
					"base-entity",
					null,
					false,
					false,
					"base class for generated entities",
					null,
					new []
					{
						"--base-entity My.Namespace.MyBaseEntity",
						"--base-entity My.Namespace.ParentClass+MyBaseNestedEntity",
					},
					new[]
					{
						"{ \"dataModel\": { \"base-entity\": \"My.Namespace.MyBaseEntity\" } }",
						"{ \"dataModel\": { \"base-entity\": \"My.Namespace.ParentClass+MyBaseNestedEntity\" } }",
					},
					null));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-datatype",
					null,
					false,
					"include DataType enum value to column mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateDataType));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-db-type",
					null,
					false,
					"include database type to column mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateDbType));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-length",
					null,
					false,
					"include database type length/size to column mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateLength));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-precision",
					null,
					false,
					"include database type precision to column mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.GeneratePrecision));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-scale",
					null,
					false,
					"include database type scale to column mappings",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateScale));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"include-db-info",
					null,
					false,
					"generate comment on data context with database information",
					"Information includes database name, data source and server version (when database provider expose this information).",
					null,
					null,
					defaultOptions.DataModel.IncludeDatabaseInfo));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-default-ctor",
					null,
					false,
					"generate default constructor on data context",
					null,
					null,
					null,
					defaultOptions.DataModel.HasDefaultConstructor));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-configuration-ctor",
					null,
					false,
					"generate data context contructor with configuration name parameter",
					"Constructor example: public MyDataContext(string context) { ... }",
					null,
					null,
					defaultOptions.DataModel.HasConfigurationConstructor));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-options-ctor",
					null,
					false,
					"generate data context contructor with options parameter",
					"Constructor example: public MyDataContext(LinqToDbConnectionOptions options) { ... }",
					null,
					null,
					defaultOptions.DataModel.HasUntypedOptionsConstructor));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-typed-options-ctor",
					null,
					false,
					"generate data context contructor with generic options parameter",
					"Constructor example: public MyDataContext(LinqToDbConnectionOptions<MyDataContext> options) { ... }",
					null,
					null,
					defaultOptions.DataModel.HasTypedOptionsConstructor));

			AddOption(
				_dataModelOptions,
				new StringCliOption(
					"context-name",
					null,
					false,
					false,
					"class name for generated data context",
					"When not specified, database name used. When database name not available, \"MyDataContext\" used",
					null,
					null,
					new[] { "MyDataContext" }));

			AddOption(
				_dataModelOptions,
				new StringCliOption(
					"base-context",
					null,
					false,
					false,
					"base class for generated data context",
					null,
					new[]
					{
						"--base-context LinqToDB.DataContext",
					},
					new[]
					{
						"{ \"dataModel\": { \"base-context\": \"LinqToDB.DataContext\" } }"
					},
					new[] { "LinqToDB.Data.DataConnection" }));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-associations",
					null,
					false,
					"generate association properties on entities",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateAssociations));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-association-extensions",
					null,
					false,
					"generate association extension methods for entities",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateAssociationExtensions));

			// combines AssociationCollectionAsArray + AssociationCollectionType
			AddOption(
				_dataModelOptions,
				new StringCliOption(
					"association-collection",
					null,
					false,
					false,
					"collection type to use for many-sided associations",
					"Should be open-generic type with one parameter or [] for array",
					new[]
					{
						"--association-collection []",
						"--association-collection System.Collections.Generic.List<>",
					},
					new[]
					{
						"{ \"dataModel\": { \"association-collection\": \"[]\" } }",
						"{ \"dataModel\": { \"association-collection\": \"System.Collections.Generic.List<>\" } }",
					},
					new[] { "System.Linq.IQueryable<>" }));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"reuse-entities-in-procedures",
					null,
					false,
					"allows use of entity mapping for return type of stored procedure/table function",
					"When procedure/function schema has same columns (by name, type and nullability) as known table/view entity, this option allows to use entity mapping for procedure/function return type. Otherwise separate mapping class will be generated for specific procedure/function.",
					null,
					null,
					defaultOptions.DataModel.MapProcedureResultToEntity));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"table-function-returns-table",
					null,
					false,
					"table functions use ITable<T> as return type, otherwise IQueryable<T> type used",
					null,
					null,
					null,
					defaultOptions.DataModel.TableFunctionReturnsTable));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"emit-schema-errors",
					null,
					false,
					"generate #error pragma for stored procedures and table functions with schema load errors",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateProceduresSchemaError));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"skip-procedures-with-schema-errors",
					null,
					false,
					"skips mapping generation for stored procedure with schema load errors, otherwise generate mapping without result table",
					null,
					null,
					null,
					defaultOptions.DataModel.SkipProceduresWithSchemaErrors));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"procedure-returns-list",
					null,
					false,
					"use List<T> for stored procedure return dataset, otherwise IEnumerable<T> used",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateProcedureResultAsList));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"add-db-type-to-procedures",
					null,
					false,
					"include database type to stored procedure parameters",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateProcedureParameterDbType));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"schema-as-type",
					null,
					false,
					"generate non-default schemas' code nested into separate class",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateSchemaAsType));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"find-methods",
					null,
					false,
					"enable generation of Find() extension methods to load entity by primary key value",
					null,
					null,
					null,
					defaultOptions.DataModel.GenerateFindExtensions));

			AddOption(
				_dataModelOptions,
				new BooleanCliOption(
					"order-find-parameters-by-ordinal",
					null,
					false,
					"use primary key column ordinal to order Find() extension method parameters (composite primary key only)",
					null,
					null,
					null,
					defaultOptions.DataModel.OrderFindParametersByColumnOrdinal));

			// TODO: EntityColumnPropertyNameOptions
			// TODO: EntityClassNameOptions
			// TODO: EntityContextPropertyNameOptions
			// TODO: DataContextClassNameOptions
			// TODO: SourceAssociationPropertyNameOptions
			// TODO: TargetSingularAssociationPropertyNameOptions
			// TODO: TargetMultipleAssociationPropertyNameOptions
			// TODO: ProcedureParameterNameOptions
			// TODO: ProcedureNameOptions
			// TODO: FunctionTupleResultClassNameOptions
			// TODO: FunctionTupleResultPropertyNameOptions
			// TODO: ProcedureMethodInfoFieldNameOptions
			// TODO: ProcedureResultClassNameOptions
			// TODO: ProcedureResultColumnPropertyNameOptions
			// TODO: SchemaClassNameOptions
			// TODO: SchemaPropertyOptions
			// TODO: FindParameterNameOptions

			// TODO: EntityClassNameProvider
			// TODO: EntityContextPropertyNameProvider
			// TODO: SchemaMap
			#endregion

			#region Schema options
			AddOption(
				_schemaOptions,
				new StringEnumCliOption(
					"objects",
					null,
					false,
					true,
					"schema objects to load",
					null,
					new[] { "--objects table,stored-procedure,table-function" },
					new[] { "{ \"schema\": { \"objects\": [\"table\", \"view\", \"table-function\"] } }" },
					new StringEnumOption[]
					{
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.Table) != 0, "table", "load tables"),
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.View) != 0, "view", "load views"),
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.ForeignKey) != 0, "foreign-key", "load foreign key constrains"),
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.StoredProcedure) != 0, "stored-procedure", "load stored procedures"),
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.ScalarFunction) != 0, "scalar-function", "load scalar functions"),
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.TableFunction) != 0, "table-function", "load table functions"),
						new ((defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.AggregateFunction) != 0, "aggregate-function", "load aggregate/window functions"),
					}));

			AddOption(
				_schemaOptions,
				new BooleanCliOption(
					"prefer-provider-types",
					null,
					false,
					"prefer provider-specific data types to regular .net types for columns and parameters",
					"Database provider could have db-specific .net types to better mapping handling to database types. E.g. MySqlGeometry, NpgsqlInet or OracleTimeStampLTZ types, provided by corresponding providers.",
					null,
					null,
					defaultOptions.Schema.PreferProviderSpecificTypes));

			AddOption(
				_schemaOptions,
				new BooleanCliOption(
					"ignore-duplicate-fk",
					null,
					false,
					"load only first (or order, returned by database) foreign key with same columns",
					null,
					null,
					null,
					defaultOptions.Schema.IgnoreDuplicateForeignKeys));

			//defaultOptions.Schema.IncludeSchemas + defaultOptions.Schema.Schemas
			AddOption(
				_schemaOptions,
				new StringCliOption(
					"include-schemas",
					null,
					false,
					true,
					"load only specified database schemas. Cannot be used with --exclude-schemas option",
					null,
					null,
					null,
					null));

			//defaultOptions.Schema.ExcludeSchemas + defaultOptions.Schema.Schemas
			AddOption(
				_schemaOptions,
				new StringCliOption(
					"exclude-schemas",
					null,
					false,
					true,
					"do not load specified database schemas. Cannot be used with --include-schemas option",
					null,
					null,
					null,
					null));

			//defaultOptions.Schema.IncludeCatalogs + defaultOptions.Schema.Catalogs
			AddOption(
				_schemaOptions,
				new StringCliOption(
					"include-catalogs",
					null,
					false,
					true,
					"load only specified database schemas. Cannot be used with --exclude-catalogs option",
					null,
					null,
					null,
					null));

			//defaultOptions.Schema.ExcludeCatalogs + defaultOptions.Schema.Catalogs
			AddOption(
				_schemaOptions,
				new StringCliOption(
					"exclude-catalogs",
					null,
					false,
					true,
					"do not load specified database schemas. Cannot be used with --include-catalogs option",
					null,
					null,
					null,
					null));

			AddOption(
				_schemaOptions,
				new BooleanCliOption(
					"safe-schema-only",
					null,
					false,
					"load stored procedure/table function schema using only safe methods",
					@"Don't use CommandBehavior.SchemaOnly execution mode to load stored procedure or table function schema as it is not safe if them contain non-transactional code.",
					null,
					null,
					defaultOptions.Schema.UseSafeSchemaLoad));

			AddOption(
				_schemaOptions,
				new BooleanCliOption(
					"load-sproc-schema",
					null,
					false,
					"enable loading stored procedure schema",
					@"When not set, procedures and their parameters will be loaded, but not result-set schema.",
					null,
					null,
					defaultOptions.Schema.LoadProceduresSchema));

			// TODO: add filtering option(s) for delegates:
			// defaultOptions.Schema.LoadTableOrView
			// defaultOptions.Schema.LoadProcedureSchema
			// defaultOptions.Schema.LoadTableFunction
			#endregion
		}
	}
}
