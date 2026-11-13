
- [General](#general)
  - [Which async model Linq To DB use?](#which-async-model-linq-to-db-use)
  - [I need to configure connection before or immediately after it opened (e.g. set SQL Server AccessToken or SQLite encryption key)](#i-need-to-configure-connection-before-or-immediately-after-it-opened-eg-set-sql-server-accesstoken-or-sqlite-encryption-key)
- [Mapping](#mapping)
  - [Do I need to use Attribute and/or Code first Mapping?](#do-i-need-to-use-attribute-andor-code-first-mapping)
  - [How can I use calculated fields?](#how-can-i-use-calculated-fields)
  - [How can I use SQL Server spatial types](#how-can-i-use-sql-server-spatial-types)
    - [How to fix it](#how-to-fix-it)
  
# General

## Which async model Linq To DB use?

By default it use `await awaitable.ConfigureAwait(false)` (same as `await awaitable`) mode for internal asyn calls.
If you need it to use another mode you can change it by setting following configuration option:

```cs
// switch to await awaitable.ConfigureAwait(true)
Configuration.ContinueOnCapturedContext = true;
```

## I need to configure connection before or immediately after it opened (e.g. set SQL Server AccessToken or SQLite encryption key)

> [!NOTE]
> You also could use connection factory method or connection interceptor to configure connection, but we recommend to use connection configuration action option as it used also for auto-detection of provider dialect (for providers with auto-detect logic and when auto-detect is enabled).

Configure connection on creation/open (SQL Server and SQLite examples):

```cs
public class MySqlServerDb : DataConnection // or DataContext
{
    public MySqlServerDb(connectionString) : base(
        new DataOptions()
            .UseSqlServer(connectionString)
            .UseBeforeConnectionOpened(connection =>
            {
                connection.AccessToken = "..token here..";
            }))
    {
    }
}

public class MySQLiteDb : DataConnection // or DataContext
{
    public MySQLiteDb(connectionString) : base(
        new DataOptions()
            .UseSQLite(connectionString)
            .UseAfterConnectionOpened(
                connection =>
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"PRAGMA KEY '{key}'";
                    cmd.ExecuteNonQuery();
                },
                // optionally add async version to use non-blocking calls from async execution path
                async (connection, cancellationToken) =>
                {
                    using var cmd = connection.CreateCommand();
                    cmd.CommandText = $"PRAGMA KEY '{key}'";
                    await cmd.ExecuteNonQueryAsync(cancellationToken);
                }))
    {
    }
}

using (var db = new MySqlServerDb())
{
    // queries here will get pre-configured connection
}
```

# Mapping

## Do I need to use Attribute and/or Code first Mapping?

Not strictly. It is possible to use `Linq To DB` with simple, non-attributed POCOs, however there will be specific limitations:

- The biggest of these is that `Linq To DB` will not have information about nullability of reference types (e.g. `string`) and treat all such columns as nullable by default if you don't enable C# nullable reference types annotations in your code and not tell `Linq To DB` to read them.

- Table and column names will have to match the class and property names.
  - You can get around this for the class itself by using the `.TableName()` Method after your `GetTable<>` call (e.x.  `conn.GetTable<MyCleanClassName>().TableName("my_not_so_clean_table_name")` )

- Unless using the explicit insert/update syntax (i.e. `.Value()`/`.Set()`), all columns will be written off the supplied POCO.

## How can I use calculated fields?

You need to mark them to be ignored during insert or update operations, e.g. using `ColumnAttribute` attribute:

```cs
public class MyEntity
{
    [Column(SkipOnInsert = true, SkipOnUpdate = true)]
    public int CalculatedField { get; set; }
}
```

## How can I use SQL Server spatial types

Spatial types for SQL Server provided by:

- [`Microsoft.SqlServer.Types`](https://www.nuget.org/packages/Microsoft.SqlServer.Types) assembly from Microsoft for .NET Framework
- [`dotMorten.Microsoft.SqlServer.Types`](https://www.nuget.org/packages/dotMorten.Microsoft.SqlServer.Types) assembly from [Morten Nielsen](https://github.com/dotMorten) for .NET Core. v1.x versions are for `System.Data.SqlClient` provider and v2.x versions are for `Microsoft.Data.SqlClient` provider
- [`Microsoft.SqlServer.Server`](https://www.nuget.org/packages/Microsoft.SqlServer.Server) nuget for use with [`Microsoft.Data.SqlClient`](https://www.nuget.org/packages/Microsoft.Data.SqlClient) provider (starting from 5.0 release of client)

`Linq To DB` will automatically locate required types. You can register types assembly in `Linq To DB` manually, but it shouldn't be needed:

```cs
SqlServerTools.ResolveSqlTypes(typeof(SqlGeography).Assembly);
```

Main problem that people hit with SQL Server spatial types is following error on select queries: `Can't create '<DB_NAME>.sys.<SPATIAL_TYPE_NAME>' type or '' specific type for <COLUMN_NAME>.`

This happens due to different versions of `Microsoft.SqlServer.Types` assembly, requested by SqlClient, and assembly, referenced by your project.

### How to fix it

For .NET Framework you just need to add assembly bindings redirect to your configuration file to redirect all assembly load requests to your version (make sure that `newVersion` contains proper version of assembly you have):

```xml
<runtime>
    <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
    <dependentAssembly>
        <assemblyIdentity name="Microsoft.SqlServer.Types" publicKeyToken="89845dcd8080cc91" culture="neutral"/>
        <bindingRedirect oldVersion="0.0.0.0-14.0.0.0" newVersion="14.0.0.0" />
    </dependentAssembly>
    </assemblyBinding>
</runtime>
```

For .NET Core it is a bit tricky because:

- .NET Core doesn't support binding redirects
- You need to use 3rd-party assembly with non-Microsoft public key and binding redirects doesn't allow such redirects anyway

To workaround it you need to add custom assembly resolver to your code:

```cs
// subscribe to assembly load request event somewhere in your init code
AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;

Assembly OnAssemblyResolve(AssemblyLoadContext assemblyLoadContext, AssemblyName assemblyName)
{
    try
    {
        // you need to unsubscribe here to avoid StackOverflowException,
        // as LoadFromAssemblyName will go in recursion here otherwise
        AssemblyLoadContext.Default.Resolving -= OnAssemblyResolve;
        // return resolved assembly for cases when it can be resolved
        return assemblyLoadContext.LoadFromAssemblyName(assemblyName);
    }
    catch
    {
        // on failue - check if it failed to load our types assembly
        // and explicitly return it
        if (assemblyName.Name == "Microsoft.SqlServer.Types")
          return typeof(SqlGeography).Assembly;
        // if it failed to load some other assembly - just pass exception as-is
        throw;
    }
    finally
    {
        // don't forget to restore our load handler
        AssemblyLoadContext.Default.Resolving += OnAssemblyResolve;
    }
}
```
