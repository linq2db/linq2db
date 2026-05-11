- [Configuration Changes](#Configuration-changes)
- [Fluent Mapping Changes](#fluent-mapping-changes)
- [Custom `IMetadataReader` Implementation Changes](#custom-iMetadataReader-implementation-changes)

# Configuration Changes

In this version we introduce new `Linq To DB` context configuration mechanism based on `DataOptions[<T>]` record classes. It replaces similar mechanism in previous versions: `LinqToDBConnectionOptionsBuilder` options builder class alongside with `LinqToDBConnectionOptions[<T>]` classes.

Main differences are:
- there is no sepearate options builder class to collect options and then convert it to options class instance - all option changes applied directly to `DataOptions` record class;
- because new options use record class, they are immutable.

To migrate old configuration code you need:
- replace use of `LinqToDBConnectionOptions` with `DataOptions`
- create `DataOptions` instance instead of `LinqToDBConnectionOptionsBuilder` instance
- don't forget to consume new options instance, returned by option setup methods
- `linq2db.AspNet` configuration extension method should return `DataOptions` instance (instead of being void action in previous versions)

## C# 9 and .NET Framework support

If your project use use lower C# versions than C# 9 (e.g. .NET Framework project), which is required by `record class`es you will receive error if you will try to modify options directly.

To avoid error there are two options:
- update `LangVersion` for your project to C# 9 or higher
- (recommended) modify options using `With*`\`Use*` extension methods that cover all available options already

Note that you will get this error (with lower C# versions) if you configure `BulkCopyOptions` class in your code. In new version we converted it to `record class` and you will need to refactor `BulkCopyOptions` configuration to use extensions. See sample migration below.

### BulkCopyOptions configuration migration

Old code:

```cs
var options = new BulkCopyOptions()
{
    MaxBatchSize = 1000,
    KeepIdentity = true
};

options.TableName = "temp_table";
```

New code:

```cs
// options management code done by extension methods
// and C# compiler has nothing to complain about
var options = new BulkCopyOptions()
    .WithMaxBatchSize(1000)
    .WithKeepIdentity(true)
    .WithTableName("temp_table");
```

## Context configuration migration

Old code:

```cs

var builder = new LinqToDbConnectionOptionsBuilder();
builder.UseSqlServer(connectionString);
builder.UseLoggerFactory(logger);

var dc = new DataConnection(builder.Build());

```

New code:
```cs

// builder class replaced with options instance
var options = new DataOptions()
    // use fluent API to pass modified options from one method to another
    .UseSqlServer(connectionString);

// don't forget to save new options instance after change
options = options.UseLoggerFactory(logger);

// no Build method anymore
var dc = new DataConnection(options);

```

# Fluent Mapping Changes

If you used fluent API for mappings configuration, you will need to make several changes to your code. What was changed:
- `GetFluentMappingBuilder` method was removed from `MappingSchema` and `IDataContext` to:
  - not promote bad practice when mappings were configured on context instance level
  - make explicit breaking change to API to bring your attention to other required changes to fluent configuration code
- to add fluent mappings to `MappingSchema`, you need to call `Build()` method on mappings builder; previous versions were adding mappings to mapping schema on the fly. This change also means that you cannot add mapping schema with fluent mappings to context if you didn't finished mappings configuration.

## Migration example

Old code:

```cs
// create new data context
using var db = new MyDataContext();

// create new mapping schema for fluent mappings
// and add it to context
var fluentMappings = new MappingSchema();
db.AddMappingSchema(fluentMappings);

// get mappings builder from mapping schema
var builder = fluentMappings.GetFluentMappingBuilder();
// or using context mapping schema directly
// var builder = db.MappingSchema.GetFluentMappingBuilder();
// or
// var builder = db.GetFluentMappingBuilder();

// configure mappings
builder.Entity<MyEntity>().Property(e => e.Id).IsPrimaryKey();

// all done, you can use your mappings already

// delete entity by primary key value from Id property
db.Delete(entity);
```

While it worked before, that example contains a lot of issues with performance and will not work in new version for at least two reasons:
- there is no `Build()` method call, so context don't know anything about new mappings;
- `AddMappingSchema(..)` method called before mappings configured - this will also not work anymore, because fluent mappings not set to mapping schema yet by `Build()` call;
- if you used `GetFluentMappingBuilder` method on context/context schema - it will also could fail as we changed library defaults and context mapping schema is not editable by default anymore.

Except those obvious breaking changes this example also has big performance issue: if you need to configure mappings you need to do it once and then use pre-configured mapping schema with all context instances otherwise you will have big performance penalty as `Linq To DB` will be unable to reuse cached mapping information.

Proper configuration of (fluent) mappings:

```cs
// setup mappings once, e.g. in application startup or MyDataContext static constructor:

private readonly MappingSchema _mappings;

static MyDataContext()
{
    // create shared mapping schema instance with all context mappings
    _mappings = new MappingSchema();

    // configure fluent mappings
    // create builder instance explicitly
    new FluentMappingBuilder(_mappings)
        .Entity<MyEntity>()
            .Property(e => e.Id)
                .IsPrimaryKey()
        // (!) commit mappings to mapping schema
        Build();

    // also we can configure additional mappings
    _mappings.SetConvertExpression(...);
}

// pass mapping schema to context base contructor (e.g. using DataOptions)
public MyDataContext(DataOptions options)
    : base(options.UseMappingSchema(_mappings))
{
}
```

# Custom `IMetadataReader` Implementation Changes

If you implemented custom metadata reader (using `IMetadataReader` interface) in your project, you need to modify implementation contract and logic of `GetAttributes` methods.

What was changed:
- removed generic `T where T: Attribute` parameter from `GetAttributes` methods
- changed return type from `T[]` to `MappingAttributes[]` for `GetAttributes` methods
- removed `bool inherited` argument from `GetAttributes`
- added new `string IMetadataReader.GetObjectID()` method

## new `GetObjectID` method

This method returns string, that is used to identify metadata reader for caching purposes:
- if your reader implementation returns same data for all instances of reader, it could return static string (e.g. reader class name)
- if different instances of reader could return different metadata, you should calculate unique identifier based on metadata; or better make sure your application doesn't create multiple instances of reader for same metadata source so you can use something like `GetHashCode.ToString()` for identification

## changes `GetAttributes` logic

Previously `Linq To DB` called `GetAttributes` method with generic type parameter to specify which attribute it wants to receive. In new version `Linq To DB` will request all mapping attributes for specific member or type. If your implementation supported several attribute types in single method (e.g. `ColumnAttribute` and `Sql.ExpressionAttribute`) you need to change it to return both attributes with single call.