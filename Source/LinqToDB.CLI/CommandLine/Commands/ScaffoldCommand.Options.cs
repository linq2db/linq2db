using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;

using LinqToDB.DataModel;
using LinqToDB.Metadata;
using LinqToDB.Naming;
using LinqToDB.Scaffold;

namespace LinqToDB.CommandLine
{
	partial class ScaffoldCommand : CliCommand
	{
		private static readonly OptionCategory _generalOptions        = new (1, "General"        , "basic options"           , "general"  );
		private static readonly OptionCategory _schemaOptions         = new (2, "Database Schema", "database schema load"    , "schema"   );
		private static readonly OptionCategory _dataModelOptions      = new (3, "Data Model"     , "data model configuration", "dataModel");
		private static readonly OptionCategory _codeGenerationOptions = new (4, "Code Generation", "code-generation options" , "code"     );
		private static readonly ScaffoldOptions _defaultOptions       = ScaffoldOptions.Default();
		private static readonly ScaffoldOptions _t4ModeOptions        = ScaffoldOptions.T4();

		/// <summary>
		/// Provides access to general scaffold options definitions.
		/// </summary>
		internal static class General
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
					false,
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
					new (false, false, DatabaseType.Access         .ToString(), "MS Access (requires OLE DB or/and ODBC provider installed)"),
					new (false, false, DatabaseType.DB2            .ToString(), "IBM DB2 LUW or z/OS"                                       ),
					new (false, false, DatabaseType.Firebird       .ToString(), "Firebird"                                                  ),
					new (false, false, DatabaseType.Informix       .ToString(), "IBM Informix"                                              ),
					new (false, false, DatabaseType.SQLServer      .ToString(), "MS SQL Server (including Azure SQL Server)"                ),
					new (false, false, DatabaseType.MySQL          .ToString(), "MySQL/MariaDB"                                             ),
					new (false, false, DatabaseType.Oracle         .ToString(), "Oracle Database"                                           ),
					new (false, false, DatabaseType.PostgreSQL     .ToString(), "PostgreSQL"                                                ),
					new (false, false, DatabaseType.SqlCe          .ToString(), "MS SQL Server Compact"                                     ),
					new (false, false, DatabaseType.SQLite         .ToString(), "SQLite"                                                    ),
					new (false, false, DatabaseType.Sybase         .ToString(), "SAP/Sybase ASE"                                            ),
					new (false, false, DatabaseType.SapHana        .ToString(), "SAP HANA"                                                  ),
					new (false, false, DatabaseType.ClickHouseMySql.ToString(), "ClickHouse (MySql interface)"                              ),
					new (false, false, DatabaseType.ClickHouseHttp .ToString(), "ClickHouse (HTTP(S) interface)"                            ),
					new (false, false, DatabaseType.ClickHouseTcp  .ToString(), "ClickHouse (TCP/binary interface)"                         ),
					new (false, false, DatabaseType.Custom		   .ToString(), "Custom provider (requires provider-name and provider-location)"));

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
					null,
					null);

			/// <summary>
			/// Custom Provider name option.
			/// </summary>
			public static readonly CliOption ProviderName = new StringCliOption(
					"provider-name",
					null,
					false,
					false,
					"custom database provider name",
					@"Allows user to specify custom provider unique name.",
					null,
					null,
					null,
					null);

			/// <summary>
			/// ProviderDetector option.
			/// </summary>
			public static readonly CliOption ProviderDetectorClass = new StringCliOption(
					"provider-detector-class",
					null,
					false,
					false,
					"provider detector class",
					@"Allows user to specify class where to find implementation of provider detector.",
					null,
					null,
					null,
					null);

			/// <summary>
			/// ProviderDetector option.
			/// </summary>
			public static readonly CliOption ProviderDetectorMethod = new StringCliOption(
					"provider-detector-method",
					null,
					false,
					false,
					"provider detector method",
					@"Allows user to specify method name for provider detector.",
					null,
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
					new (false, false, "x86", "x86 architecture"),
					new (false, false, "x64", "x64 architecture"));

			/// <summary>
			/// Base options template option.
			/// </summary>
			public static readonly CliOption OptionsTemplate = new StringEnumCliOption(
					"template",
					't',
					false,
					false,
					"select base set of default options",
					"Specify this option only if you want to to use scaffolding options, similar to used by old T4 templates by default",
					null,
					null,
					false,
					new (true , true , "default", "set of parameters, used by default (as specified in option help)"),
					new (false, false, "t4"     , "set of parameters, similar to T4 defaults (compat. option)"      ));

			/// <summary>
			/// T4 template or custom assembly path option.
			/// </summary>
			public static readonly CliOption Interceptors = new StringCliOption(
					"customize",
					null,
					false,
					false,
					"specify path to T4 template or assembly with scaffolding customization logic",
					@$"Option accepts path to file with customization logic which could be:
- assembly (recognized by .dll extension);
- T4 template.

If you choose T4, you can create initial empty template using 'dotnet linq2db template' command. It will generate initial template file with pre-generated extension points which you can modify to implement required customizations.
Customization using compiled assembly has several requirements:
- it should be compatible with current runtime, used by 'dotnet linq2db' tool;
- assembly should contain exactly one interceptor class with customization logic. It should be inherited from {nameof(ScaffoldInterceptors)} and has default public constructor;
- linq2db.Tools version should match tool's version to avoid possible compatibility issues/errors.",
					null,
					null,
					null,
					null);
		}

		/// <summary>
		/// Provides access to code-generation scaffold options definitions.
		/// </summary>
		internal static class CodeGen
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
					_defaultOptions.CodeGeneration.EnableNullableReferenceTypes,
					_t4ModeOptions.CodeGeneration.EnableNullableReferenceTypes);

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
					new[] { "\\t" },
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
					/*lang=json,strict*/
					new[] { @"{ ""code"": { ""new-line"": ""\r\n"" } }" },
					new[] { "value of Environment.NewLine" },
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
					_defaultOptions.CodeGeneration.SuppressMissingXmlDocWarnings,
					_t4ModeOptions.CodeGeneration.SuppressMissingXmlDocWarnings);

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
					_defaultOptions.CodeGeneration.MarkAsAutoGenerated,
					_t4ModeOptions.CodeGeneration.MarkAsAutoGenerated);

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
					!_defaultOptions.CodeGeneration.ClassPerFile,
					!_t4ModeOptions.CodeGeneration.ClassPerFile);

			/// <summary>
			/// Single-file generation mode option.
			/// </summary>
			public static readonly CliOption AddGeneratedFileSuffix = new BooleanCliOption(
					"generated-suffix",
					null,
					false,
					"adds \".generated.<extension>\" suffix to generated files",
					null,
					null,
					null,
					_defaultOptions.CodeGeneration.AddGeneratedFileSuffix,
					_t4ModeOptions.CodeGeneration.AddGeneratedFileSuffix);

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
					/*lang=json,strict*/
					new[] { @"{ ""code"": { ""conflicts"": [""My.Namespace.SomeType+ConflictingNestedType"", ""Some.ConflictingNamespace.Or.Type""] } }" },
					null,
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
					_defaultOptions.CodeGeneration.Namespace != null ? new[] { _defaultOptions.CodeGeneration.Namespace } : null,
					_t4ModeOptions .CodeGeneration.Namespace != null ? new[] { _t4ModeOptions .CodeGeneration.Namespace } : null);
		}

		/// <summary>
		/// Provides access to data model scaffold options definitions.
		/// </summary>
		internal static class DataModel
		{
			/// <summary>
			/// Naming option help text.
			/// </summary>
			private const string NAMING_HELP = @"Naming options could be specified only in JSON file and defined as object with following properties:
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
    + ""none""                : no transformations applied, treat whole identifier as one word
    + ""split_by_underscore"" : split base name, got from database object name, into separate words by underscore (_)
    + ""association""         : emulation of identifier generation logic for association name used by T4 templates (compat. option)
- pluralize_if_ends_with_word_only : bool    : when set, pluralization not applied if name ends with non-word (e.g. with digit)
- ignore_all_caps                  : bool    : when set, casing not applied to names that contain only uppercase letters
If you don't specify some property, CLI will use default value for current option. This allows you to override only some properties without need to specify all properties.
";

			/*lang=json,strict*/
			/// <summary>
			/// Naming option example template.
			/// </summary>
#pragma warning disable JSON001 // Invalid JSON pattern
#if SUPPORTS_COMPOSITE_FORMAT
			private static readonly CompositeFormat NAMING_EXAMPLE_TEMPLATE = CompositeFormat.Parse(
 @"
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
}}");
#else
			private const string NAMING_EXAMPLE_TEMPLATE =
 @"
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
#endif
#pragma warning restore JSON001 // Invalid JSON pattern

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
					_defaultOptions.DataModel.IncludeDatabaseName,
					_t4ModeOptions.DataModel.IncludeDatabaseName);

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
					_defaultOptions.DataModel.GenerateDefaultSchema,
					_t4ModeOptions.DataModel.GenerateDefaultSchema);

			/// <summary>
			/// Specifies type of generated metadata source.
			/// </summary>
			public static readonly CliOption Metadata = new StringEnumCliOption(
					"metadata",
					null,
					false,
					false,
					"specify type of generated metadata",
					null,
					null,
					null,
					false,
					new StringEnumOption(_defaultOptions.DataModel.Metadata == MetadataSource.None         , _t4ModeOptions.DataModel.Metadata == MetadataSource.None         , "none"      , "don't emit metadata for model"         ),
					new StringEnumOption(_defaultOptions.DataModel.Metadata == MetadataSource.Attributes   , _t4ModeOptions.DataModel.Metadata == MetadataSource.Attributes   , "attributes", "annotate model with mapping attributes"),
					new StringEnumOption(_defaultOptions.DataModel.Metadata == MetadataSource.FluentMapping, _t4ModeOptions.DataModel.Metadata == MetadataSource.FluentMapping, "fluent"    , "annotate model using fluent mapping"   ));

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
						/*lang=json,strict*/
						"{ \"dataModel\": { \"base-entity\": \"My.Namespace.MyBaseEntity\" } }",
						/*lang=json,strict*/
						"{ \"dataModel\": { \"base-entity\": \"My.Namespace.ParentClass+MyBaseNestedEntity\" } }",
					},
					null,
					null);

			/// <summary>
			/// Enables partial class modifier on entity mapping classes.
			/// </summary>
			public static readonly CliOption EntityClassIsPartial = new BooleanCliOption(
					"partial-entities",
					null,
					false,
					"when set to true, generates partial class modifier on entity mapping classes",
					null,
					null,
					null,
					_defaultOptions.DataModel.EntityClassIsPartial,
					_t4ModeOptions.DataModel.EntityClassIsPartial);

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
					_defaultOptions.DataModel.GenerateDataType,
					_t4ModeOptions.DataModel.GenerateDataType);

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
					_defaultOptions.DataModel.GenerateDbType,
					_t4ModeOptions.DataModel.GenerateDbType);

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
					_defaultOptions.DataModel.GenerateLength,
					_t4ModeOptions.DataModel.GenerateLength);

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
					_defaultOptions.DataModel.GeneratePrecision,
					_t4ModeOptions.DataModel.GeneratePrecision);

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
					_defaultOptions.DataModel.GenerateScale,
					_t4ModeOptions.DataModel.GenerateScale);

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
					_defaultOptions.DataModel.IncludeDatabaseInfo,
					_t4ModeOptions.DataModel.IncludeDatabaseInfo);

			/// <summary>
			/// Generate InitDataContext partial method on data context class option.
			/// </summary>
			public static readonly CliOption EmitInitDataContextMethod = new BooleanCliOption(
					"add-init-context",
					null,
					false,
					"generate InitDataContext partial method on data context for custom context setup",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateInitDataContextMethod,
					_t4ModeOptions.DataModel.GenerateInitDataContextMethod);

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
					_defaultOptions.DataModel.HasDefaultConstructor,
					_t4ModeOptions.DataModel.HasDefaultConstructor);

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
					_defaultOptions.DataModel.HasConfigurationConstructor,
					_t4ModeOptions.DataModel.HasConfigurationConstructor);

			/// <summary>
			/// Data context contructor with non-generic options parameter generation option.
			/// </summary>
			public static readonly CliOption EmitOptionsConstructor = new BooleanCliOption(
					"add-options-ctor",
					null,
					false,
					"generate data context contructor with options parameter",
					$"Constructor example: public MyDataContext({nameof(DataOptions)} options) {{ ... }}",
					null,
					null,
					_defaultOptions.DataModel.HasUntypedOptionsConstructor,
					_t4ModeOptions.DataModel.HasUntypedOptionsConstructor);

			/// <summary>
			/// Data context contructor with generic options parameter generation option.
			/// </summary>
			public static readonly CliOption EmitTypedOptionsConstructor = new BooleanCliOption(
					"add-typed-options-ctor",
					null,
					false,
					"generate data context contructor with generic options parameter",
					$"Constructor example: public MyDataContext({nameof(DataOptions)}<MyDataContext> options) {{ ... }}",
					null,
					null,
					_defaultOptions.DataModel.HasTypedOptionsConstructor,
					_t4ModeOptions.DataModel.HasTypedOptionsConstructor);

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
					new[] { "MyDataContext" },
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
						/*lang=json,strict*/
						"{ \"dataModel\": { \"base-context\": \"LinqToDB.DataContext\" } }"
					},
					new[] { "LinqToDB.Data.DataConnection" },
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
					_defaultOptions.DataModel.GenerateAssociations,
					_t4ModeOptions.DataModel.GenerateAssociations);

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
					_defaultOptions.DataModel.GenerateAssociationExtensions,
					_t4ModeOptions.DataModel.GenerateAssociationExtensions);

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
						/*lang=json,strict*/
						"{ \"dataModel\": { \"association-collection\": \"[]\" } }",
						/*lang=json,strict*/
						"{ \"dataModel\": { \"association-collection\": \"System.Collections.Generic.List<>\" } }",
					},
					new[] { "System.Collections.Generic.IEnumerable<>" },
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
					_defaultOptions.DataModel.MapProcedureResultToEntity,
					_t4ModeOptions.DataModel.MapProcedureResultToEntity);

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
					_defaultOptions.DataModel.TableFunctionReturnsTable,
					_t4ModeOptions.DataModel.TableFunctionReturnsTable);

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
					_defaultOptions.DataModel.GenerateProceduresSchemaError,
					_t4ModeOptions.DataModel.GenerateProceduresSchemaError);

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
					_defaultOptions.DataModel.SkipProceduresWithSchemaErrors,
					_t4ModeOptions.DataModel.SkipProceduresWithSchemaErrors);

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
					_defaultOptions.DataModel.GenerateProcedureResultAsList,
					_t4ModeOptions.DataModel.GenerateProcedureResultAsList);

			/// <summary>
			/// Specify stored procedure sync/async mapping generation option.
			/// </summary>
			public static readonly CliOption StoredProcedureTypes = new StringEnumCliOption(
					"procedure-types",
					null,
					false,
					true,
					"enables generation of sync and async versions of stored procedure mapping",
					null,
					null,
					null,
					false,
					new StringEnumOption(_defaultOptions.DataModel.GenerateProcedureSync , _t4ModeOptions.DataModel.GenerateProcedureSync , "sync" , "generate sync stored procedure call mappings" ),
					new StringEnumOption(_defaultOptions.DataModel.GenerateProcedureAsync, _t4ModeOptions.DataModel.GenerateProcedureAsync, "async", "generate async stored procedure call mappings"));

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
					_defaultOptions.DataModel.GenerateProcedureParameterDbType,
					_t4ModeOptions.DataModel.GenerateProcedureParameterDbType);

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
					_defaultOptions.DataModel.GenerateSchemaAsType,
					_t4ModeOptions.DataModel.GenerateSchemaAsType);

			/// <summary>
			/// Generates <see cref="IEquatable{T}"/> interface implementation on entity class for entity with primary key columns.
			/// </summary>
			public static readonly CliOption GenerateIEquatable = new BooleanCliOption(
					"equatable-entities",
					null,
					false,
					"entity classes will implement IEquatable<T> interface using primary key column(s) for identity calculation. Requires reference to linq2db.Tools nuget from generated code.",
					null,
					null,
					null,
					_defaultOptions.DataModel.GenerateIEquatable,
					_t4ModeOptions.DataModel.GenerateIEquatable);

			/// <summary>
			/// Generate Find extension method for entity option.
			/// </summary>
			public static readonly CliOption GenerateFind = new StringEnumCliOption(
					"find-methods",
					null,
					false,
					true,
					"enable generation of extension methods to access entity by primary key",
					null,
					null,
					null,
					false,
					new StringEnumOption( _defaultOptions.DataModel.GenerateFindExtensions                                         == 0                                   ,  _t4ModeOptions.DataModel.GenerateFindExtensions                                         == 0                                   , "none"                , "disable generation of Find extensions. Cannot be used with other values"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindByPkOnTable           ) == FindTypes.FindByPkOnTable           , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindByPkOnTable           ) == FindTypes.FindByPkOnTable           , "sync-pk-table"       , "generate sync entity load extension on table object with primary key parameters: Entity?            Find     (this ITable<Entity> table, pk_fields)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByPkOnTable      ) == FindTypes.FindAsyncByPkOnTable      , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByPkOnTable      ) == FindTypes.FindAsyncByPkOnTable      , "async-pk-table"      , "generate sync entity load extension on table object with primary key parameters: Task<Entity?>      FindAsync(this ITable<Entity> table, pk_fields, CancellationToken)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByPkOnTable      ) == FindTypes.FindQueryByPkOnTable      , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByPkOnTable      ) == FindTypes.FindQueryByPkOnTable      , "query-pk-table"      , "generate entity query extension on table object with primary key parameters    : IQueryable<Entity> FindQuery(this ITable<Entity> table, pk_fields)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindByPkOnContext         ) == FindTypes.FindByPkOnContext         , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindByPkOnContext         ) == FindTypes.FindByPkOnContext         , "sync-pk-context"     , "generate sync entity load extension on table object with primary key parameters: Entity?            Find     (this DataContext    db   , pk_fields)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByPkOnContext    ) == FindTypes.FindAsyncByPkOnContext    , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByPkOnContext    ) == FindTypes.FindAsyncByPkOnContext    , "async-pk-context"    , "generate sync entity load extension on table object with primary key parameters: Task<Entity?>      FindAsync(this DataContext    db   , pk_fields, CancellationToken)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByPkOnContext    ) == FindTypes.FindQueryByPkOnContext    , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByPkOnContext    ) == FindTypes.FindQueryByPkOnContext    , "query-pk-context"    , "generate entity query extension on table object with primary key parameters    : IQueryable<Entity> FindQuery(this DataContext    db   , pk_fields)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindByRecordOnTable       ) == FindTypes.FindByRecordOnTable       , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindByRecordOnTable       ) == FindTypes.FindByRecordOnTable       , "sync-entity-table"   , "generate sync entity load extension on table object with entity parameter      : Entity?            Find     (this ITable<Entity> table, Entity row)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByRecordOnTable  ) == FindTypes.FindAsyncByRecordOnTable  , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByRecordOnTable  ) == FindTypes.FindAsyncByRecordOnTable  , "async-entity-table"  , "generate sync entity load extension on table object with entity parameter      : Task<Entity?>      FindAsync(this ITable<Entity> table, Entity row, CancellationToken)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByRecordOnTable  ) == FindTypes.FindQueryByRecordOnTable  , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByRecordOnTable  ) == FindTypes.FindQueryByRecordOnTable  , "query-entity-table"  , "generate entity query extension on table object with entity parameter          : IQueryable<Entity> FindQuery(this ITable<Entity> table, Entity row)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindByRecordOnContext     ) == FindTypes.FindByRecordOnContext     , (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindByRecordOnContext     ) == FindTypes.FindByRecordOnContext     , "sync-entity-context" , "generate sync entity load extension on generated context with entity parameter : Entity?            Find     (this DataContext>   db   , Entity row)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByRecordOnContext) == FindTypes.FindAsyncByRecordOnContext, (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindAsyncByRecordOnContext) == FindTypes.FindAsyncByRecordOnContext, "async-entity-context", "generate sync entity load extension on generated context with entity parameter : Task<Entity?>      FindAsync(this DataContext    db   , Entity row, CancellationToken)"),
					new StringEnumOption((_defaultOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByRecordOnContext) == FindTypes.FindQueryByRecordOnContext, (_t4ModeOptions.DataModel.GenerateFindExtensions & FindTypes.FindQueryByRecordOnContext) == FindTypes.FindQueryByRecordOnContext, "query-entity-context", "generate entity query extension on generated context with entity parameter     : IQueryable<Entity> FindQuery(this DataContext    db   , Entity row)"));

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
					_defaultOptions.DataModel.OrderFindParametersByColumnOrdinal,
					_t4ModeOptions.DataModel.OrderFindParametersByColumnOrdinal);

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
					/*lang=json,strict*/
					new[] { "{ \"dataModel\": { \"schema1\": \"SchemaOne\", \"schema2\": \"SpecialSchema\" } }" });

			/// <summary>
			/// Data context class naming option (for autogenerated names only).
			/// </summary>
			public static readonly CliOption DataContextClassNaming = DefineNamingOption(
				"data-context-class-name",
				"data context class naming options (for auto-generated names only)",
				_defaultOptions.DataModel.DataContextClassNameOptions,
				_t4ModeOptions.DataModel.DataContextClassNameOptions);

			/// <summary>
			/// Entity class naming option.
			/// </summary>
			public static readonly CliOption EntityClassNaming = DefineNamingOption(
				"entity-class-name",
				"entity class naming options",
				_defaultOptions.DataModel.EntityClassNameOptions,
				_t4ModeOptions.DataModel.EntityClassNameOptions);

			/// <summary>
			/// Entity column property naming option.
			/// </summary>
			public static readonly CliOption EntityColumnPropertyNaming = DefineNamingOption(
				"entity-column-property-name",
				"entity column properties naming options",
				_defaultOptions.DataModel.EntityColumnPropertyNameOptions,
				_t4ModeOptions.DataModel.EntityColumnPropertyNameOptions);

			/// <summary>
			/// Entity data context property naming option.
			/// </summary>
			public static readonly CliOption EntityContextPropertyNaming = DefineNamingOption(
				"entity-context-property-name",
				"entity table access data context property naming options",
				_defaultOptions.DataModel.EntityContextPropertyNameOptions,
				_t4ModeOptions.DataModel.EntityContextPropertyNameOptions);

			/// <summary>
			/// Association direct reference property/method naming option.
			/// </summary>
			public static readonly CliOption AssociationNaming = DefineNamingOption(
				"association-name",
				"association property or extension method naming options",
				_defaultOptions.DataModel.SourceAssociationPropertyNameOptions,
				_t4ModeOptions.DataModel.SourceAssociationPropertyNameOptions);

			/// <summary>
			/// Association back reference property/method naming option for non-many cardinality association.
			/// </summary>
			public static readonly CliOption AssocationBackReferenceSingleNaming = DefineNamingOption(
				"association-single-backreference-name",
				"association backreference property or extension method naming options for single-record cardinality",
				_defaultOptions.DataModel.TargetSingularAssociationPropertyNameOptions,
				_t4ModeOptions.DataModel.TargetSingularAssociationPropertyNameOptions);

			/// <summary>
			/// Association back reference property/method naming option for many cardinality association.
			/// </summary>
			public static readonly CliOption AssocationBackReferenceManyNaming = DefineNamingOption(
				"association-multi-backreference-name",
				"association backreference property or extension method naming options for multi-record cardinality",
				_defaultOptions.DataModel.TargetMultipleAssociationPropertyNameOptions,
				_t4ModeOptions.DataModel.TargetMultipleAssociationPropertyNameOptions);

			/// <summary>
			/// Stored procedure or function mapping method naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncMethodNaming = DefineNamingOption(
				"proc-or-func-method-name",
				"procedure or function method naming options",
				_defaultOptions.DataModel.ProcedureNameOptions,
				_t4ModeOptions.DataModel.ProcedureNameOptions);

			/// <summary>
			/// Stored procedure or function mapping method parameters naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncParameterNaming = DefineNamingOption(
				"proc-or-func-param-name",
				"procedure or function method parameters naming options",
				_defaultOptions.DataModel.ProcedureParameterNameOptions,
				_t4ModeOptions.DataModel.ProcedureParameterNameOptions);

			/// <summary>
			/// Stored procedure or table function result-set record class naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncResultClassNaming = DefineNamingOption(
				"proc-or-func-result-class-name",
				"procedure or table function custom result record mapping class naming options",
				_defaultOptions.DataModel.ProcedureResultClassNameOptions,
				_t4ModeOptions.DataModel.ProcedureResultClassNameOptions);

			/// <summary>
			/// Stored procedure async results wrapper class naming option.
			/// </summary>
			public static readonly CliOption AsyncProcResultClassNaming = DefineNamingOption(
				"async-proc-multi-result-class-name",
				@"results wrapper/holder class naming options for async signature of stored procedure with multiple results. E.g.
- procedure with one or more return, out or in-out parameters and rowcount value;
- procedure with one or more return, out or in-out parameters and result table.",
				_defaultOptions.DataModel.AsyncProcedureResultClassNameOptions,
				_t4ModeOptions.DataModel.AsyncProcedureResultClassNameOptions);

			/// <summary>
			/// Stored procedure async results wrapper class properties naming option.
			/// </summary>
			public static readonly CliOption AsyncProcResultClassPropertyNaming = DefineNamingOption(
				"async-proc-multi-result-property-name",
				@"results wrapper/holder class properties naming options for async signature of stored procedure with multiple results. E.g.
- procedure with one or more return, out or in-out parameters and rowcount value;
- procedure with one or more return, out or in-out parameters and result table.",
				_defaultOptions.DataModel.AsyncProcedureResultClassPropertiesNameOptions,
				_t4ModeOptions.DataModel.AsyncProcedureResultClassPropertiesNameOptions);

			/// <summary>
			/// Stored procedure or table function result-set record column property naming option.
			/// </summary>
			public static readonly CliOption ProcOrFuncResultColumnPropertyNaming = DefineNamingOption(
				"proc-or-func-result-property-name",
				"procedure or table function custom result record column property naming options. You probably don't want to specify this option, as it used for field with private visibility.",
				_defaultOptions.DataModel.ProcedureResultColumnPropertyNameOptions,
				_t4ModeOptions.DataModel.ProcedureResultColumnPropertyNameOptions);

			/// <summary>
			/// Scalar function with tuple return type tuple mapping class naming option.
			/// </summary>
			public static readonly CliOption FunctionTupleClassNaming = DefineNamingOption(
				"function-tuple-class-name",
				"tuple class naming options for scalar function with tuple return type",
				_defaultOptions.DataModel.FunctionTupleResultClassNameOptions,
				_t4ModeOptions.DataModel.FunctionTupleResultClassNameOptions);

			/// <summary>
			/// Scalar function with tuple return type tuple field property naming option.
			/// </summary>
			public static readonly CliOption FunctionTupleFieldPropertyNaming = DefineNamingOption(
				"function-tuple-class-field-name",
				"tuple class field naming options for scalar function with tuple return type",
				_defaultOptions.DataModel.FunctionTupleResultPropertyNameOptions,
				_t4ModeOptions.DataModel.FunctionTupleResultPropertyNameOptions);

			/// <summary>
			/// Non-default schema wrapper class naming option.
			/// </summary>
			public static readonly CliOption SchemaWrapperClassNaming = DefineNamingOption(
				"schema-class-name",
				"non-default schema wrapper class naming options",
				_defaultOptions.DataModel.SchemaClassNameOptions,
				_t4ModeOptions.DataModel.SchemaClassNameOptions);

			/// <summary>
			/// Non-default schema data context property naming option.
			/// </summary>
			public static readonly CliOption SchemaContextPropertyNaming = DefineNamingOption(
				"schema-context-property-name",
				"non-default schema context property in main context naming options",
				_defaultOptions.DataModel.SchemaPropertyNameOptions,
				_t4ModeOptions.DataModel.SchemaPropertyNameOptions);

			/// <summary>
			/// Find extension method parameters naming option.
			/// </summary>
			public static readonly CliOption FindParameterNaming = DefineNamingOption(
				"find-parameter-name",
				"Find extension method parameters naming options",
				_defaultOptions.DataModel.FindParameterNameOptions,
				_t4ModeOptions.DataModel.FindParameterNameOptions);

			private static NamingCliOption DefineNamingOption(string option, string help, NormalizationOptions? defaults, NormalizationOptions? t4defaults)
			{
				return new NamingCliOption(
					option,
					help,
					NAMING_HELP,
					new[] { string.Format(CultureInfo.InvariantCulture, NAMING_EXAMPLE_TEMPLATE, option) },
					defaults,
					t4defaults);
			}
		}

		/// <summary>
		/// Provides access to database schema scaffold options definitions.
		/// </summary>
		internal static class SchemaOptions
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
					/*lang=json,strict*/
					new[] { "{ \"schema\": { \"objects\": [\"table\", \"view\", \"table-function\"] } }" },
					false,
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.Table            ) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.Table            ) != 0, "table"             , "load tables"                    ),
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.View             ) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.View             ) != 0, "view"              , "load views"                     ),
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.ForeignKey       ) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.ForeignKey       ) != 0, "foreign-key"       , "load foreign key constrains"    ),
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.StoredProcedure  ) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.StoredProcedure  ) != 0, "stored-procedure"  , "load stored procedures"         ),
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.ScalarFunction   ) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.ScalarFunction   ) != 0, "scalar-function"   , "load scalar functions"          ),
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.TableFunction    ) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.TableFunction    ) != 0, "table-function"    , "load table functions"           ),
					new ((_defaultOptions.Schema.LoadedObjects & Schema.SchemaObjects.AggregateFunction) != 0, (_t4ModeOptions.Schema.LoadedObjects & Schema.SchemaObjects.AggregateFunction) != 0, "aggregate-function", "load aggregate/window functions"));

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
					_defaultOptions.Schema.PreferProviderSpecificTypes,
					_t4ModeOptions.Schema.PreferProviderSpecificTypes);

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
					_defaultOptions.Schema.IgnoreDuplicateForeignKeys,
					_t4ModeOptions.Schema.IgnoreDuplicateForeignKeys);

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
					null,
					null);

			/// <summary>
			/// Explicit list of schemas to load option.
			/// </summary>
			public static readonly CliOption DefaultSchemas = new StringCliOption(
					"default-schemas",
					null,
					false,
					true,
					"specify which schemas should be recognized as default schemas",
					@"Objects from schemas, marked as default, will be:
  - put to main data context instead of separate schema-specific class (see also schema-as-type option)
  - will skip generation of schema name in metadata (see also include-default-schema-name option)

When this option is not set, CLI tool use database-specific logic to detect default schema. Usually it is current user/schema name associated with connection string, used for database scaffolding and supports only one schema. Using this option you can specify multiple schemas.",
					null,
					null,
					_defaultOptions.Schema.DefaultSchemas?.ToArray(),
					_t4ModeOptions.Schema.DefaultSchemas?.ToArray());

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
					null,
					null);

			/// <summary>
			/// Load database name component for schema objects.
			/// </summary>
			public static readonly CliOption LoadDatabaseName = new BooleanCliOption(
					"database-in-name",
					null,
					false,
					"include database name in name of db object in database schema",
					null,
					null,
					null,
					_defaultOptions.Schema.LoadDatabaseName,
					_t4ModeOptions.Schema.LoadDatabaseName);

			/// <summary>
			/// Don't load history tables for SQL Server temporal tables.
			/// </summary>
			public static readonly CliOption IgnoreSystemHistoryTables = new BooleanCliOption(
					"mssql-ignore-temporal-history-tables",
					null,
					false,
					"ignore history tables for SQL Server temporal tables (SQL Server 2016+ only)",
					null,
					null,
					null,
					_defaultOptions.Schema.IgnoreSystemHistoryTables,
					_t4ModeOptions.Schema.IgnoreSystemHistoryTables);

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
					_defaultOptions.Schema.UseSafeSchemaLoad,
					_t4ModeOptions.Schema.UseSafeSchemaLoad);

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
					_defaultOptions.Schema.LoadProceduresSchema,
					_t4ModeOptions.Schema.LoadProceduresSchema);

			/// <summary>
			/// Load result-set schema for stored procedures option.
			/// </summary>
			public static readonly CliOption EnableSqlServerReturnValue = new BooleanCliOption(
					"mssql-enable-return-value-parameter",
					null,
					false,
					"(only for SQL Server) enable generation of RETURN_VALUE parameter for stored procedures",
					null,
					null,
					null,
					_defaultOptions.Schema.EnableSqlServerReturnValue,
					_t4ModeOptions.Schema.EnableSqlServerReturnValue);

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
						/*lang=json*/
						"{ \"schema\": { \"include-tables\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles tables",
						/*lang=json*/
						"{ \"schema\": { \"include-tables\": [ { \"regex\": \"^audit_.+$\", \"schema\": \"dbo\" } ] } } // all tables starting from audit_ prefix"
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
						/*lang=json*/
						"{ \"schema\": { \"exclude-tables\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles tables ignored",
						/*lang=json*/
						"{ \"schema\": { \"exclude-tables\": [ { \"regex\": \"^audit_.+$\", \"schema\": \"dbo\" } ] } } // all tables starting from audit_ prefix ignored"
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
						/*lang=json*/
						"{ \"schema\": { \"include-views\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles views",
						/*lang=json*/
						"{ \"schema\": { \"include-views\": [ { \"regex\": \"^audit_.+$\", \"schema\": \"dbo\" } ] } } // all views starting from audit_ prefix"
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
						/*lang=json*/
						"{ \"schema\": { \"exclude-views\": [ \"Users\", { \"name\": \"Roles\", \"schema\": \"dbo\" } ] } } // Users and dbo.Roles views ignored",
						/*lang=json*/
						"{ \"schema\": { \"exclude-views\": [ { \"regex\": \"^audit_.+$\", \"schema\": \"dbo\" } ] } } // all views starting from audit_ prefix ignored"
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
						/*lang=json*/
						"{ \"schema\": { \"procedures-with-schema\": [ \"GetUsers\", { \"name\": \"LoadPermissions\", \"schema\": \"dbo\" } ] } } // GetUsers and dbo.LoadPermissions procedures",
						/*lang=json*/
						"{ \"schema\": { \"procedures-with-schema\": [ { \"regex\": \"^Load.+$\", \"schema\": \"dbo\" } ] } } // all procedures starting from Load prefix"
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
						/*lang=json*/
						"{ \"schema\": { \"procedures-without-schema\": [ \"DropAllTables\", { \"name\": \"FormatAllDrives\", \"schema\": \"dbo\" } ] } } // DropAllTables and dbo.FormatAllDrives procedures schema not loaded",
						/*lang=json*/
						"{ \"schema\": { \"procedures-without-schema\": [ { \"regex\": \"^Delete.+$\", \"schema\": \"dbo\" } ] } } // all procedures starting from Delete prefix"
					});

			/// <summary>
			/// Stored procedure load filter option.
			/// </summary>
			public static readonly CliOption IncludedStoredProcedures = new ObjectNameFilterCliOption(
					"include-stored-procedures",
					null,
					false,
					"only load stored procedures with specified name(s)",
					@"Provided stored procedures names should have same casing as actual procedure name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for stored procedure;
  - stored procedure name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - stored procedure schema (schema property);
  - regular expression (regex property) instead of exact stored procedure name (name property).
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
string // also you can put stored procedure name as string directly to list
",
					new[] { "--include-stored-procedures GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						/*lang=json*/
						"{ \"schema\": { \"include-stored-procedures\": [ \"ActiveUsers\", { \"name\": \"InactiveUsers\", \"schema\": \"dbo\" } ] } } // ActiveUsers and dbo.InactiveUsers procedures",
						/*lang=json*/
						"{ \"schema\": { \"include-stored-procedures\": [ { \"regex\": \"^Query.+$\", \"schema\": \"dbo\" } ] } } // all stored procedures starting from Query prefix"
					});

			/// <summary>
			/// Stored procedure skip filter option.
			/// </summary>
			public static readonly CliOption ExcludedStoredProcedures = new ObjectNameFilterCliOption(
					"exclude-stored-procedures",
					null,
					false,
					"skip load of stored procedures with specified name(s)",
					@"Provided stored procedures names should have same casing as actual procedure name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for stored procedure;
  - stored procedure name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - stored procedure schema (schema property);
  - regular expression (regex property) instead of exact stored procedure name (name property).
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
string // also you can put stored procedure name as string directly to list
",
					new[] { "--exclude-stored-procedure GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						/*lang=json*/
						"{ \"schema\": { \"exclude-stored-procedure\": [ \"TestProcedure\", { \"name\": \"CheckDb\", \"schema\": \"dbo\" } ] } } // TestProcedure and dbo.CheckDb procedures ignored",
						/*lang=json*/
						"{ \"schema\": { \"exclude-stored-procedure\": [ { \"regex\": \"^Audit.+$\", \"schema\": \"dbo\" } ] } } // all stored procedures starting from Audit prefix ignored"
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
						/*lang=json*/
						"{ \"schema\": { \"include-table-functions\": [ \"ActiveUsers\", { \"name\": \"InactiveUsers\", \"schema\": \"dbo\" } ] } } // ActiveUsers and dbo.InactiveUsers functions",
						/*lang=json*/
						"{ \"schema\": { \"include-table-functions\": [ { \"regex\": \"^Query.+$\", \"schema\": \"dbo\" } ] } } // all table functions starting from Query prefix"
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
						/*lang=json*/
						"{ \"schema\": { \"exclude-table-functions\": [ \"TestFunction\", { \"name\": \"CheckDb\", \"schema\": \"dbo\" } ] } } // TestFunction and dbo.CheckDb functions ignored",
						/*lang=json*/
						"{ \"schema\": { \"exclude-table-functions\": [ { \"regex\": \"^Audit.+$\", \"schema\": \"dbo\" } ] } } // all table functions starting from Audit prefix ignored"
					});

			/// <summary>
			/// Scalar function load filter option.
			/// </summary>
			public static readonly CliOption IncludedScalarFunctions = new ObjectNameFilterCliOption(
					"include-scalar-functions",
					null,
					false,
					"only load scalar functions with specified name(s)",
					@"Provided scalar functions names should have same casing as actual function name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for scalar function;
  - scalar function name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - scalar function schema (schema property);
  - regular expression (regex property) instead of exact scalar function name (name property).
JSON list element schema:
{
    ""name""  : string  // scalar function name
    ""schema"": string? // scalar function schema (optional)
}
|
{
    ""regex"" : string  // scalar function name matching regular expression
    ""schema"": string? // scalar function schema (optional)
}
|
string // also you can put scalar function name as string directly to list
",
					new[] { "--include-scalar-functions GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						/*lang=json*/
						"{ \"schema\": { \"include-scalar-functions\": [ \"ActiveUsers\", { \"name\": \"InactiveUsers\", \"schema\": \"dbo\" } ] } } // ActiveUsers and dbo.InactiveUsers functions",
						/*lang=json*/
						"{ \"schema\": { \"include-scalar-functions\": [ { \"regex\": \"^Query.+$\", \"schema\": \"dbo\" } ] } } // all scalar functions starting from Query prefix"
					});

			/// <summary>
			/// Scalar function skip filter option.
			/// </summary>
			public static readonly CliOption ExcludedScalarFunctions = new ObjectNameFilterCliOption(
					"exclude-scalar-functions",
					null,
					false,
					"skip load of scalar functions with specified name(s)",
					@"Provided scalar functions names should have same casing as actual function name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for scalar function;
  - scalar function name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - scalar function schema (schema property);
  - regular expression (regex property) instead of exact scalar function name (name property).
JSON list element schema:
{
    ""name""  : string  // scalar function name
    ""schema"": string? // scalar function schema (optional)
}
|
{
    ""regex"" : string  // scalar function name matching regular expression
    ""schema"": string? // scalar function schema  (optional)
}
|
string // also you can put scalar function name as string directly to list
",
					new[] { "--exclude-scalar-functions GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						/*lang=json*/
						"{ \"schema\": { \"exclude-scalar-functions\": [ \"TestFunction\", { \"name\": \"CheckDb\", \"schema\": \"dbo\" } ] } } // TestFunction and dbo.CheckDb functions ignored",
						/*lang=json*/
						"{ \"schema\": { \"exclude-scalar-functions\": [ { \"regex\": \"^Audit.+$\", \"schema\": \"dbo\" } ] } } // all scalar functions starting from Audit prefix ignored"
					});

			/// <summary>
			/// Aggregate function load filter option.
			/// </summary>
			public static readonly CliOption IncludedAggregateFunctions = new ObjectNameFilterCliOption(
					"include-aggregate-functions",
					null,
					false,
					"only load aggregate functions with specified name(s)",
					@"Provided aggregate functions names should have same casing as actual function name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for aggregate function;
  - aggregate function name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - aggregate function schema (schema property);
  - regular expression (regex property) instead of exact aggregate function name (name property).
JSON list element schema:
{
    ""name""  : string  // aggregate function name
    ""schema"": string? // aggregate function schema (optional)
}
|
{
    ""regex"" : string  // aggregate function name matching regular expression
    ""schema"": string? // aggregate function schema (optional)
}
|
string // also you can put aggregateaggregate function name as string directly to list
",
					new[] { "--include-aggregate-functions GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						/*lang=json*/
						"{ \"schema\": { \"include-aggregate-functions\": [ \"ActiveUsers\", { \"name\": \"InactiveUsers\", \"schema\": \"dbo\" } ] } } // ActiveUsers and dbo.InactiveUsers functions",
						/*lang=json*/
						"{ \"schema\": { \"include-aggregate-functions\": [ { \"regex\": \"^Query.+$\", \"schema\": \"dbo\" } ] } } // all aggregate functions starting from Query prefix"
					});

			/// <summary>
			/// Aggregate function skip filter option.
			/// </summary>
			public static readonly CliOption ExcludedAggregateFunctions = new ObjectNameFilterCliOption(
					"exclude-aggregate-functions",
					null,
					false,
					"skip load of aggregate functions with specified name(s)",
					@"Provided aggregate functions names should have same casing as actual function name in database. Specifying this option in command line has several limitations and it is recommended to use JSON for it instead:
  - there is no way to specify schema name for aggregate function;
  - aggregate function name cannot have comma (,) as it is used as list separator;
  - only exact match possible;
JSON allows you to specify more options:
  - aggregate function schema (schema property);
  - regular expression (regex property) instead of exact aggregate function name (name property).
JSON list element schema:
{
    ""name""  : string  // aggregate function name
    ""schema"": string? // aggregate function schema (optional)
}
|
{
    ""regex"" : string  // aggregate function name matching regular expression
    ""schema"": string? // aggregate function schema  (optional)
}
|
string // also you can put aggregate function name as string directly to list
",
					new[] { "--exclude-aggregate-functions GetUsers,GetRoles,LoadPermissions" },
					new[]
					{
						/*lang=json*/
						"{ \"schema\": { \"exclude-aggregate-functions\": [ \"TestFunction\", { \"name\": \"CheckDb\", \"schema\": \"dbo\" } ] } } // TestFunction and dbo.CheckDb functions ignored",
						/*lang=json*/
						"{ \"schema\": { \"exclude-aggregate-functions\": [ { \"regex\": \"^Audit.+$\", \"schema\": \"dbo\" } ] } } // all aggregate functions starting from Audit prefix ignored"
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
			SapHana,
			// all three ClickHouse clients used as we don't know which protocol available for user
			ClickHouseMySql,
			ClickHouseHttp,
			ClickHouseTcp,
			//Added for External Provider
			Custom
		}

		public static CliCommand Instance { get; } = new ScaffoldCommand();
	}
}
