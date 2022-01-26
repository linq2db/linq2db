using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LinqToDB.Naming;
using LinqToDB.Scaffold;

namespace LinqToDB.CLI
{
	partial class ScaffoldCommand : CliCommand
	{
		private static readonly OptionCategory _generalOptions        = new (1, "General"        , "basic options"           , "general"  );
		private static readonly OptionCategory _schemaOptions         = new (2, "Database Schema", "database schema load"    , "schema"   );
		private static readonly OptionCategory _dataModelOptions      = new (3, "Data Model"     , "data model configuration", "dataModel");
		private static readonly OptionCategory _codeGenerationOptions = new (4, "Code Generation", "code-generation options" , "code"     );
		private static readonly ScaffoldOptions _defaultOptions       = ScaffoldOptions.Default();

		/// <summary>
		/// Provides access to general scaffold options definitions.
		/// </summary>
		public static class General
		{
			/// <summary>
			/// Import settings JSON option.
			/// </summary>
			public static readonly CliOption Import = new ImportCliOption(
					"import",
					'i',
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
					null);

			/// <summary>
			/// Output directory option.
			/// </summary>
			public static readonly CliOption Output = new StringCliOption(
					"output",
					'o',
					false,
					false,
					"relative or full path to folder to put generated files",
					"If folder doesn't exists, it will be created. When not specified, current folder used.",
					null,
					null,
					null);

			/// <summary>
			/// Overwrite output files option.
			/// </summary>
			public static readonly CliOption Overwrite = new BooleanCliOption(
					"overwrite",
					'f',
					false,
					"overwrite existing files",
					null,
					null,
					null,
					false);

			/// <summary>
			/// Database provider option.
			/// </summary>
			public static readonly CliOption Provider = new StringEnumCliOption(
					"provider",
					'p',
					true,
					false,
					"database provider (database)",
					null,
					null,
					null,
					false,
					new StringEnumOption[]
					{
						// TODO: implement provider discovery for access
						new (false, DatabaseType.Access    .ToString(), "MS Access (requires OLE DB or/and ODBC provider installed)"),
						new (false, DatabaseType.DB2       .ToString(), "IBM DB2 LUW or z/OS"                                       ),
						new (false, DatabaseType.Firebird  .ToString(), "Firebird"                                                  ),
						new (false, DatabaseType.Informix  .ToString(), "IBM Informix"                                              ),
						new (false, DatabaseType.SQLServer .ToString(), "MS SQL Server (including Azure SQL Server)"                ),
						new (false, DatabaseType.MySQL     .ToString(), "MySQL/MariaDB"                                             ),
						new (false, DatabaseType.Oracle    .ToString(), "Oracle Database"                                           ),
						new (false, DatabaseType.PostgreSQL.ToString(), "PostgreSQL"                                                ),
						new (false, DatabaseType.SqlCe     .ToString(), "MS SQL Server Compact"                                     ),
						new (false, DatabaseType.SQLite    .ToString(), "SQLite"                                                    ),
						new (false, DatabaseType.Sybase    .ToString(), "SAP/Sybase ASE"                                            ),
						new (false, DatabaseType.SapHana   .ToString(), "SAP HANA"                                                  ),
					});

			/// <summary>
			/// Database provider location option.
			/// </summary>
			public static readonly CliOption ProviderLocation = new StringCliOption(
					"provider-location",
					'l',
					false,
					false,
					"database provider location",
					@"Allows user to specify path to database provider for some databases.
Supported databases:
- SQL Server Compact Edition : value is a full path to System.Data.SqlServerCe.dll assembly from Private folder of SQL CE installation
- SAP HANA                   : value is a full path to Sap.Data.Hana.Core.v2.1.dll assembly from HDB client installation folder
- IBM DB2 and Informix       : value is a full path to IBM.Data.DB2.Core.dll assembly in DB2 provider folder",
					null,
					null,
					null);

			/// <summary>
			/// Connection string option.
			/// </summary>
			public static readonly CliOption ConnectionString = new StringCliOption(
					"connection",
					'c',
					true,
					false,
					"database connection string",
					null,
					null,
					null,
					null);

			/// <summary>
			/// Connection string option.
			/// </summary>
			public static readonly CliOption AdditionalConnectionString = new StringCliOption(
					"additional-connection",
					null,
					false,
					false,
					"secondary database connection string",
					@"Allows user to specify additional database connection using different provider. This option supported only for Access and require that main and additional connection strings use OLE DB and ODBC (in any order).
This is caused by fact that both OLE DB and ODBC Access providers return incomplete or invalid information in different places. Using database schema data from both providers allows us to build complete and proper database schema.
List of known issues, solved this way:
- OLE DB doesn't provide information about autoincrement (counter) columns
- OLE DB returns nullable type for procedure column returned for table counter column
- ODBC doesn't provide information about primary keys and foreign keys
- ODBC marks all columns in tables and procedure results as nullable except counter and bit
- ODBC doesn't provide length information for text-based procedure parameter type
- ODBC doesn't show some procedures in schema (no logic traced)",
					null,
					null,
					null);

			/// <summary>
			/// Database provider (AKA scaffold process) architecture option.
			/// </summary>
			public static readonly CliOption Architecture = new StringEnumCliOption(
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
					false,
					new StringEnumOption[]
					{
						new (false, "x86", "x86 architecture"),
						new (false, "x64", "x64 architecture"),
					});

			/// <summary>
			/// Base options template option.
			/// </summary>
			public static readonly CliOption OptionsTemplate = new StringEnumCliOption(
					"template",
					't',
					false,
					false,
					"select base set of default options",
					@"Specify this option only if you want to to use scaffolding options, similar to used by old T4 templates by default",
					null,
					null,
					false,
					new StringEnumOption[]
					{
						new (true , "default", "set of parameters, used by default (as specified in option help)"),
						new (false, "t4"     , "set of parameters, similar to T4 defaults (compat. option)"      ),
					});
		}

		/// <summary>
		/// Provides access to code-generation scaffold options definitions.
		/// </summary>
		public static class CodeGen
		{
			/// <summary>
			/// Nullable annotations generation option.
			/// </summary>
			public static readonly CliOption NRTEnable = new BooleanCliOption(
					"nrt",
					null,
					false,
					"enable generation of nullable reference type annotations",
					null,
					null,
					null,
					_defaultOptions.CodeGeneration.EnableNullableReferenceTypes);

			/// <summary>
			/// Code indent string option.
			/// </summary>
			public static readonly CliOption Indent = new StringCliOption(
					"indent",
					null,
					false,
					false,
					"code indent string",
					null,
					null,
					null,
					new[] { "\\t" });

			/// <summary>
			/// Code new-line string option.
			/// </summary>
			public static readonly CliOption NewLine = new StringCliOption(
					"new-line",
					null,
					false,
					false,
					"new line sequence, used by generated code",
					"it is recommended to use json for this option, as passing new line characters in command line could be tricky",
					new[] { "--new-line \"/*I'm newline*/\"" },
					new[] { @"{ ""code"": { ""new-line"": ""\r\n"" }" },
					new[] { "value of Environment.NewLine" });

			/// <summary>
			/// Xml-doc warning suppression option.
			/// </summary>
			public static readonly CliOption NoXmlDocWarns = new BooleanCliOption(
					"no-xmldoc-warn",
					null,
					false,
					"suppress missing xml-doc warnings in generated code",
					null,
					null,
					null,
					_defaultOptions.CodeGeneration.SuppressMissingXmlDocWarnings);

			/// <summary>
			/// Auto-generated code marker option.
			/// </summary>
			public static readonly CliOption MarkAutogenerated = new BooleanCliOption(
					"autogenerated",
					null,
					false,
					"marks code with <auto-generated /> comment",
					null,
					null,
					null,
					_defaultOptions.CodeGeneration.MarkAsAutoGenerated);

			/// <summary>
			/// Single-file generation mode option.
			/// </summary>
			public static readonly CliOption SingleFile = new BooleanCliOption(
					"single-file",
					null,
					false,
					"generates all code in single file",
					null,
					null,
					null,
					!_defaultOptions.CodeGeneration.ClassPerFile);

			/// <summary>
			/// List of external conflicting identifiers option.
			/// </summary>
			public static readonly CliOption ConflictingIdentifiers = new StringCliOption(
					"conflicts",
					null,
					false,
					true,
					"specify namespaces and/or types that conflict with generated code",
					@"If generated code has name conflict with other namespaces or types you define or reference in your code, you can pass those names to this method. Code generator will use this information to generate non-conflicting names.",
					new[] { "--conflicts My.Namespace.SomeType+ConflictingNestedType,Some.ConflictingNamespace.Or.Type" },
					new[] { @"{ ""code"": { ""conflicts"": [""My.Namespace.SomeType+ConflictingNestedType"", ""Some.ConflictingNamespace.Or.Type""] }" },
					null);

			/// <summary>
			/// Custom file header auto-generated comment text option.
			/// </summary>
			public static readonly CliOption CustomHeader = new StringCliOption(
					"header",
					null,
					false,
					false,
					"specify custom text for <auto-generated /> file header comment (when enabled)",
					@"When not specified, uses default linq2db header text.",
					null,
					null,
					null);

			/// <summary>
			/// Generated code namespace option.
			/// </summary>
			public static readonly CliOption Namespace = new StringCliOption(
					"namespace",
					'n',
					false,
					false,
					"namespace name for generated code",
					null,
					null,
					null,
					_defaultOptions.CodeGeneration.Namespace != null ? new[] { _defaultOptions.CodeGeneration.Namespace } : null);
		}

		/// <summary>
		/// Provides access to data model scaffold options definitions.
		/// </summary>
		public static class DataModel
		{
			/// <summary>
			/// Naming option help text.
			/// </summary>
			private const string NAMING_HELP = @"(naming options could be specified only in JSON file)
Naming options is an object with following properties:
- case                             : string  : specify name casing (see values below)
    + ""none""          : no casing applied to identifier
    + ""pascal_case""   : identifier cased using PascalCase
    + ""camel_case""    : identifier cased using camelCase
    + ""snake_case""    : identifier cased using snake_case
    + ""lower_case""    : identifier cased using lowercase
    + ""upper_case""    : identifier cased using UPPERCASE
    + ""t4_pluralized"" : emulation of casing logic for pluralized names used by T4 templates (compat. option)
    + ""t4""            : emulation of casing logic for non-pluralized names used by T4 templates (compat. option)
- pluralization                    : string  : specify name pluralization (see values below)
    + ""none""                       : don't pluralize identifier
    + ""singular""                   : singularize identifier
    + ""plural""                     : pluralize identifier
    + ""plural_multiple_characters"" : pluralize identifier only when last word is longer than one character
- prefix                           : string? : optional name prefix
- suffix                           : string? : optional name suffix
- transformation                   : string  : base name transformation logic (see values below)
    + ""split_by_underscore"" : split base name, got from database object name, into separate words by underscore (_)
    + ""t4""                  : emulation of identifier generation logic for association name used by T4 templates (compat. option)
- pluralize_if_ends_with_word_only : bool    : when set, pluralization not applied if name ends with non-word (e.g. with digit)
- ignore_all_caps                  : bool    : when set, casing not applied to names that contain only uppercase letters
";

			/// <summary>
			/// Naming option example template.
			/// </summary>
			private const string NAMING_EXAMPLE_TEMPLATE = @"
{{
  ""dataModel"":
  {{
    ""{0}"":
    {{
      ""case""         : ""pascal_case"",
      ""pluralization"": ""singular"",
      ""suffix""       : ""Record""
    }}
  }}
}}";

			/// <summary>
			/// Generate database name in mappings option.
			/// </summary>
			public static readonly CliOption GenerateDbName = new BooleanCliOption(
					"include-db-name",
					null,
					false,
					"include database name into generated mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.IncludeDatabaseName);

			/// <summary>
			/// Generate schema name for default schemas in mappings option.
			/// </summary>
			public static readonly CliOption GenerateDefaultSchemaName = new BooleanCliOption(
					"include-default-schema-name",
					null,
					false,
					"include default schema(s) name into generated mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateDefaultSchema);

			/// <summary>
			/// Base entity class option.
			/// </summary>
			public static readonly CliOption BaseEntity = new StringCliOption(
					"base-entity",
					null,
					false,
					false,
					"base class for generated entities",
					null,
					new[]
					{
						"--base-entity My.Namespace.MyBaseEntity",
						"--base-entity My.Namespace.ParentClass+MyBaseNestedEntity",
					},
					new[]
					{
						"{ \"dataModel\": { \"base-entity\": \"My.Namespace.MyBaseEntity\" } }",
						"{ \"dataModel\": { \"base-entity\": \"My.Namespace.ParentClass+MyBaseNestedEntity\" } }",
					},
					null);

			/// <summary>
			/// Generate <see cref="DataType"/> values on entity columns mappings option.
			/// </summary>
			public static readonly CliOption DataTypeOnTables = new BooleanCliOption(
					"include-datatype",
					null,
					false,
					"include DataType enum value to column mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateDataType);

			/// <summary>
			/// Generate database type name on entity columns mappings option.
			/// </summary>
			public static readonly CliOption DbTypeOnTables = new BooleanCliOption(
					"include-db-type",
					null,
					false,
					"include database type to column mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateDbType);

			/// <summary>
			/// Generate database type length/size on entity columns mappings option.
			/// </summary>
			public static readonly CliOption LengthOnTables = new BooleanCliOption(
					"include-length",
					null,
					false,
					"include database type length/size to column mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateLength);

			/// <summary>
			/// Generate database type precision on entity columns mappings option.
			/// </summary>
			public static readonly CliOption PrecisionOnTables = new BooleanCliOption(
					"include-precision",
					null,
					false,
					"include database type precision to column mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.GeneratePrecision);

			/// <summary>
			/// Generate database type scale on entity columns mappings option.
			/// </summary>
			public static readonly CliOption ScaleOnTables = new BooleanCliOption(
					"include-scale",
					null,
					false,
					"include database type scale to column mappings",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateScale);

			/// <summary>
			/// Generate database information comment on data context class option.
			/// </summary>
			public static readonly CliOption EmitDbInfo = new BooleanCliOption(
					"include-db-info",
					null,
					false,
					"generate comment on data context with database information",
					"Information includes database name, data source and server version (when database provider expose this information).",
					null,
					null,
					_defaultOptions.DataModel.IncludeDatabaseInfo);

			/// <summary>
			/// Default data context contructor generation option.
			/// </summary>
			public static readonly CliOption EmitDefaultConstructor = new BooleanCliOption(
					"add-default-ctor",
					null,
					false,
					"generate default constructor on data context",
					null,
					null,
					null,
					_defaultOptions.DataModel.HasDefaultConstructor);

			/// <summary>
			/// Data context contructor with configuration parameter generation option.
			/// </summary>
			public static readonly CliOption EmitConfigurationConstructor = new BooleanCliOption(
					"add-configuration-ctor",
					null,
					false,
					"generate data context contructor with configuration name parameter",
					"Constructor example: public MyDataContext(string context) { ... }",
					null,
					null,
					_defaultOptions.DataModel.HasConfigurationConstructor);

			/// <summary>
			/// Data context contructor with non-generic options parameter generation option.
			/// </summary>
			public static readonly CliOption EmitOptionsConstructor = new BooleanCliOption(
					"add-options-ctor",
					null,
					false,
					"generate data context contructor with options parameter",
					"Constructor example: public MyDataContext(LinqToDbConnectionOptions options) { ... }",
					null,
					null,
					_defaultOptions.DataModel.HasUntypedOptionsConstructor);

			/// <summary>
			/// Data context contructor with generic options parameter generation option.
			/// </summary>
			public static readonly CliOption EmitTypedOptionsConstructor = new BooleanCliOption(
					"add-typed-options-ctor",
					null,
					false,
					"generate data context contructor with generic options parameter",
					"Constructor example: public MyDataContext(LinqToDbConnectionOptions<MyDataContext> options) { ... }",
					null,
					null,
					_defaultOptions.DataModel.HasTypedOptionsConstructor);

			/// <summary>
			/// Data context class name option.
			/// </summary>
			public static readonly CliOption DataContextName = new StringCliOption(
					"context-name",
					null,
					false,
					false,
					"class name for generated data context",
					"When not specified, database name used. When database name not available, \"MyDataContext\" used",
					null,
					null,
					new[] { "MyDataContext" });

			/// <summary>
			/// Base data context class option.
			/// </summary>
			public static readonly CliOption DataContextBaseClass = new StringCliOption(
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
					new[] { "LinqToDB.Data.DataConnection" });

			/// <summary>
			/// Generation of association properties on entity option.
			/// </summary>
			public static readonly CliOption EmitAssociations = new BooleanCliOption(
					"add-associations",
					null,
					false,
					"generate association properties on entities",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateAssociations);

			/// <summary>
			/// Generation of association extension methods for entity option.
			/// </summary>
			public static readonly CliOption EmitAssociationExtensions = new BooleanCliOption(
					"add-association-extensions",
					null,
					false,
					"generate association extension methods for entities",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateAssociationExtensions);

			/// <summary>
			/// Type of association return type for association back reference with many cardinality option.
			/// </summary>
			public static readonly CliOption AssociationCollectionType = new StringCliOption(
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
					new[] { "System.Collections.Generic.IEnumerable<>" });

			/// <summary>
			/// Reuse of known entity mappings for procedure/table function return record option.
			/// </summary>
			public static readonly CliOption ReuseEntitiesInFunctions = new BooleanCliOption(
					"reuse-entities-in-procedures",
					null,
					false,
					"allows use of entity mapping for return type of stored procedure/table function",
					"When procedure/function schema has same columns (by name, type and nullability) as known table/view entity, this option allows to use entity mapping for procedure/function return type. Otherwise separate mapping class will be generated for specific procedure/function.",
					null,
					null,
					_defaultOptions.DataModel.MapProcedureResultToEntity);

			/// <summary>
			/// Return <see cref="ITable{T}"/> for table function instead of <see cref="IQueryable{T}"/> option.
			/// </summary>
			public static readonly CliOption TableFunctionReturnsITable = new BooleanCliOption(
					"table-function-returns-table",
					null,
					false,
					"table functions use ITable<T> as return type, otherwise IQueryable<T> type used",
					null,
					null,
					null,
					_defaultOptions.DataModel.TableFunctionReturnsTable);

			/// <summary>
			/// Emit compilation error pragma for stored procedures/table functions with schema load errors option.
			/// </summary>
			public static readonly CliOption EmitSchemaErrors = new BooleanCliOption(
					"emit-schema-errors",
					null,
					false,
					"generate #error pragma for stored procedures and table functions with schema load errors",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateProceduresSchemaError);

			/// <summary>
			/// Skip mapping generation for stored procedure with schema load error option.
			/// </summary>
			public static readonly CliOption SkipProceduresWithSchemaErrors = new BooleanCliOption(
					"skip-procedures-with-schema-errors",
					null,
					false,
					"skips mapping generation for stored procedure with schema load errors, otherwise generate mapping without result table",
					null,
					null,
					null,
					_defaultOptions.DataModel.SkipProceduresWithSchemaErrors);

			/// <summary>
			/// Use <see cref="List{T}"/> over <see cref="IEnumerable{T}"/> as return type for stored procedure with result set option.
			/// </summary>
			public static readonly CliOption ReturnListFromProcedures = new BooleanCliOption(
					"procedure-returns-list",
					null,
					false,
					"use List<T> for stored procedure return dataset, otherwise IEnumerable<T> used",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateProcedureResultAsList);

			/// <summary>
			/// Emit database type name for stored procedure parameters option.
			/// </summary>
			public static readonly CliOption DbTypeInProcedures = new BooleanCliOption(
					"add-db-type-to-procedures",
					null,
					false,
					"include database type to stored procedure parameters",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateProcedureParameterDbType);

			/// <summary>
			/// Emit separate context-like classes for non-default schemas option.
			/// </summary>
			public static readonly CliOption SchemasAsTypes = new BooleanCliOption(
					"schema-as-type",
					null,
					false,
					"generate non-default schemas' code nested into separate class",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateSchemaAsType);

			/// <summary>
			/// Generate Find extension method for entity option.
			/// </summary>
			public static readonly CliOption GenerateFind = new BooleanCliOption(
					"find-methods",
					null,
					false,
					"enable generation of Find() extension methods to load entity by primary key value",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateFindExtensions);

			/// <summary>
			/// Order Find extension method parameters by primary key column ordinal instead of order by parameter name option.
			/// </summary>
			public static readonly CliOption FindParametersInOrdinalOrder = new BooleanCliOption(
					"order-find-parameters-by-ordinal",
					null,
					false,
					"use primary key column ordinal to order Find() extension method parameters (composite primary key only)",
					null,
					null,
					null,
					_defaultOptions.DataModel.OrderFindParametersByColumnOrdinal);

			/// <summary>
			/// Non-default schema name to use for schema class option.
			/// </summary>
			public static readonly CliOption SchemaTypeClassNames = new StringDictionaryCliOption(
					"schema-class-names",
					null,
					false,
					"provides schema context class/property names for non-default schemas (appliable only if schema-as-type option enabled)",
					"Without this option schema name will be used as base name to generate schema context/property names. Specifying this option in command line has limitations and it is recommended to use JSON for it instead because names in CLI cannot contain comma (,) or equality (=) characters, as they used as separators.",
					new[] { "--schema-class-names schema1=SchemaOne,schema2=SpecialSchema" },
					new[] { "{ \"dataModel\": { \"schema1\": \"SchemaOne\", \"schema2\": \"SpecialSchema\" } } }" });

			/// <summary>
			/// Data context class naming option.
			/// </summary>
			public static readonly CliOption DataContextClassNaming = DefineNamingOption(
				"data-context-class-name",
				"data context class naming options",
				_defaultOptions.DataModel.DataContextClassNameOptions);

			/// <summary>
			/// Entity class naming option.
			/// </summary>
			public static readonly CliOption EntityClassNaming = DefineNamingOption(
				"entity-class-name",
				"entity class naming options",
				_defaultOptions.DataModel.EntityClassNameOptions);
			
			/// <summary>
			/// Entity column property naming option.
			/// </summary>
			public static readonly CliOption EntityColumnPropertyNaming = DefineNamingOption(
				"entity-column-property-name",
				"entity column properties naming options",
				_defaultOptions.DataModel.EntityColumnPropertyNameOptions);
			
			/// <summary>
			/// Entity data context property naming option.
			/// </summary>
			public static readonly CliOption EntityContextPropertyNaming = DefineNamingOption(
				"entity-context-property-name",
				"entity table access data context property naming options",
				_defaultOptions.DataModel.EntityContextPropertyNameOptions);

			/// <summary>
			/// Association direct reference property/method naming option.
			/// </summary>
			public static readonly CliOption AssociationNaming = DefineNamingOption(
				"association-name",
				"association property or extension method naming options",
				_defaultOptions.DataModel.SourceAssociationPropertyNameOptions);
			
			/// <summary>
			/// Association back reference property/method naming option for non-many cardinality association.
			/// </summary>
			public static readonly CliOption AssocationBackReferenceSingleNaming = DefineNamingOption(
				"association-single-backreference-name",
				"association backreference property or extension method naming options for single-record cardinality",
				_defaultOptions.DataModel.TargetSingularAssociationPropertyNameOptions);

			/// <summary>
			/// Association back reference property/method naming option for many cardinality association.
			/// </summary>
			public static readonly CliOption AssocationBackReferenceManyNaming = DefineNamingOption(
				"association-multi-backreference-name",
				"association backreference property or extension method naming options for multi-record cardinality",
				_defaultOptions.DataModel.TargetMultipleAssociationPropertyNameOptions);

			/// <summary>
			/// Stored procedure or function mapping method naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncMethodNaming = DefineNamingOption(
				"proc-or-func-method-name",
				"procedure or function method naming options",
				_defaultOptions.DataModel.ProcedureNameOptions);
			
			/// <summary>
			/// Stored procedure or function mapping method parameters naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncParameterNaming = DefineNamingOption(
				"proc-or-func-param-name",
				"procedure or function method parameters naming options",
				_defaultOptions.DataModel.ProcedureParameterNameOptions);
			
			/// <summary>
			/// Stored procedure or table function result-set record class naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncResultClassNaming = DefineNamingOption(
				"proc-or-func-result-class-name",
				"procedure or table function custom result record mapping class naming options",
				_defaultOptions.DataModel.ProcedureResultClassNameOptions);
			
			/// <summary>
			/// Stored procedure or table function result-set record column property naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncResultColumnPropertyNaming = DefineNamingOption(
				"proc-or-func-result-property-name",
				"procedure or table function custom result record column property naming options. You probably don't want to specify this option, as it used for field with private visibility.",
				_defaultOptions.DataModel.ProcedureResultColumnPropertyNameOptions);
			
			/// <summary>
			/// Table function <see cref="MethodInfo"/> field naming option.
			/// </summary>
			public static readonly CliOption TableFunctionMethodInfoNaming = DefineNamingOption(
				"table-function-methodinfo-field-name",
				"table function FieldInfo field naming options",
				_defaultOptions.DataModel.TableFunctionMethodInfoFieldNameOptions);
			
			/// <summary>
			/// Scalar function with tuple return type tuple mapping class naming option.
			/// </summary>
			public static readonly CliOption FunctionTupleClassNaming = DefineNamingOption(
				"function-tuple-class-name",
				"tuple class naming options for scalar function with tuple return type",
				_defaultOptions.DataModel.FunctionTupleResultClassNameOptions);

			/// <summary>
			/// Scalar function with tuple return type tuple field property naming option.
			/// </summary>
			public static readonly CliOption FunctionTupleFieldPropertyNaming = DefineNamingOption(
				"function-tuple-class-field-name",
				"tuple class field naming options for scalar function with tuple return type",
				_defaultOptions.DataModel.FunctionTupleResultPropertyNameOptions);

			/// <summary>
			/// Non-default schema wrapper class naming option.
			/// </summary>
			public static readonly CliOption SchemaWrapperClassNaming = DefineNamingOption(
				"schema-class-name",
				"non-default schema wrapper class naming options",
				_defaultOptions.DataModel.SchemaClassNameOptions);
			
			/// <summary>
			/// Non-default schema data context property naming option.
			/// </summary>
			public static readonly CliOption SchemaContextPropertyNaming = DefineNamingOption(
				"schema-context-property-name",
				"non-default schema context property in main context naming options",
				_defaultOptions.DataModel.SchemaPropertyOptions);

			/// <summary>
			/// Find extension method parameters naming option.
			/// </summary>
			public static readonly CliOption FindParameterNaming = DefineNamingOption(
				"find-parameter-name",
				"Find extension method parameters naming options",
				_defaultOptions.DataModel.FindParameterNameOptions);

			private static NamingCliOption DefineNamingOption(string option, string help, NormalizationOptions? defaults)
			{
				return new NamingCliOption(
					option,
					help,
					NAMING_HELP,
					new[] { string.Format(NAMING_EXAMPLE_TEMPLATE, option) },
					defaults);
			}
		}

		/// <summary>
		/// Provides access to database schema scaffold options definitions.
		/// </summary>
		public static class SchemaOptions
		{
			/// <summary>
			/// Schema objects to load option.
			/// </summary>
			public static readonly CliOption LoadedObjects = new StringEnumCliOption(
					"objects",
					null,
					false,
					true,
					"schema objects to load",
					null,
					new[] { "--objects table,stored-procedure,table-function" },
					new[] { "{ \"schema\": { \"objects\": [\"table\", \"view\", \"table-function\"] } }" },
					false,
					new StringEnumOption[]
					{
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.Table            ) != 0, "table"             , "load tables"                    ),
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.View             ) != 0, "view"              , "load views"                     ),
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.ForeignKey       ) != 0, "foreign-key"       , "load foreign key constrains"    ),
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.StoredProcedure  ) != 0, "stored-procedure"  , "load stored procedures"         ),
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.ScalarFunction   ) != 0, "scalar-function"   , "load scalar functions"          ),
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.TableFunction    ) != 0, "table-function"    , "load table functions"           ),
						new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.AggregateFunction) != 0, "aggregate-function", "load aggregate/window functions"),
					});

			/// <summary>
			/// Prefer provider-specific types over general .net types for columns and parameters option.
			/// </summary>
			public static readonly CliOption PreferProviderTypes = new BooleanCliOption(
					"prefer-provider-types",
					null,
					false,
					"prefer provider-specific data types to regular .net types for columns and parameters",
					"Database provider could have db-specific .net types to better mapping handling to database types. E.g. MySqlGeometry, NpgsqlInet or OracleTimeStampLTZ types, provided by corresponding providers.",
					null,
					null,
					_defaultOptions.Schema.PreferProviderSpecificTypes);

			/// <summary>
			/// Skip load (and association generation) for duplicate foreign keys with same set of column option.
			/// </summary>
			public static readonly CliOption IgnoreDuplicateFKs = new BooleanCliOption(
					"ignore-duplicate-fk",
					null,
					false,
					"load only first (or order, returned by database) foreign key with same columns",
					null,
					null,
					null,
					_defaultOptions.Schema.IgnoreDuplicateForeignKeys);

			/// <summary>
			/// Explicit list of schemas to load option.
			/// </summary>
			public static readonly CliOption IncludedSchemas = new StringCliOption(
					"include-schemas",
					null,
					false,
					true,
					"load only specified database schemas",
					null,
					null,
					null,
					null);

			/// <summary>
			/// List of schemas to skip option.
			/// </summary>
			public static readonly CliOption ExcludedSchemas = new StringCliOption(
					"exclude-schemas",
					null,
					false,
					true,
					"do not load specified database schemas",
					null,
					null,
					null,
					null);

			/// <summary>
			/// Explicit list of catalogs/databases for schema load option.
			/// </summary>
			public static readonly CliOption IncludedCatalogs = new StringCliOption(
					"include-catalogs",
					null,
					false,
					true,
					"load only specified database schemas",
					null,
					null,
					null,
					null);

			/// <summary>
			/// List of catalogs/databases to exclude from schema load.
			/// </summary>
			public static readonly CliOption ExcludedCatalogs = new StringCliOption(
					"exclude-catalogs",
					null,
					false,
					true,
					"do not load specified database schemas",
					null,
					null,
					null,
					null);

			/// <summary>
			/// Use only safe schema load methods to load table function and store procedure result-set schema option.
			/// </summary>
			public static readonly CliOption UseSafeSchemaLoadOnly = new BooleanCliOption(
					"safe-schema-only",
					null,
					false,
					"load stored procedure/table function schema using only safe methods",
					@"Don't use CommandBehavior.SchemaOnly execution mode to load stored procedure or table function schema as it is not safe if them contain non-transactional code.",
					null,
					null,
					_defaultOptions.Schema.UseSafeSchemaLoad);

			/// <summary>
			/// Load result-set schema for stored procedures option.
			/// </summary>
			public static readonly CliOption LoadProcedureSchema = new BooleanCliOption(
					"load-sproc-schema",
					null,
					false,
					"enable loading stored procedure schema",
					@"When not set, procedures and their parameters will be loaded, but not result-set schema.",
					null,
					null,
					_defaultOptions.Schema.LoadProceduresSchema);

			/// <summary>
			/// Table load filter option.
			/// </summary>
			public static readonly CliOption IncludedTables = new ObjectNameFilterCliOption(
					"include-tables",
					null,
					false,
					"only load tables with specified name(s)",
					@"Provided table names should have same casing as actual table name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for table;
  - table name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - table schema (schema property);
  - regular expression (regex property) instead of exact table name (name property).
JSON list element schema:
{
    ""name""  : string  // table name
    ""schema"": string? // table schema (optional)
}
|
{
    ""regex"" : string  // table name matching regular expression
    ""schema"": string? // table schema (optional)
}
|
string // also you can put table name as string directly to list
",
					new[] { "--include-tables Users,Roles,Permissions" },
					new[]
					{
						"{ \"schema\": { \"include-tables\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles tables",
						"{ \"schema\": { \"include-tables\": [ { \"regex\": \"^audit_.$+\", \"schema\": \"dbo\" } ] } } // all tables starting from audit_ prefix"
					});

			/// <summary>
			/// Table skip filter option.
			/// </summary>
			public static readonly CliOption ExcludedTables = new ObjectNameFilterCliOption(
					"exclude-tables",
					null,
					false,
					"skip load of tables with specified name(s)",
					@"Provided table names should have same casing as actual table name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for table;
  - table name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - table schema (schema property);
  - regular expression (regex property) instead of exact table name (name property).
JSON list element schema:
{
    ""name""  : string  // table name
    ""schema"": string? // table schema (optional)
}
|
{
    ""regex"" : string  // table  name matching regular expression
    ""schema"": string? // table schema (optional)
}
|
string // also you can put table name as string directly to list
",
					new[] { "--exclude-tables Users,Roles,Permissions" },
					new[]
					{
						"{ \"schema\": { \"exclude-tables\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles tables ignored",
						"{ \"schema\": { \"exclude-tables\": [ { \"regex\": \"^audit_.$+\", \"schema\": \"dbo\" } ] } } // all tables starting from audit_ prefix ignored"
					});

			/// <summary>
			/// View load filter option.
			/// </summary>
			public static readonly CliOption IncludedViews = new ObjectNameFilterCliOption(
					"include-views",
					null,
					false,
					"only load views with specified name(s)",
					@"Provided view names should have same casing as actual view name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for view;
  - view name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - view schema (schema property);
  - regular expression (regex property) instead of exact view name (name property).
JSON list element schema:
{
    ""name""  : string  // view name
    ""schema"": string? // view schema (optional)
}
|
{
    ""regex"" : string  // view name matching regular expression
    ""schema"": string? // view schema (optional)
}
|
string // also you can put view name as string directly to list
",
					new[] { "--include-views Users,Roles,Permissions" },
					new[]
					{
						"{ \"schema\": { \"include-views\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles views",
						"{ \"schema\": { \"include-views\": [ { \"regex\": \"^audit_.$+\", \"schema\": \"dbo\" } ] } } // all views starting from audit_ prefix"
					});

			/// <summary>
			/// View skip filter option.
			/// </summary>
			public static readonly CliOption ExcludedViews = new ObjectNameFilterCliOption(
					"exclude-views",
					null,
					false,
					"skip load of views with specified name(s)",
					@"Provided view names should have same casing as actual view name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for view;
  - view name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - view schema (schema property);
  - regular expression (regex property) instead of exact view name (name property).
JSON list element schema:
{
    ""name""  : string  // view name
    ""schema"": string? // view schema (optional)
}
|
{
    ""regex"" : string  // view name matching regular expression
    ""schema"": string? // view schema (optional)
}
|
string // also you can put view name as string directly to list
",
					new[] { "--exclude-views Users,Roles,Permissions" },
					new[]
					{
						"{ \"schema\": { \"exclude-views\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles views ignored",
						"{ \"schema\": { \"exclude-views\": [ { \"regex\": \"^audit_.$+\", \"schema\": \"dbo\" } ] } } // all views starting from audit_ prefix ignored"
					});

			/// <summary>
			/// Procedure filter for result-set schema load option.
			/// </summary>
			public static readonly CliOption ProceduresWithSchema = new ObjectNameFilterCliOption(
					"procedures-with-schema",
					null,
					false,
					"only load schema for stored procedures with specified name(s)",
					@"Provided stored procedure names should have same casing as actual procedure name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for procedure;
  - procedure name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - procedure schema (schema property);
  - regular expression (regex property) instead of exact procedure name (name property).
JSON list element schema:
{
    ""name""  : string  // stored procedure name
    ""schema"": string? // stored procedure schema (optional)
}
|
{
    ""regex"" : string  // stored procedure name matching regular expression
    ""schema"": string? // stored procedure schema (optional)
}
|
string // also you can put procedure name as string directly to list
",
					new[] { "--procedures-with-schema GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						"{ \"schema\": { \"procedures-with-schema\": [ \"GetUsers\", { \"name\": \"LoadPermissions\", \"schema\": \"dbo\" } ] } } // GetUsers and dbo.LoadPermissions procedures",
						"{ \"schema\": { \"procedures-with-schema\": [ { \"regex\": \"^Load.$+\", \"schema\": \"dbo\" } ] } } // all procedures starting from Load prefix"
					});

			/// <summary>
			/// Procedure filter for result-set schema load skip option.
			/// </summary>
			public static readonly CliOption ProceduresWithoutSchema = new ObjectNameFilterCliOption(
					"procedures-without-schema",
					null,
					false,
					"skip load of schema for stored procedures with specified name(s)",
					@"Provided stored procedure names should have same casing as actual procedure name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for procedure;
  - procedure name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - procedure schema (schema property);
  - regular expression (regex property) instead of exact procedure name (name property).
JSON list element schema:
{
    ""name""  : string  // stored procedure name
    ""schema"": string? // stored procedure schema (optional)
}
|
{
    ""regex"" : string  // stored procedure name matching regular expression
    ""schema"": string? // stored procedure schema  (optional)
}
|
string // also you can put procedure name as string directly to list
",
					new[] { "--procedures-without-schema FormatAllDrives,DropAllTables" },
					new[]
					{
						"{ \"schema\": { \"procedures-without-schema\": [ \"DropAllTables\", { \"name\": \"FormatAllDrives\", \"schema\": \"dbo\" } ] } } // DropAllTables and dbo.FormatAllDrives procedures schema not loaded",
						"{ \"schema\": { \"procedures-without-schema\": [ { \"regex\": \"^Delete.$+\", \"schema\": \"dbo\" } ] } } // all procedures starting from Delete prefix"
					});

			/// <summary>
			/// Table function load filter option.
			/// </summary>
			public static readonly CliOption IncludedTableFunctions = new ObjectNameFilterCliOption(
					"include-table-functions",
					null,
					false,
					"only load table functions with specified name(s)",
					@"Provided table functions names should have same casing as actual function name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for table function;
  - table function name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - table function schema (schema property);
  - regular expression (regex property) instead of exact table function name (name property).
JSON list element schema:
{
    ""name""  : string  // table function name
    ""schema"": string? // table function schema (optional)
}
|
{
    ""regex"" : string  // table function name matching regular expression
    ""schema"": string? // table function schema (optional)
}
|
string // also you can put table function name as string directly to list
",
					new[] { "--include-table-functions GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						"{ \"schema\": { \"include-table-functions\": [ \"ActiveUsers\", { \"name\": \"InactiveUsers\", \"schema\": \"dbo\" } ] } } // ActiveUsers and dbo.InactiveUsers functions",
						"{ \"schema\": { \"include-table-functions\": [ { \"regex\": \"^Query.$+\", \"schema\": \"dbo\" } ] } } // all table functions starting from Query prefix"
					});

			/// <summary>
			/// Table function skip filter option.
			/// </summary>
			public static readonly CliOption ExcludedTableFunctions = new ObjectNameFilterCliOption(
					"exclude-table-functions",
					null,
					false,
					"skip load of table functions with specified name(s)",
					@"Provided table functions names should have same casing as actual function name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for table function;
  - table function name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - table function schema (schema property);
  - regular expression (regex property) instead of exact table function name (name property).
JSON list element schema:
{
    ""name""  : string  // table function name
    ""schema"": string? // table function schema (optional)
}
|
{
    ""regex"" : string  // table function name matching regular expression
    ""schema"": string? // table function schema  (optional)
}
|
string // also you can put table function name as string directly to list
",
					new[] { "--exclude-table-functions GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						"{ \"schema\": { \"exclude-table-functions\": [ \"TestFunction\", { \"name\": \"CheckDb\", \"schema\": \"dbo\" } ] } } // TestFunction and dbo.CheckDb functions ignored",
						"{ \"schema\": { \"exclude-table-functions\": [ { \"regex\": \"^Audit.$+\", \"schema\": \"dbo\" } ] } } // all table functions starting from Audit prefix ignored"
					});
		}

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

		public static CliCommand Instance { get; } = new ScaffoldCommand();
	}
}
