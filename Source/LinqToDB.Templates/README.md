<!--TOC-->
- [Create your Data Model](#create-your-data-model)
  - [T4 template example](#t4-template-example)
- [Configuring template](#configuring-template)
  - [Configuring schema load process](#configuring-schema-load-process)
  - [Configuring generation process](#configuring-generation-process)
    - [Global/generic options](#globalgeneric-options)
    - [DataContext configuration](#datacontext-configuration)
    - [Schemas configuration](#schemas-configuration)
    - [Table mappings configuration](#table-mappings-configuration)
    - [Columns configuration](#columns-configuration)
    - [Associations configuration](#associations-configuration)
    - [Procedures and functions configuration](#procedures-and-functions-configuration)
    - [Other generated functionality](#other-generated-functionality)
    - [Pluralization services](#pluralization-services)
    - [Naming configuration](#naming-configuration)
  - [Example of generation process customization](#example-of-generation-process-customization)
  - [Useful members and data structures](#useful-members-and-data-structures)
<!--/TOC-->

# Create your Data Model

Follow the next steps to create a data model from your existing database:

1. Create new *.tt file (e.g. MyDatabase.tt) in the folder where you would like to generate your data model. For example:

- MyProject
  - DataModels
    - MyDatabase.tt

2. Copy content from the *CopyMe.<DB_NAME>.tt.txt* file located in the *LinqToDB.Templates* folder.

3. Find the following methods in your template and provide connection parameters:

```c#
    Load<DB_NAME>Metadata("MyServer", "MyDatabase", "root", "TestPassword");
//  Load<DB_NAME>Metadata(connectionString);
```

4. See more at [LinqToDB T4 Models](https://linq2db.github.io/articles/T4.htm) and this [readme](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB.Templates/README.md) on GitHub.

## T4 template example

Your data model template should look like the following:

```cs
<#@ template language="C#" debug="True" hostSpecific="True"                    #>
<#@ CleanupBehavior processor="T4VSHost" CleanupAfterProcessingtemplate="true" #>
<#@ output extension=".generated.cs"                                           #>

<#@ include file="$(LinqToDBT4SqlServerTemplatesPath)LinqToDB.SqlServer.ttinclude"   once="true" #>
<#@ include file="$(LinqToDBT4SqlServerTemplatesPath)PluralizationService.ttinclude" once="true" #>
<#
    // Configuring schema load process
    //
    GetSchemaOptions.GetProcedures = true;

    // Configuring generation process
    //
    NamespaceName        = "DataModels";
    DataContextName      = "TestDataDB";
    GenerateSchemaAsType = true;

    // Loading metadata
    //
    LoadSqlServerMetadata("MyServer", "MyDatabase", "User", "Password");

    // Customizing generation process
    //
    GetColumn("Order", "OrderID").MemberName = "ID";
    GetColumn("Order", "Day").    Type       = "DayOfWeek";

    GenerateModel();
#>
```

# Configuring template

## Configuring schema load process

Use the following initialization **before** you call the `LoadMetadata()` method.

All schema load functionality configured using `GetSchemaOptions` property of [`LinqToDB.SchemaProvider.GetSchemaOptions`](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/GetSchemaOptions.cs) type. Check this class for all available options.

All loaded schema information is used for mappings generation, so if you want to limit generated mappings, it is the best place to do it.

```cs
// Enables loading of tables and views information
GetSchemaOptions.GetTables             = true;

// Enables loading of foreign key relations for associations
GetSchemaOptions.GetForeignKeys        = true;

// Enables loading of functions and procedures information
GetSchemaOptions.GetProcedures         = true;

// Enables use of System.Char type in generated model for text types
// with length 1 instead of System.String
GetSchemaOptions.GenerateChar1AsString = false;

// Enables generation of provider-specific type for column or parameter mapping
// when both common .net type and provider-specific type supported.
GetSchemaOptions.PreferProviderSpecificTypes = false;

// (string[]) List of schemas to select.
// Option applied only if is not empty
GetSchemaOptions.IncludedSchemas = null;

// (string[]) List of schemas to exclude from select.
// Option applied only if is not empty
GetSchemaOptions.ExcludedSchemas = null;

// (string) explicit name of default schema.
// If not specified, use default schema for current connection.
GetSchemaOptions.DefaultSchema = null;

// Option makes sense only for providers that return schema for several databases
// (string[]) List of databases/catalogs to select.
// Option applied only if is not empty
GetSchemaOptions.IncludedCatalogs = null;

// Option makes sense only for providers that return schema for several databases
// (string[]) List of databases/catalogs to exclude from select.
// Option applied only if is not empty
GetSchemaOptions.ExcludedCatalogs = null;

// Custom filter for table/view schema load
// Can be used to exclude views or tables from generation based in their descriptor.
// This filter especially useful, when you want to exclude table, referenced by other generated
// tables using associations, or by procedures using excluded table as result. Doing it in filter
// will automatically prevent associations generation and will trigger generation of procedure-specific
// result classes.
// LoadTableData type:
// https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/LoadTableData.cs
Func<LoadTableData,bool> GetSchemaOptions.LoadTable = null;

// Comparer, used for IncludedSchemas/ExcludedSchemas/IncludedCatalogs/ExcludedCatalogs lookups
StringComparer                    = StringComparer.OrdinalIgnoreCase;

// Custom filter for procedure/function result schema loader.
// Can be used to exclude schema load for functions, that generate error during schema load
// Also check GenerateProcedureErrors option below
// ProcedureSchema type:
// https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/ProcedureSchema.cs
GetSchemaOptions.LoadProcedure    = (ProcedureSchema p) => true;

// SQL Server 2012+ only
// true: use sp_describe_first_result_set procedure to load procedure schema
// false: use CommandBehavior.SchemaOnly to load procedure schema
GetSchemaOptions.UseSchemaOnly    = Common.Configuration.SqlServer.UseSchemaOnlyToGetSchema = false;

// type: Func<ForeignKeySchema,string>
// Defines custom association naming logic
// https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/ForeignKeySchema.cs
GetSchemaOptions.GetAssociationMemberName = null;

// Procedures load progress reporting callback
// Not applicable for T4 templates
GetSchemaOptions.ProcedureLoadingProgress = (int total, int current) => {};
```

## Configuring generation process

Use the following initialization **before** you call the `LoadMetadata()` method.

### Global/generic options

```cs
// Namespace to use for generated model
NamespaceName                = "DataModels";

// Enables generation of nullable reference type annotations
EnableNullableReferenceTypes = true;

// Disable CS8618 for uninitialized model columns and references of non-nullable reference type
EnforceModelNullability      = true;

// Defines method to distinguish value types from reference types by type name
// used by nullable reference types feature to detect reference types, when only type name available
// If EnableNullableReferenceTypes enabled, but value type not recognized properly
// you must provide your own resolver for unresolved types
// IsValueType = typeName => {
//    switch (typeName)
//    {
//        case "unresolved type name": return true; // or false for reference type
//        default: return IsValueTypeDefault(typeName);
//    }
// };
// by default resolve unknown types, ending with ? as value types and other types as reference types
Func<string,boolean> IsValueType = IsValueTypeDefault;
```

### DataContext configuration

```c#

// (string) Name of data context class.
// Default: <DATABASE_NAME> + "DB"
DataContextName                 = null;

// (string) Name of base class for generated data context class.
// Default: LinqToDB.Data.DataConnection or LinqToDB.IDataContext.
BaseDataContextClass            = null;

// Enables generation of constructors for data context class.
// Disabling could be useful if you need to have custom implementation
// of constructors in partial class
GenerateConstructors            = true;

// (string) Defines name of default configuration to use with default data context constructor
DefaultConfiguration            = null;

// Enables generation of data model only without data context class.
GenerateModelOnly               = false;

// Enables generation of data model as an interface.
GenerateModelInterface          = false;

// Enables generation of data context comment with database name, data source and database version
GenerateDatabaseInfo            = true;

// Generates constructors that call user-defined methods returning DataOptions
GetDataOptionsMethod            = "GetDataOptions({0})";

// Generates data options constructors
GenerateDataOptionsConstructors = true;
```

### Schemas configuration

```c#
// Enables generation of mappings for each schema in separate type (db.MySchema.MyTable)
GenerateSchemaAsType      = false;

// Contains mapping of schema name to corresponding schema class name
// By default is empty and class name generated from schema name
// Requires GenerateSchemaAsType=true set
SchemaNameMapping         = Dictionary<string,string>();

// Suffix, added to schema class name
// Requires GenerateSchemaAsType=true set
SchemaNameSuffix          = "Schema";

// Name of data context class for schema.
// Requires GenerateSchemaAsType=true set
SchemaDataContextTypeName = "DataContext"
```

### Table mappings configuration

```c#
// (string) Specify base class (or comma-separated list of class and/or interfaces) for table mappings
BaseEntityClass                    = null;

// Enables generation of TableAttribute.Database property using database name, returned by schema loader
GenerateDatabaseName               = false;

// Enables generation of TableAttribute.Database property with provided name value.
// (string) If set, overrides GenerateDatabaseName behavior
DatabaseName                       = null;

// Enables generation of TableAttribute.Server property with provided name value.
ServerName                         = null;

// Enables generation of TableAttribute.Schema property for default schema
IncludeDefaultSchema               = true;

// Enables generation of mappings for views
GenerateViews                      = true;

// Enables prefixing mapping classes for tables in non-default schema with schema name
// E.g. MySchema.MyTable -> MySchema_MyTable
// Applicable only if GenerateSchemaAsType = false
PrefixTableMappingWithSchema       = true;

// Enables prefixing mapping classes for tables in default schema with schema name
// E.g. dbo.MyTable -> dbo_MyTable
// Applicable only if IncludeDefaultSchema = true && GenerateSchemaAsType = false && PrefixTableMappingWithSchema = true
PrefixTableMappingForDefaultSchema = false;

// Generates database name from TableSchema.CatalogName
GenerateDatabaseNameFromTable      = false;

```

### Columns configuration

```c#
// Enables compact generation of column properties
IsCompactColumns                    = true;

// Enables compact generation of aliased column properties
IsCompactColumnAliases              = true;

// Enables generation of DataType, Length, Precision and Scale properties 
// of ColumnAttribute. Could be overridden (except DataType) by options below
GenerateDataTypes                   = false;

// (boolean) Enables or disables generation of ColumnAttribute.Length property.
// If null, GenerateDataTypes value is used
GenerateLengthProperty              = null;

// (boolean) Enables or disables generation of ColumnAttribute.Precision property.
// If null, GenerateDataTypes value is used
GeneratePrecisionProperty           = null;

// (boolean) Enables or disables generation of ColumnAttribute.Scale property.
// If null, GenerateDataTypes value is used
GenerateScaleProperty               = null;

// Enables generation of ColumnAttribute.DbType property.
GenerateDbTypes                     = false;

// Enables generation of ObsoleteAttribute for column aliases
GenerateObsoleteAttributeForAliases = false;
```

### Associations configuration

```c#
// Defines type template for one-to-many association, when it is generated as a member of table mapping.
// Some other options: "{0}[]", "List<{0}>".
OneToManyAssociationType      = "IEnumerable<{0}>";

// Enables generation of associations in table mappings
GenerateAssociations          = true;

// Enables generation of back side of association. Applies to both table mapping members and extension
// associations
GenerateBackReferences        = true;

// Enables generation of associations as extension methods for related table mapping classes
GenerateAssociationExtensions = false;

// Defines method to generate name for "one" side of association
Func<ForeignKey, string> GetAssociationExtensionSingularName
    = GetAssociationExtensionSingularNameDefault;

// Defines method to generate name for "many" side of association
Func<ForeignKey, string> GetAssociationExtensionPluralName
    = GetAssociationExtensionPluralNameDefault;

// Enables generation of association columns using 'nameof' expression
GenerateNameOf = true;
```

### Procedures and functions configuration

```c#
// Enables use of existing table mappings for procedures and functions that return same results as
// defined by mapping
ReplaceSimilarTables             = true;

// If enabled, procedure schema load error will be generated as #error directive and fail build
// of output file. Useful for initial generation to highlight places, that require review or
// additional hints for schema loader
// Also check GetSchemaOptions.LoadProcedure option above
GenerateProcedureErrors          = true;

// If enabled, methods for procedures that return table will be generated with List<T> return type and
// IMPORTANT: this will lead to load of all procedure results into list and could lead
// to performance issues on big results
GenerateProcedureResultAsList    = false;

// Enables stored procedure methods to accept generated context object or DataConnection type
GenerateProceduresOnTypedContext = true;
```

### Other generated functionality

```c#
// Enables generation of Find(pk fields) extension methods for record selection by primary key value
GenerateFindExtensions = true;
```

### Pluralization services

```c#
// Enables pluralization of table mapping classes
PluralizeClassNames                 = false;

// Enables singularization of table mapping classes
SingularizeClassNames               = true;

// Enables pluralization of ITable<> properties in data context
PluralizeDataContextPropertyNames   = true;

// Enables singularization of ITable<> properties in data context
SingularizeDataContextPropertyNames = false;

// Enables pluralization of foreign key names
PluralizeForeignKeyNames            = true;

// Enables singularization of foreign key names
SingularizeForeignKeyNames          = true;
```

### Naming configuration

```c#
// Enables normalization of type and member names.
// Default normalization removes underscores and capitalize first letter.
// Could be overridden using ToValidName option below.
// By default doesn't normalize names without underscores.
// see NormalizeNamesWithoutUnderscores setting
NormalizeNames                                 = true;

// enables normalization of names without underscores.
NormalizeNamesWithoutUnderscores               = false;

// Defines logic to convert type/member name, derived from database object name, to C# identifier.
Func<string, bool, string> ToValidName         = ToValidNameDefault;

// Makes C# identifier valid by removing unsupported symbols and calling ToValidName
Func<string, bool, string> ConvertToCompilable = ConvertToCompilableDefault;
```

## Example of generation process customization

Use the following code to modify your model **before** you call the `GenerateModel()` method.

```c#
// Replaces table mapping class name
GetTable("Person").TypeName  = "MyName";
// Sets base class & interface for mapping class
GetTable("Person").BaseClass = "PersonBase, IId";

// Replaces property name for column PersonID of Person table with ID.
GetColumn("Person", "PersonID")    .MemberName   = "ID";
// Sets [Column(SkipOnUpdate=true)].
// Same logic can be used for other column options
GetColumn("Person", "PasswordHash").SkipOnUpdate = true;
// Change column property type
GetColumn("Person", "Gender")      .Type               = "global::Model.Gender";
// or
// TypeBuilder usually used when type name depends on name from model and could change before
// code generation
GetColumn("Person", "Gender")      .TypeBuilder        = () => "global::Model.Gender";

// Replaces association property name
GetFK("Orders", "FK_Orders_Customers").MemberName      = "Customers";
// Changes association type
GetFK("Orders", "FK_Orders_Customers").AssociationType = AssociationType.OneToMany;

SetTable(string tableName,
    string TypeName = null,
    string DataContextPropertyName = null)

    .Column(string columnName, string MemberName = null, string Type = null, bool? IsNullable = null)
    .FK    (string fkName,     string MemberName = null, AssociationType? AssociationType = null)
    ;

// Adds extra namespace to usings
Model.Usings.Add("MyNamespace");

// Replaces all property names for columns where name is '<TableName>' + 'ID' with 'ID' 
// and adds a base interface to the generated class.
foreach (var t in Tables.Values)
    foreach (var c in t.Columns.Values)
        if (c.IsPrimaryKey && c.MemberName == t.TypeName + "ID")
        {
            c.MemberName = "ID";
            t.Interfaces.Add("IIdentifiable");
        }
```

## Useful members and data structures

```c#
Dictionary<string,Table>     Tables     = new Dictionary<string,Table>    ();
Dictionary<string,Procedure> Procedures = new Dictionary<string,Procedure>();

Table      GetTable     (string name);
Procedure  GetProcedure (string name);
Column     GetColumn    (string tableName, string columnName);
ForeignKey GetFK        (string tableName, string fkName);
ForeignKey GetForeignKey(string tableName, string fkName);

public class Table
{
    public string Schema;
    public string TableName;
    public string DataContextPropertyName;
    public bool   IsView;
    public string Description;
    public string AliasPropertyName;
    public string AliasTypeName;
    public string TypeName;

    public Dictionary<string,Column>     Columns;
    public Dictionary<string,ForeignKey> ForeignKeys;
}

public partial class Column : Property
{
    public string    ColumnName; // Column name in database
    public bool      IsNullable;
    public bool      IsIdentity;
    public string    ColumnType; // Type of the column in database
    public DbType    DbType;
    public string    Description;
    public bool      IsPrimaryKey;
    public int       PrimaryKeyOrder;
    public bool      SkipOnUpdate;
    public bool      SkipOnInsert;
    public bool      IsDuplicateOrEmpty;
    public string    AliasName;
    public string    MemberName;
}

public enum AssociationType
{
    Auto,
    OneToOne,
    OneToMany,
    ManyToOne,
}

public partial class ForeignKey : Property
{
    public string           KeyName;
    public Table            OtherTable;
    public List<Column>     ThisColumns;
    public List<Column>     OtherColumns;
    public bool             CanBeNull;
    public ForeignKey       BackReference;
    public string           MemberName;
    public AssociationType  AssociationType;
}

public partial class Procedure : Method
{
    public string          Schema;
    public string          ProcedureName;
    public bool            IsFunction;
    public bool            IsTableFunction;
    public bool            IsDefaultSchema;

    public Table           ResultTable;
    public Exception       ResultException;
    public List<Table>     SimilarTables;
    public List<Parameter> ProcParameters;
}

public class Parameter
{
    public string   SchemaName;
    public string   SchemaType;
    public bool     IsIn;
    public bool     IsOut;
    public bool     IsResult;
    public int?     Size;
    public string   ParameterName;
    public string   ParameterType;
    public Type     SystemType;
    public string   DataType;
}
```
