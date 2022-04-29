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

Each of those steps has own interception points to customize model.

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
        new ObjectName(null, null, null, "my_table_name"),
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
public sealed record ObjectName(string? Server, string? Database, string? Schema, string Name);

public sealed record DatabaseType(string? Name, long? Length, int? Precision, int? Scale);

// table/view descriptors
public sealed record Table(
    ObjectName                  Name,
    string?                     Description,
    IReadOnlyCollection<Column> Columns,
    Identity?                   Identity,
    PrimaryKey?                 PrimaryKey);

public sealed record View(
    ObjectName                  Name,
    string?                     Description,
    IReadOnlyCollection<Column> Columns,
    Identity?                   Identity,
    PrimaryKey?                 PrimaryKey);

public sealed record ForeignKey(
    string                                 Name,
    ObjectName                             Source,
    ObjectName                             Target,
    IReadOnlyList<ForeignKeyColumnMapping> Relation);

public sealed record Column(string Name, string? Description, DatabaseType Type, bool Nullable, bool Insertable, bool Updatable);

public sealed record Identity(string Column, Sequence? Sequence);

public sealed record Sequence(ObjectName? Name);

public sealed record PrimaryKey(string? Name, IReadOnlyCollection<string> Columns);

public sealed record ForeignKeyColumnMapping(string SourceColumn, string TargetColumn);

// procedures and functions descriptors
public sealed record StoredProcedure(
    ObjectName                                  Name,
    string?                                     Description,
    IReadOnlyCollection<Parameter>              Parameters,
    Exception?                                  SchemaError,
    IReadOnlyList<IReadOnlyList<ResultColumn>>? ResultSets,
    Result                                      Result);

public sealed record TableFunction(
    ObjectName                         Name,
    string?                            Description,
    IReadOnlyCollection<Parameter>     Parameters,
    Exception?                         SchemaError,
    IReadOnlyCollection<ResultColumn>? Result);

public sealed record AggregateFunction(
    ObjectName                     Name,
    string?                        Description,
    IReadOnlyCollection<Parameter> Parameters,
    ScalarResult                   Result);

public sealed record ScalarFunction(
    ObjectName                     Name,
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

#### Data Model Interceptors

Those interceptors called during data context model generation from database schema.

TBD: not implemented yet

##### Type mapping interceptor

This interceptor allows user to specify which .NET type should be used for specific database type and usefull in several cases:

- when default type mapping use wrong type
- user wants to use different type for specific database type
- default type mapping cannot map type and uses fallback type (`System.Object`)

> Database type doesn't include nullability flag. Nullability applied to type automatically later.

```cs
// IMPORTANT: this method called only once for each database type
// ITypeParser inte
TypeMapping GetTypeMapping(DatabaseType databaseType, ITypeParser typeParser, TypeMapping defaultMapping);

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

#### Code Model Interceptors

> TBD: not implemented yet

#### Code Generation Interceptors

> TBD: not implemented yet
