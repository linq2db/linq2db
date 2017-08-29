# T4 Models

T4 models are used to generate POCO's C# code using your database structure.

### Build status
* Master: [![Build status](https://ci.appveyor.com/api/projects/status/ld4cv53wyfi4qtqm/branch/master?svg=true)](https://ci.appveyor.com/project/igor-tkachev/t4models/branch/master)
* Current: [![Build status](https://ci.appveyor.com/api/projects/status/ld4cv53wyfi4qtqm?svg=true)](https://ci.appveyor.com/project/igor-tkachev/t4models)

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

# Running

After package installing you will see new `LinqToDB.Templates` folder in your project, this folder contains all needed T4 stuff to generate your model. Also would be created new folder in tour solution: `$(SolutionDir).tools\linq2db.t4models`, it is used to store and link assemblies, needed for generation (linq2db.dll and data provider assemblies).

To create a data model template take a look at one of the CopyMe.XXX.tt.txt file in your LinqToDB.Templates project folder. Copy this file to needed project location and rename it, like `MyModel.tt`

There are few main steps in this file:
1. Configuring generation process (read below)
1. Loading metadata - this is a call to `LoadMatadata()` function - it connects to your database and fetches all needed metadata (table structure, views, and so on)
1. Customizing generation process (read below)
1. Calling `GenerateModel()` - this will run model generation 

## Configuring generation process

Use the following initialization **before** you call the `LoadMetadata()` method.

```c#
NamespaceName                 = "DataModels";       // Namespace of the generated classes.

DataContextName               = null;               // DataContext class name. If null - database name + "DB".
BaseDataContextClass          = null;               // Base DataContext class name. If null - LinqToDB.Data.DataConnection.
GenerateConstructors          = true;               // Enforce generating DataContext constructors.
DefaultConfiguration          = null;               // Defines default configuration for default DataContext constructor.

BaseEntityClass               = null;               // Base Entity class name. If null - none.
DatabaseName                  = null;               // Table database name - [Table(Database="DatabaseName")].
GenerateDatabaseName          = false;              // Always generate table database name, even though DatabaseName is null.
IncludeDefaultSchema          = true;               // Default schema name is generated - [Table(Database="Northwind", Schema="dbo", Name="Customers")]

OneToManyAssociationType      = "IEnumerable<{0}>"; // One To Many association type (for members only). Change it to "List<{0}>" if needed.
GenerateAssociations          = true;               // Enforce generating associations as type members.
GenerateBackReferences        = true;               // Enforce generating backreference associations (affects both members and extensions).
GenerateAssociationExtensions = false;              // Enforce generating associations as extension methods. NB: this option does not affect GenerateAssociations. This will require linq2db 1.9.0 and above

ReplaceSimilarTables          = true;               // Replaces stored procedure result class names with similar to existing table class names.
GenerateFindExtensions        = true;               // Generates find extension methods based on PKs information.
IsCompactColumns              = true;               // If true, column compact view.

PluralizeClassNames                 = false;   // If true, pluralizes table class names.
SingularizeClassNames               = true;    // If true, singularizes table class names.
PluralizeDataContextPropertyNames   = true;    // If true, pluralizes DataContext property names.
SingularizeDataContextPropertyNames = false;   // If true, singularizes DataContex pProperty names.

GenerateDataTypes                   = false;   // If true, generates the DataType/Length/Precision/Scale properties of the Column attribute (unless overriden by the properties below).
GenerateDataTypeProperty            = null;    // If true, generates the DataType property of the Column attribute. If false, excludes generation on the DataType property even if GenerateDataTypes == true.
GenerateLengthProperty              = null;    // If true, generates the Length property of the Column attribute. If false, excludes generation on the Length property even if GenerateDataTypes == true.
GeneratePrecisionProperty           = null;    // If true, generates the Precision property of the Column attribute. If false, excludes generation on the Precision property even if GenerateDataTypes == true.
GenerateScaleProperty               = null;    // If true, generates the Scale property of the Column attribute. If false, excludes generation on the Scale property even if GenerateDataTypes == true.
GenerateDbTypes                     = false;   // If true, generates the DbType property of the Column attribute.

GenerateObsoleteAttributeForAliases = false;   // If true, generates [Obsolete] attribute for aliases.
IsCompactColumnAliases              = true;    // If true, column alias compact view.

NormalizeNames                      = true;    // convert some_name to SomeName for types and members

GetSchemaOptions.ExcludedSchemas = new[] { "TestUser", "SYSSTAT" }; // Defines excluded schemas.
GetSchemaOptions.IncludedSchemas = new[] { "TestUser", "SYS" };     // Defines only included schemas.

GetSchemaOptions.ExcludedCatalogs = new[] { "TestUser", "SYSSTAT" }; // Defines excluded catalogs.
GetSchemaOptions.IncludedCatalogs = new[] { "TestUser", "SYS" };     // Defines only included catalogs.

Func<string, bool, string> ToValidName         = ToValidNameDefault;          // Defines function to convert names to valid (My_Table to MyTable) 
Func<string, bool, string> ConvertToCompilable = ConvertToCompilableDefault;  // Converts name to c# compatible. By default removes uncompatible symbols and converts result with ToValidName

Func<ForeignKey, string> GetAssociationExtensionSinglularName = GetAssociationExtensionSinglularNameDefault; // Gets singular method extension method name for association 
Func<ForeignKey, string> GetAssociationExtensionPluralName    = GetAssociationExtensionPluralNameDefault;    // Gets plural method extension method name for association 

```

## Provider specific configurations
### SQL Server
```cs
bool GenerateSqlServerFreeText = true; // Defines wheather to generate extensions for Free Text search, or not
```
### PostgreSQL
```cs
bool GenerateCaseSensitiveNames = false; // Defines whether to generate case sensitive or insensitive names 
```
### Sybase
```cs
bool GenerateSybaseSystemTables = false; // Defines whether to generate Sybase sysobjects tables or not
```

## Customizing generation process

Use the following code to modify your model **before** you call the `GenerateModel()` method.

```c#
GetTable("Person").TypeName  = "MyName";                                            // Replaces table name.
GetTable("Person").BaseClass = "PersonBase, IId";                                   // Set base class & interface for type, null to reset 

GetColumn("Person", "PersonID")    .MemberName   = "ID";                            // Replaces column PersonID of Person table with ID.
GetColumn("Person", "PasswordHash").SkipOnUpdate = true;                            // Set [Column(SkipOnUpdate=true)], same for other column options
GetColumn("Person", "Gender")      .Type         = "global::Model.Gender";          // Change column type

GetFK("Orders", "FK_Orders_Customers").MemberName      = "Customers";               // Replaces association name.
GetFK("Orders", "FK_Orders_Customers").AssociationType = AssociationType.OneToMany; // Changes association type.

SetTable(string tableName,
	string TypeName = null,
	string DataContextPropertyName = null)

	.Column(string columnName, string MemberName = null, string Type = null, bool? IsNullable = null)
	.FK    (string fkName,     string MemberName = null, AssociationType? AssociationType = null)
	;

Model.Usings.Add("MyNamespace"); // Adds using of namespace.

// Replaces all the columns where name is 'TableName' + 'ID' with 'ID'.
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
