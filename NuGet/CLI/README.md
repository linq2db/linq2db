# LINQ to DB CLI tools

***
> **NOTE**: This is not a library you could reference from your project, but command line utility, installed using `dotnet tool` command (see [installation notes](#installation)).
***

- [LINQ to DB CLI tools](#linq-to-db-cli-tools)
  - [Installation](#installation)
  - [Use](#use)
    - [Usage Examples](#usage-examples)
      - [Generate SQLite database model in current folder](#generate-sqlite-database-model-in-current-folder)
      - [Generate SQLite database model using response file](#generate-sqlite-database-model-using-response-file)
  - [Customize Scaffold with Code](#customize-scaffold-with-code)
    - [Customization with assembly](#customization-with-assembly)
    - [Customization with T4 template](#customization-with-t4-template)
    - [Interceptors Overview](#interceptors-overview)
      - [Schema Load Interceptors](#schema-load-interceptors)
      - [Data Model Interceptors](#data-model-interceptors)
        - [Type mapping interceptor](#type-mapping-interceptor)
      - [Code Model Interceptors](#code-model-interceptors)
      - [Code Generation Interceptors](#code-generation-interceptors)

## Installation

> Requres .NET Core 3.1 or higher.

Install as global tool:

`dotnet tool install -g linq2db.cli`

Update:

`dotnet tool update -g linq2db.cli`

General information on .NET Tools could be found [here](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools)

## Use

To invoke tool use `dotnet-linq2db <PARAMETERS>` or `dotnet linq2db <PARAMETERS>` command.

Available commands:

- `dotnet linq2db help`: prints general help
- `dotnet linq2db help scaffold`: prints help for `scaffold` command
- `dotnet linq2db scaffold <options>`: performs database model scaffolding
- `dotnet linq2db template [-o template_path]`: creates base T4 template file for scaffolding customization code

For list of available options, use `dotnet linq2db help scaffold` command.

### Usage Examples

#### Generate SQLite database model in current folder

This command uses minimal set of options, required for scaffolding (database provider and connection string) and generates database model classes in current folder.

`dotnet linq2db scaffold -p SQLite -c "Data Source=c:\Databases\MyDatabase.sqlite"`

#### Generate SQLite database model using response file

This command demonstrates use of configuration file with scaffold options combined with command line options.

`dotnet linq2db scaffold -i database.json -c "Data Source=c:\Databases\MyDatabase.sqlite"`

database.json file:

```json
{
    "general": {
        "provider": "SQLite",
        "connection": "Data Source=c:\\Databases\\TestDatabase.sqlite",
        "output": "c:\\MyProject\\DbModel",
        "overwrite": true
    }
}
```

Here you can see that connection string passed using both command line and `json` config file. In such cases option passed in command line takes precedence.

Scaffold configs (response files) are convenient in many ways:

- you can store scaffolding options for your project in source control and share with other developers
- with many options it is hard to work with command line
- some options not available from CLI or hard to use due to CLI nature (e.g. various issues with escaping of parameters)

## Customize Scaffold with Code

For more advanced scaffolding configuration you can use scaffold interceptor class (inherited from `ScaffoldInterceptors` class), passed as pre-built assembly (don't forget that scaffold utility use .net core 3.1+, so don't target it with .NET Framework TFM) or T4 template.
Class, inherited from `ScaffoldInterceptors` should have default constructor or constructor with `ScaffoldOptions` parameters.

Main difference between assembly and T4 approach is:

- with assembly you can write customization in any .net language in your favorite IDE, but need to build it to .net assembly to use
- with T4 you can use only C# and IDE support for T4 templates is limited, but you will have ready-to-use T4 template to modify and compilation will be done by cli tool

To invoke scaffolding with code-based customization use `--customize path_to_file` option:

`dotnet linq2db scaffold -i database.json -c "Data Source=c:\Databases\MyDatabase.sqlite" --customize CustomAssembly.dll`

`dotnet linq2db scaffold -i database.json -c "Data Source=c:\Databases\MyDatabase.sqlite" --customize CustomTemplate.t4`

CLI tool will detect custmization approach using file extension:

- `.dll`: referenced file will be loaded as assembly
- any other extension: referenced file will be treated as T4 template

### Customization with assembly

1. Create new .net library project and reference `linq2db.Tools` nuget
2. Add class, inherited from `LinqToDB.Scaffold.ScaffoldInterceptors` and override required customization methods
3. Build assembly and use it with `--custmize` option

> CLI tool tries to locate and load referenced 3rd-party assemblies, used by customization assembly automatically.
>
> If it fails to find referenced assembly, you can try to enable local copy behavior by adding following property to project file:
>
> `<CopyLocalLockFileAssemblies>true</CopyLocalLockFileAssemblies>`

### Customization with T4 template

1. Generate initial T4 template file using `dotnet linq2db template` command
2. Edit `Interceptors` class methods in template with required customization logic
3. Use template file with `--custmize` option

### Interceptors Overview

Scaffold process is a multi-stange process with following steps:

- database schema load
- generation of data context object model from schema
- generation of data context code model from object model
- generation of source code from code model

We allow injection of interceptors/extension points at some of those stages. They could be split into three categories:

- Database schema load interceptors. Those interceptors allow user to add, remove or modify information about database objects, used for data model generation.
- Database type mapping interceptor. This interceptor allows user to override default .NET type used for specific database type in data model or specify .NET type for database type, not known to scaffold utility (e.g. some custom database type)
- Data model interceptors. Those interceptors work with generate data model objects and allow user to modify them before they will be converted to source code. Such objects include:
  - entities (table/view mapping classes)
  - methods for stored procedure or function mapping
  - associations (foreign key relations)
  - some other generated objects.

#### Schema Load Interceptors

During schema load stage user can filter, modify or even add new database object descriptors to database schema. There is interception method for each of currently supported database object types:

```cs
IEnumerable<Table>             GetTables            (IEnumerable<Table>             tables);
IEnumerable<View>              GetViews             (IEnumerable<View>              views);
IEnumerable<ForeignKey>        GetForeignKeys       (IEnumerable<ForeignKey>        keys);
IEnumerable<StoredProcedure>   GetProcedures        (IEnumerable<StoredProcedure>   procedures);
IEnumerable<TableFunction>     GetTableFunctions    (IEnumerable<TableFunction>     functions);
IEnumerable<ScalarFunction>    GetScalarFunctions   (IEnumerable<ScalarFunction>    functions);
IEnumerable<AggregateFunction> GetAggregateFunctions(IEnumerable<AggregateFunction> functions)
```

> Database object-specific interceptor will be called only if specific object type load was allowed in options (see `--objects` parameter).

<details>
  <summary>Interceptor implementation example (table)</summary>

```cs
public override IEnumerable<Table> GetTables(IEnumerable<Table> tables)
{
    foreach (var table in tables)
    {
        if (table.Name.Schema == "private")
        {
            // hide/ignore tables from "private" schema
            // note that it could be also done using --exclude-schemas CLI option
            continue;
        }

        if (table.Name.Name.StartsWith("test_"))
        {
            // modify record: remove test_ prefix from table name
            yield return table with
            {
                Name = table.Name with
                {
                    Name = table.Name.Name.Substring("test_".Length)
                }
            };
            continue;
        }

        yield return table;
    }

    // add new table record, not returned by schema API
    yield return new Table(
        new SqlObjectName("my_table_name"),
        null,
        new[] { new Column("pk", null, new DatabaseType("BIGINT", null, null, null), false, false, false) },
        new Identity("pk", null),
        new PrimaryKey("PK_my_table_name", new[] { "pk" }));
}
```

</details>

<details>
  <summary>Database schema models</summary>

```cs
// generic descriptors
public readonly record struct SqlObjectName(string Name, string? Server = null, string? Database = null, string? Schema = null, string? Package = null);

public sealed record DatabaseType(string? Name, long? Length, int? Precision, int? Scale);

// table/view descriptors
public sealed record Table(
    SqlObjectName               Name,
    string?                     Description,
    IReadOnlyCollection<Column> Columns,
    Identity?                   Identity,
    PrimaryKey?                 PrimaryKey);

public sealed record View(
    SqlObjectName               Name,
    string?                     Description,
    IReadOnlyCollection<Column> Columns,
    Identity?                   Identity,
    PrimaryKey?                 PrimaryKey);

public sealed record ForeignKey(
    string                                 Name,
    SqlObjectName                          Source,
    SqlObjectName                          Target,
    IReadOnlyList<ForeignKeyColumnMapping> Relation);

public sealed record Column(string Name, string? Description, DatabaseType Type, bool Nullable, bool Insertable, bool Updatable);

public sealed record Identity(string Column, Sequence? Sequence);

public sealed record Sequence(SqlObjectName? Name);

public sealed record PrimaryKey(string? Name, IReadOnlyCollection<string> Columns);

public sealed record ForeignKeyColumnMapping(string SourceColumn, string TargetColumn);

// procedures and functions descriptors
public sealed record StoredProcedure(
    SqlObjectName                               Name,
    string?                                     Description,
    IReadOnlyCollection<Parameter>              Parameters,
    Exception?                                  SchemaError,
    IReadOnlyList<IReadOnlyList<ResultColumn>>? ResultSets,
    Result                                      Result);

public sealed record TableFunction(
    SqlObjectName                      Name,
    string?                            Description,
    IReadOnlyCollection<Parameter>     Parameters,
    Exception?                         SchemaError,
    IReadOnlyCollection<ResultColumn>? Result);

public sealed record AggregateFunction(
    SqlObjectName                  Name,
    string?                        Description,
    IReadOnlyCollection<Parameter> Parameters,
    ScalarResult                   Result);

public sealed record ScalarFunction(
    SqlObjectName                  Name,
    string?                        Description,
    IReadOnlyCollection<Parameter> Parameters,
    Result                         Result);

public sealed record Parameter(
    string             Name,
    string?            Description,
    DatabaseType       Type,
    bool               Nullable,
    ParameterDirection Direction);

public enum ParameterDirection
{
    Input,
    Output,
    InputOutput
}

public enum ResultKind
{
    Void,
    Tuple,
    Scalar,
}

public sealed record ScalarResult(string? Name, DatabaseType Type, bool Nullable)
    : Result(ResultKind.Scalar);

public sealed record TupleResult(IReadOnlyCollection<ScalarResult> Fields, bool Nullable)
    : Result(ResultKind.Tuple);

public sealed record VoidResult() : Result(ResultKind.Void);

public sealed record ResultColumn(string? Name, DatabaseType Type, bool Nullable);
```

</details>

#### Type mapping interceptor

This interceptor allows user to specify which .NET type should be used for specific database type and usefull in several cases:

- when default type mapping use wrong type
- user wants to use different type for specific database type
- default type mapping cannot map type and uses fallback type (`System.Object`)

> Database type doesn't include nullability flag. Nullability applied to type automatically later.

```cs
// IMPORTANT: this method called only once for each database type
// ITypeParser inte
TypeMapping? GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping? defaultMapping);

// IType is internal .NET type abstraction created only using ITypeParser interface methods
// DataType is LinqToDB.DataType mapping enum
public sealed record TypeMapping(IType CLRType, DataType? DataType);

// this interface provides helpers to create type tokens from System.Type or type name
public interface ITypeParser
{
    // create type token from .NET Type
    IType Parse(Type type);
    IType Parse<T>();

    // create type token from full type name (with namespace)
    // namespaces and type separated by dot (.)
    // nested types separated by plus (+)
    // generic types allowed
    // Example: "My.NameSpace.WrapperClass+NestedClass<int, string>"
    // 
    // valueType: specify that type is reference or value type to properly handle type nullability
    IType Parse(string typeName, bool valueType);
}
```

<details>
  <summary>Example</summary>

```cs
// defaultMapping could be null if tool cannot map database type
// in such cases default type (System.Object) will be used in mapping
public override TypeMapping? GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping? defaultMapping)
{
    // use provider-specific (Npgsql) type for "date" database type
    if (databaseType?.Name?.ToLower() == "date")
        return new TypeMapping(typeParser.Parse<NpgsqlTypes.NpgsqlDate>(), null);
        // or use string if Npgsql assembly not referenced
        // return new TypeMapping(typeParser.Parse("NpgsqlTypes.NpgsqlDate", true), null);

    // for other types use default mapping
    return defaultMapping;
}
```

</details>

#### Data Model Interceptors

This group of interceptors allow user to modify generated data model objects before they converted to source code.

```cs
// This interceptor works with single entity model object that
// corresponds to database table or view.
// List of options, available for modification:
// - Table/view mapping metadata
// - Entity class code generation options (name, visibility, inheritance, etc)
// - Data context table property for entity
// - Find/FindQuery entity extensions generation options
// - List of entity column properties (column metada and property code generation options)
void PreprocessEntity(ITypeParser typeParser, EntityModel entityModel);
```

<details>
  <summary>Interceptor implementation examples</summary>

```cs
// several use-cases cases of entity customization
public override void PreprocessEntity(ITypeParser typeParser, EntityModel entityModel)
{
    // change type for specific column: use DateOnly type for event_date columns in audit table
    if (entityModel.Metadata.Name?.Name.StartsWith("Audit$") == true)
    {
        var dateColumn = entityModel.Columns.Where(c => c.Metadata.Name == "event_date").Single();
        // create type from name string because CLI tool use .net core 3.1 runtime without DateOnly type
        dateColumn.Property.Type = typeParser.Parse("System.DateOnly", true);
    }

    // for log tables, remove table access properties from data context class
    if (entityModel.Metadata.Name?.Name.StartsWith("Logs$") == true)
        entityModel.ContextProperty = null;

    // for table with name "alltypes" we cannot recognize separate words to properly generate class name
    // let's modify generated entity class name in model
    if (entityModel.Class.Name == "Alltypes")
        entityModel.Class.Name = "AllTypes";

    // mark column as non-editable
    var creatorColumn = entityModel.Columns.Where(c => c.Metadata.Name == "created_by").FirstOrDefault();
    if (creatorColumn != null)
        creatorColumn.Metadata.SkipOnUpdate = true;
}
```

</details>

<details>
  <summary>Data model classes</summary>

```cs
// data model descriptors

// entity descriptor
public sealed class EntityModel
{
    public EntityMetadata    Metadata             { get; set; }
    public ClassModel        Class                { get; set; }
    public PropertyModel?    ContextProperty      { get; set; }
    public FindTypes         FindExtensions       { get; set; }
    public bool              ImplementsIEquatable { get; set; }
    public List<ColumnModel> Columns              { get;      }
}

// column descriptor
public sealed class ColumnModel
{
    public ColumnMetadata Metadata { get; set; }
    public PropertyModel  Property { get; set; }
}

// Flags to specify generated Find/FindQuery extensions per-entity
[Flags]
public enum FindTypes
{
    // specify generated method signatures:

    None,
    /// <summary>
    /// Method version: sync Find().
    /// </summary>
    Sync                       = 0x0001,
    /// <summary>
    /// Method version: async FindAsync().
    /// </summary>
    Async                      = 0x0002,
    /// <summary>
    /// Method version: FindQuery().
    /// </summary>
    Query                      = 0x0004,

    // specify what should be passed to methods: primary key values or whole entity instance

    /// <summary>
    /// Method primary key: from parameters.
    /// </summary>
    ByPrimaryKey               = 0x0010,
    /// <summary>
    /// Method primary key: from entity object.
    /// </summary>
    ByEntity                   = 0x0020,

    // specify extended type

    /// <summary>
    /// Method extends: entity table.
    /// </summary>
    OnTable                    = 0x0100,
    /// <summary>
    /// Method extends: generated context.
    /// </summary>
    OnContext                  = 0x0200,

    // some ready-to-use flags combinations
    FindByPkOnTable            = Sync | ByPrimaryKey | OnTable,
    FindAsyncByPkOnTable       = Async | ByPrimaryKey | OnTable,
    FindQueryByPkOnTable       = Query | ByPrimaryKey | OnTable,
    FindByRecordOnTable        = Sync | ByEntity | OnTable,
    FindAsyncByRecordOnTable   = Async | ByEntity | OnTable,
    FindQueryByRecordOnTable   = Query | ByEntity | OnTable,
    FindByPkOnContext          = Sync | ByPrimaryKey | OnContext,
    FindAsyncByPkOnContext     = Async | ByPrimaryKey | OnContext,
    FindQueryByPkOnContext     = Query | ByPrimaryKey | OnContext,
    FindByRecordOnContext      = Sync | ByEntity | OnContext,
    FindAsyncByRecordOnContext = Async | ByEntity | OnContext,
    FindQueryByRecordOnContext = Query | ByEntity | OnContext,
}

public sealed class StoredProcedureModel : TableFunctionModelBase
{
    public FunctionParameterModel? Return  { get; set; }
    public List<FunctionResult>    Results { get; set; } = new();
}

public sealed class TableFunctionModel : TableFunctionModelBase
{
    public string                MethodInfoFieldName { get; set; }
    public TableFunctionMetadata Metadata            { get; set; }
    public FunctionResult?       Result              { get; set; }
}

public sealed class ScalarFunctionModel : ScalarFunctionModelBase
{
    public IType?      Return      { get; set; }
    public TupleModel? ReturnTuple { get; set; }
}

public sealed class AggregateFunctionModel : ScalarFunctionModelBase
{
    public IType ReturnType { get; set; }
}

public abstract class TableFunctionModelBase : FunctionModelBase
{
    public SqlObjectName Name  { get; set; }
    public string?       Error { get; set; }
}

public abstract class ScalarFunctionModelBase : FunctionModelBase
{
    public FunctionMetadata Metadata { get; set; }
}

public abstract class FunctionModelBase
{
    public MethodModel                  Method     { get; set; }
    public List<FunctionParameterModel> Parameters { get;      } = new();
}

public sealed class FunctionParameterModel
{
    public ParameterModel                 Parameter  { get; set; }
    public string?                        DbName     { get; set; }
    public DatabaseType?                  Type       { get; set; }
    public DataType?                      DataType   { get; set; }
    public bool                           IsNullable { get; set; }
    public System.Data.ParameterDirection Direction  { get; set; }
}

public sealed class TupleModel
{
    public ClassModel            Class     { get; set; }
    public bool                  CanBeNull { get; set; }
    public List<TupleFieldModel> Fields    { get;      } = new ();
}

public sealed class TupleFieldModel
{
    public PropertyModel Property { get; set; }
    public DatabaseType  Type     { get; set; }
    public DataType?     DataType { get; set; }
}

public sealed record FunctionResult(
    ResultTableModel?     CustomTable,
    EntityModel?          Entity,
    AsyncProcedureResult? AsyncResult);

public sealed class ResultTableModel
{
    public ClassModel        Class   { get; set; }
    public List<ColumnModel> Columns { get;      } = new ();
}

public sealed class AsyncProcedureResult
{
    public ClassModel                                        Class               { get; set; }
    public PropertyModel                                     MainResult          { get; set; }
    public Dictionary<FunctionParameterModel, PropertyModel> ParameterProperties { get; } = new();
}

public sealed class AssociationModel
{
    public AssociationMetadata SourceMetadata         { get; set; }
    public AssociationMetadata TargetMetadata         { get; set; }
    public EntityModel         Source                 { get; set; }
    public EntityModel         Target                 { get; set; }
    public PropertyModel?      Property               { get; set; }
    public PropertyModel?      BackreferenceProperty  { get; set; }
    public MethodModel?        Extension              { get; set; }
    public MethodModel?        BackreferenceExtension { get; set; }
    public ColumnModel[]?      FromColumns            { get; set; }
    public ColumnModel[]?      ToColumns              { get; set; }
    public bool                ManyToOne              { get; set; }
}

```

</details>

<details>
  <summary>Code models</summary>

 ```cs

public sealed class ClassModel
{
    public string?              Summary          { get; set; }
    public string               Name             { get; set; }
    public string?              Namespace        { get; set; }
    public IType?               BaseType         { get; set; }
    public List<IType>?         Interfaces       { get; set; }
    public Modifiers            Modifiers        { get; set; }
    public string?              FileName         { get; set; }
    public List<CodeAttribute>? CustomAttributes { get; set; }
}

public sealed class PropertyModel
{
    public string               Name             { get; set; }
    public IType?               Type             { get; set; }
    public string?              Summary          { get; set; }
    public Modifiers            Modifiers        { get; set; }
    public bool                 IsDefault        { get; set; }
    public bool                 HasSetter        { get; set; }
    public string?              TrailingComment  { get; set; }
    public List<CodeAttribute>? CustomAttributes { get; set; }
}

public sealed class MethodModel
{
    public string?              Summary          { get; set; }
    public string               Name             { get; set; }
    public Modifiers            Modifiers        { get; set; }
    public List<CodeAttribute>? CustomAttributes { get; set; }
}

public sealed class ParameterModel
{
    public string                 Name        { get; set; }
    public IType                  Type        { get; set; }
    public string?                Description { get; set; }
    public CodeParameterDirection Direction   { get; set; }
}

// various modifiers on type/type member
[Flags]
public enum Modifiers
{
    None      = 0,
    Public    = 0x0001,
    Protected = 0x0002,
    Internal  = 0x0004,
    Private   = 0x0008,
    New       = 0x0010,
    Override  = 0x0020,
    Abstract  = 0x0040,
    Sealed    = 0x0080,
    Partial   = 0x0100,
    Extension = 0x0200 | Static,
    ReadOnly  = 0x0400,
    Async     = 0x0800,
    Static    = 0x1000,
    Virtual   = 0x2000,
}

public enum CodeParameterDirection
{
    In,
    Ref,
    Out
}

 ```

</details>

<details>
  <summary>Metadata models</summary>

 ```cs
public sealed class EntityMetadata
{
    public SqlObjectName? Name                      { get; set; }
    public bool           IsView                    { get; set; }
    public string?        Configuration             { get; set; }
    public bool           IsColumnAttributeRequired { get; set; } = true;
    public bool           IsTemporary               { get; set; }
    public TableOptions   TableOptions              { get; set; }
}

public sealed class ColumnMetadata
{
    public string?       Name              { get; set; }
    public DatabaseType? DbType            { get; set; }
    public DataType?     DataType          { get; set; }
    public bool          CanBeNull         { get; set; }
    public bool          SkipOnInsert      { get; set; }
    public bool          SkipOnUpdate      { get; set; }
    public bool          IsIdentity        { get; set; }
    public bool          IsPrimaryKey      { get; set; }
    public int?          PrimaryKeyOrder   { get; set; }
    public string?       Configuration     { get; set; }
    public string?       MemberName        { get; set; }
    public string?       Storage           { get; set; }
    public string?       CreateFormat      { get; set; }
    public bool          IsColumn          { get; set; } = true;
    public bool          IsDiscriminator   { get; set; }
    public bool          SkipOnEntityFetch { get; set; }
    public int?          Order             { get; set; }
}

// table function/stored procedure
public sealed class TableFunctionMetadata
{
    public SqlObjectName? Name          { get; set; }
    public string?        Configuration { get; set; }
    public int[]?         ArgIndices    { get; set; }
}

// scalar/aggregate function
public sealed class FunctionMetadata
{
    public SqlObjectName?      Name             { get; set; }
    public int[]?              ArgIndices       { get; set; }
    public string?             Configuration    { get; set; }
    public bool?               ServerSideOnly   { get; set; }
    public bool?               PreferServerSide { get; set; }
    public bool?               InlineParameters { get; set; }
    public bool?               IsPredicate      { get; set; }
    public bool?               IsAggregate      { get; set; }
    public bool?               IsWindowFunction { get; set; }
    public bool?               IsPure           { get; set; }
    public bool?               CanBeNull        { get; set; }
    public Sql.IsNullableType? IsNullable       { get; set; }
}

public sealed class AssociationMetadata
{
    public bool             CanBeNull             { get; set; }
    public ICodeExpression? ThisKeyExpression     { get; set; }
    public ICodeExpression? OtherKeyExpression    { get; set; }
    public string?          Configuration         { get; set; }
    public string?          Alias                 { get; set; }
    public string?          Storage               { get; set; }
    public string?          ThisKey               { get; set; }
    public string?          OtherKey              { get; set; }
    public string?          ExpressionPredicate   { get; set; }
    public string?          QueryExpressionMethod { get; set; }
}

```

</details>
