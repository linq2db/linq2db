# T4 Models

T4 models are used to generate POCO's C# code using your database structure.

## Installation

Firstly you should install one of tools packages into your project:

`Install-Package linq2db.XXX`

Where XXX is one of supported databases, for example:

`Install-Package linq2db.SqlServer`

This also will install needed linq2db packages:

* linq2db.t4models
* linq2db 

But **not** data provider packages (install them only if needed to compile your project, T4 models ships it's own data provider assemblies).

### .Net Core specific

Because of .Net Core projects do not support NuGet content files all stuff is not copied into project's folder, so to run T4 templates you'll need:

* open `$(SolutionDir).tools\linq2db.t4models` in Explorer 
* copy `CopyMe.XXX.Core.tt.txt` to your project's folder or subfolder, then you should use it instead of `CopyMe.XXX.tt.txt`

## Running

After package installing you will see new `LinqToDB.Templates` folder in your project, this folder contains all needed T4 stuff to generate your model. Also would be created new folder in tour solution: `$(SolutionDir).tools\linq2db.t4models`, it is used to store and link assemblies, needed for generation (linq2db.dll and data provider assemblies).

To create a data model template take a look at one of the CopyMe.XXX.tt.txt file in your LinqToDB.Templates project folder. Copy this file to needed project location and rename it, like `MyModel.tt`

There are few main steps in this file:

1. Configuring generation process (read below)
1. Loading metadata - this is a call to `LoadMatadata()` function - it connects to your database and fetches all needed metadata (table structure, views, and so on)
1. Customizing generation process (read below)
1. Calling `GenerateModel()` - this will run model generation 

## Configuring schema load process

Use the following initialization **before** you call the `LoadMetadata()` method.

All schema load functionality configured using `GetSchemaOptions` property of [`LinqToDB.SchemaProvider.GetSchemaOptions`](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/GetSchemaOptions.cs) type. Check this class for all available options.

All loaded schema information is used for mappings generation, so if you want to limit generated mappings, it is the best place to do it.

```cs
// Enables loading of tables and views information
GetSchemaOptions.GetTables             = true;
// Enables loading of functions and procedures information
GetSchemaOptions.GetProcedures         = true;
// Enables use of System.Char type in generated model for text types
// with length 1 instead of System.String
GetSchemaOptions.GenerateChar1AsString = false;

// (string[]) List of schemas to select.
// Option applied only if is is not empty
GetSchemaOptions.IncludedSchemas = null;
// (string[]) List of schemas to exclude from select.
// Option applied only if is is not empty
GetSchemaOptions.ExcludedSchemas = null;

// Option makes sense only for providers that return schema for several databases
// (string[]) List of databases/catalogs to select.
// Option applied only if is is not empty
GetSchemaOptions.IncludedCatalogs = null;
// Option makes sense only for providers that return schema for several databases
// (string[]) List of databases/catalogs to exclude from select.
// Option applied only if is is not empty
GetSchemaOptions.ExcludedCatalogs = null;

// Comparer, used for IncludedSchemas/ExcludedSchemas/IncludedCatalogs/ExcludedCatalogs lookups
StringComparer                    = StringComparer.OrdinalIgnoreCase;

// Custom filter for procedure/function result schema loader.
// Can be used to exclude schema load for functions, that generate error during schema load
// Also check GenerateProcedureErrors option below
// ProcedureSchema type:
// https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/ProcedureSchema.cs
GetSchemaOptions.LoadProcedure     = (ProcedureSchema p) => true;

// type: Func<ForeignKeySchema, string>
// Defines custom association naming logic
// https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/SchemaProvider/ForeignKeySchema.cs
GetSchemaOptions.GetAssociationMemberName = null;

// Procedures load progress reporting callback
// Not applicable for T4 templates
GetSchemaOptions.ProcedureLoadingProgress = (int total, int current) => {};
```

## Configuring generation process

Use the following initialization **before** you call the `LoadMetadata()` method.

```cs
/* Global/generic options */
// Namespace to use for generated model
NamespaceName                  = "DataModels";

/* Data context configuration */
// (string) Name of base class for generated data context class.
// Default: LinqToDB.Data.DataConnection.
BaseDataContextClass           = null;
// (string) Name of data context class.
// Default: <DATABASE_NAME> + "DB"
DataContextName                = null;
// Enables generation of constructors for data context class.
// Disabling could be usefull if you need to have custom implementation
// of constructors in partial class
GenerateConstructors          = true;               // Enforce generating DataContext constructors.
// (string) Defines name of default configuration to use with default data context constructor
DefaultConfiguration          = null;

/* Schemas configuration */
// Enables generation of mappings for each schema in separate type
GenerateSchemaAsType            = false;
// Contains mapping of schema name to corresponding schema class name
// By default is empty and class name generated from schema name
// Requires GenerateSchemaAsType=true set
SchemaNameMapping               = Dictionary<string,string>();
// Suffix, added to schema class name
// Requires GenerateSchemaAsType=true set
SchemaNameSuffix                = "Schema"
// Name of data context class for schema.
// Requires GenerateSchemaAsType=true set
SchemaDataContextTypeName       = "DataContext"

/* Table mappings configuration */
// (string) Specify base class (or comma-separated list of class and/or interfaces) for table mappings
BaseEntityClass               = null;
// Enables generation of TableAttribute.Database property using database name, returned by schema loader
GenerateDatabaseName          = false;
// Enables generation of TableAttribute.Database property with provided name value.
// (string) If set, overrides GenerateDatabaseName behavior
DatabaseName                  = null;
// Enables generation of TableAttribute.Schema property for default schema
IncludeDefaultSchema          = true;
// Enables generation of mappings for views
GenerateViews                 = true;
// Enables prefixing mapping classes for tables in non-default schema with schema name
// E.g. MySchema.MyTable -> MySchema_MyTable
// Applicable only if GenerateSchemaAsType = false
PrefixTableMappingWithSchema  = true;

/* Columns comfiguration */
// Enables compact generation of column properties
IsCompactColumns              = true;
// Enables compact generation of aliased column properties
IsCompactColumnAliases              = true;
// Enables generation of DataType, Length, Precision and Scale properties of ColumnAttribute.
// Could be overriden (except DataType) by options below
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

/* Associations configuration */
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
Func<ForeignKey, string> GetAssociationExtensionSinglularName
    = GetAssociationExtensionSinglularNameDefault;
// Defines method to generate name for "many" side of association
Func<ForeignKey, string> GetAssociationExtensionPluralName
    = GetAssociationExtensionPluralNameDefault;

/* Procedures and functions configuration */
// Enables use of existing table mappings for procedures and functions that return same results as
// defined by mapping
ReplaceSimilarTables          = true;
// If enabled, procedure schema load error will be generated as #error directive and fail build
// of output file. Useful for initial generation to highlight places, that require review or
// additional hints for schema loader
// Also check GetSchemaOptions.LoadProcedure option above
GenerateProcedureErrors       = true;
// If enabled, methods for procedures that return table will be generated with List<T> return type and
// IMPORTANT: this will lead to load of all procedure results into list and could lead
// to performance issues on big results
GenerateProcedureResultAsList = false;

/* Other generated functionality */
// Enables generation of Find(pk fields) extension methods for record selection by primary key value
GenerateFindExtensions        = true;

/* Pluralization services */
// Enables pluralization of table mapping classes
PluralizeClassNames                 = false;
// Enables singularization of table mapping classes
SingularizeClassNames               = true;
// Enables pluralization of ITable<> properties in data context
PluralizeDataContextPropertyNames   = true;
// Enables singularization of ITable<> properties in data context
SingularizeDataContextPropertyNames = false;

/* Naming configuration */
// Enables normalization of of type and member names.
// Default normalization removes underscores and capitalize first letter.
// Could be overriden using ToValidName option below.
NormalizeNames                                 = false;
// Defines logic to convert type/member name, derived from database object name, to C# identifier.
Func<string, bool, string> ToValidName         = ToValidNameDefault;
// Makes C# identifier valid by removing unsupported symbols and calling ToValidName
Func<string, bool, string> ConvertToCompilable = ConvertToCompilableDefault;
```

## Provider specific options

### SQL Server

```cs
// Enables generation of extensions for Free Text Search
bool GenerateSqlServerFreeText = true;
```

### PostgreSQL

```cs
// Enables generation of case-sensitive names of database objects
bool GenerateCaseSensitiveNames = false;
```

### Sybase

```cs
// Enables generation of Sybase sysobjects tables
bool GenerateSybaseSystemTables = false;
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
GetColumn("Person", "Gender")      .Type         = "global::Model.Gender";

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

// Replaces all property names for columns where name is '<TableName>' + 'ID' with 'ID'.
foreach (var t in Tables.Values)
	foreach (var c in t.Columns.Values)
		if (c.IsPrimaryKey && c.MemberName == t.TypeName + "ID")
			c.MemberName = "ID";
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
