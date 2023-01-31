# LINQ to DB<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

- [Standout Features](#standout-features)
  - [Related Linq To DB and 3rd-party projects](#related-linq-to-db-and-3rd-party-projects)
- [Configuring connection strings](#configuring-connection-strings)
  - [Passing Into Constructor](#passing-into-constructor)
  - [Using Config File (.NET Framework)](#using-config-file-net-framework)
  - [Using Connection String Settings Provider](#using-connection-string-settings-provider)
  - [Use with ASP.NET Core](#use-with-aspnet-core)
- [Define **POCO** class](#define-poco-class)
  - [Configuration using mapping attributes](#configuration-using-mapping-attributes)
  - [Fluent Configuration](#fluent-configuration)
  - [Inferred Configuration](#inferred-configuration)
  - [DataConnection class](#dataconnection-class)
- [Queries](#queries)
  - [Selecting Columns](#selecting-columns)
  - [Composing queries](#composing-queries)
  - [Paging](#paging)
  - [Joins](#joins)
  - [Creating your POCOs](#creating-your-pocos)
  - [Insert](#insert)
  - [Update](#update)
  - [Delete](#delete)
  - [Bulk Copy](#bulk-copy)
  - [Transactions](#transactions)
  - [Merge](#merge)
  - [Window (Analytic) Functions](#window-analytic-functions)
- [MiniProfiler](#miniprofiler)
- [More](#more)

LINQ to DB is the fastest LINQ database access library offering a simple, light, fast, and type-safe layer between your POCO objects and your database.

Architecturally it is one step above micro-ORMs like Dapper, Massive, or PetaPoco, in that you work with LINQ expressions, not with magic strings, while maintaining a thin abstraction layer between your code and the database. Your queries are checked by the C# compiler and allow for easy refactoring.

However, it's not as heavy as LINQ to SQL or Entity Framework. There is no change-tracking, so you have to manage that yourself, but on the positive side you get more control and faster access to your data.

In other words **LINQ to DB is type-safe SQL**.

## Standout Features

- Rich Querying API:
  - [Explicit Join Syntax](https://linq2db.github.io/articles/sql/Join-Operators.html) (In addition to standard LINQ join syntax)
  - [CTE Support](https://linq2db.github.io/articles/sql/CTE.html)
  - [Bulk Copy/Insert](https://linq2db.github.io/articles/sql/Bulk-Copy.html)
  - [Window/Analytic Functions](https://linq2db.github.io/articles/sql/Window-Functions-%28Analytic-Functions%29.html)
  - [Merge API](https://linq2db.github.io/articles/sql/merge/Merge-API-Description.html)
- Extensibility:
  - [Ability to Map Custom SQL to Static Functions](https://github.com/linq2db/linq2db/tree/master/Source/LinqToDB/Sql/)

See [Github.io documentation](https://linq2db.github.io/index.html) for more details.

<!-- You can visit our [blog](http://blog.linq2db.com/) -->

Code examples and demos can be found [here](https://github.com/linq2db/examples) or in [tests](https://github.com/linq2db/linq2db/tree/master/Tests/Linq).

### Related Linq To DB and 3rd-party projects

- [linq2db.EntityFrameworkCore](https://github.com/linq2db/linq2db.EntityFrameworkCore) (adds support for linq2db functionality in EF.Core projects)
- [LinqToDB.Identity](https://github.com/linq2db/LinqToDB.Identity) - ASP.NET Core Identity provider using Linq To DB
- [LINQPad Driver](https://github.com/linq2db/linq2db.LINQPad)
- [DB2 iSeries Provider](https://github.com/LinqToDB4iSeries/Linq2DB4iSeries)
- [ASP.NET CORE 2 Template](https://github.com/David-Mawer/LINQ2DB-MVC-Core-2/tree/master/LINQ2DB-MVC-Core-2)
- [ASP.NET CORE 3 Template with Angular](https://github.com/David-Mawer/LINQ2DB-AngularWebApp-Core-3)
- [ASP.NET CORE 5 Template](https://github.com/David-Mawer/LINQ2DB-MVC-Core-5)
- [PostGIS extensions for linq2db](https://github.com/apdevelop/linq2db-postgis-extensions)

Notable open-source users:

- [nopCommerce](https://github.com/nopSolutions/nopCommerce) - popular open-source e-commerce solution
- [OdataToEntity](https://github.com/voronov-maxim/OdataToEntity) - library to create OData service from database context
- [SunEngine](https://github.com/sunengine/SunEngine) - site, blog and forum engine

## Configuring connection strings

### Passing Into Constructor

You can simply pass connection string into `DataConnection` or `DataContext` constructor using [`DataOptions`](https://linq2db.github.io/api/LinqToDB.DataOptions.html) class.

Minimal configuration example:
```cs
var db = new DataConnection(
  new DataOptions()
    .UseSqlServer(@"Server=.\;Database=Northwind;Trusted_Connection=True;"));
```

Use connection configuration action to setup SqlClient-specific authentication token:

```cs
var options = new DataOptions()
  .UseSqlServer(connectionString, SqlServerVersion.v2017, SqlServerProvider.MicrosoftDataSqlClient)
  .UseBeforeConnectionOpened(cn =>
    {
        ((SqlConnection)cn).AccessToken = accessToken;
    });

// pass configured options to data context constructor
var dc = new DataContext(options);
```

> [!TIP]
> There are a lot of configuration methods on `DataOptions` you can use.
>
> [!TIP]
> It is recommended to create configured `DataOptions` instance once and use it everywhere. E.g. you can register it in your DI container.
>

### Using Config File (.NET Framework)

In your `web.config` or `app.config` make sure you have a connection string (check [this file](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/ProviderName.cs) for supported providers):

```xml
<connectionStrings>
  <add name="Northwind" 
    connectionString = "Server=.\;Database=Northwind;Trusted_Connection=True;" 
    providerName     = "SqlServer" />
</connectionStrings>
```

### Using Connection String Settings Provider

Alternatively, you can implement custom settings provider with `ILinqToDBSettings` interface, for example:

```cs
public class ConnectionStringSettings : IConnectionStringSettings
{
    public string ConnectionString { get; set; }
    public string Name             { get; set; }
    public string ProviderName     { get; set; }
    public bool   IsGlobal         => false;
}

public class MySettings : ILinqToDBSettings
{
    public IEnumerable<IDataProviderSettings> DataProviders
        => Enumerable.Empty<IDataProviderSettings>();

    public string DefaultConfiguration => "SqlServer";
    public string DefaultDataProvider  => "SqlServer";

    public IEnumerable<IConnectionStringSettings> ConnectionStrings
    {
        get
        {
            // note that you can return multiple ConnectionStringSettings instances here
            yield return
                new ConnectionStringSettings
                {
                    Name             = "Northwind",
                    ProviderName     = ProviderName.SqlServer,
                    ConnectionString =
                        @"Server=.\;Database=Northwind;Trusted_Connection=True;"
                };
        }
    }
}
```

And later just set on program startup before the first query is done (Startup.cs for example):

```cs
DataConnection.DefaultSettings = new MySettings();
```

### Use with ASP.NET Core

See [article](https://linq2db.github.io/articles/get-started/asp-dotnet-core/index.html).

## Define **POCO** class

You can generate POCO classes from your database using [linq2db.cli](https://www.nuget.org/packages/linq2db.cli)  `dotnet tool`.

Alternatively, you can write them manually and map to database using mapping attributes or `fluent mapping configuration`. Also you can use POCO classes as-is without additional mappings if they use same naming for classes and properties as table and column names in database.

### Configuration using mapping attributes

```c#
using System;
using LinqToDB.Mapping;

[Table("Products")]
public class Product
{
  [PrimaryKey, Identity]
  public int ProductID { get; set; }

  [Column("ProductName"), NotNull]
  public string Name { get; set; }

  [Column]
  public int VendorID { get; set; }

  [Association(ThisKey = nameof(VendorID), OtherKey = nameof(Vendor.ID))]
  public Vendor Vendor { get; set; }

  // ... other columns ...
}
```

This approach involves attributes on all properties that should be mapped. This way lets you to configure all possible things linq2db ever supports. There is one thing to mention: if you add at least one attribute into POCO, all other properties should also have attributes, otherwise they will be ignored:

```c#
using System;
using LinqToDB.Mapping;

[Table("Products")]
public class Product
{
  [PrimaryKey, Identity]
  public int ProductID { get; set; }

  // Property `Name` will be ignored as it lacks `Column` attibute.
  public string Name { get; set; }
}
```

### Fluent Configuration

This method lets you configure your mapping dynamically at runtime. Furthermore, it lets you to have several different configurations if you need so. You will get all configuration abilities available with attribute configuration. These two approaches are interchangeable in their abilities. This kind of configuration is done through the class `MappingSchema`.

With fluent approach you can configure only things that require it explicitly. All other properties will be inferred by Linq To DB:

```c#
// IMPORTANT: configure mapping schema instance only once
// and use it with all your connections that need those mappings
// Never create new mapping schema for each connection as
// it will seriously harm performance
var myFluentMappings = new MappingSchema();
var builder          = mappingSchema.GetFluentMappingBuilder();

builder.Entity<Product>()
    .HasTableName("Products")
    .HasSchemaName("dbo")
    .HasIdentity(x => x.ProductID)
    .HasPrimaryKey(x => x.ProductID)
    .Ignore(x => x.SomeNonDbProperty)
    .Property(x => x.TimeStamp)
        .HasSkipOnInsert()
        .HasSkipOnUpdate()
    .Association(x => x.Vendor, x => x.VendorID, x => x.VendorID, canBeNull: false)
    ;

//... other mapping configurations

// commit configured mappings to mapping schema
builder.Build();
```

In this example we configured only three properties and one association. We let Linq To DB to infer all other properties as columns with same name as property.

To use your `MappingSchema` instance you should pass it `DataConnection` or `DataContext` constructor:

```cs
var options = new DataOptions()
    .UseSqlServer(@"Server=.\;Database=Northwind;Trusted_Connection=True;")
    .UseMappingSchema(myFluentMappings);

var db = new DataConnection(option);
```

### Inferred Configuration

This approach involves no attributes at all. In this case Linq To DB will use POCO's name as table name and property names as column names (with exact same casing, which could be important for case-sensitive databases). This might seem to be convenient, but there are some restrictions:

- Linq To DB will not infer primary key even if class has property called `ID`;
- it will not infer nullability of reference types if you don't use nullable reference types annotations;
- associations will not be automatically configured.

```c#
using System;
using LinqToDB.Mapping;

public class Product
{
  public int    ProductID { get; set; }

  public string Name      { get; set; }

  public int    VendorID  { get; set; }

  public Vendor Vendor    { get; set; }

  // ... other columns ...
}
```

This way Linq To DB will auto-configure `Product` class to map to `Product` table with fields `ProductID`, `Name`, and `VendorID`. POCO will not get `ProductID` property treated as primary key. And there will be no association with `Vendor`.

This approach is not generally recommended.

### DataConnection class

At this point LINQ to DB doesn't know how to connect to our database or which POCOs go with what database. All this mapping is done through a `DataConnection` class:

```c#
public class DbNorthwind : LinqToDB.Data.DataConnection
{
  public DbNorthwind() : base("Northwind") { }

  public ITable<Product>  Product  => this.GetTable<Product>();
  public ITable<Category> Category => this.GetTable<Category>();

  // ... other tables ...
}
```

We call the base constructor with the "Northwind" parameter. This parameter (called `configuration name`) has to match the `name="Northwind"` we defined above as name of our connection string. We also added convenience properties for `Product` and `Category` mapping classes to write LINQ queries.

And now let's get some data:

```cs
using LinqToDB;
using LinqToDB.Common;

public static List<Product> GetProducts()
{
  using var db = new DbNorthwind();

  var query = from p in db.Product
                where p.ProductID > 25
                orderby p.Name descending
                select p;

  return query.ToList();
}
```

Make sure you **always** wrap your `DataConnection` class (in our case `DbNorthwind`) in a `using` statement. This is required for proper resource management, like releasing the database connections back into the pool ([more details](https://linq2db.github.io/articles/general/Managing-data-connection.html)).

## Queries

### Selecting Columns

Most times we get the entire row from the database:

```c#
from p in db.Product
where p.ProductID == 5
select p;
```

However, sometimes getting all the fields is too wasteful so we want only certain fields, but still use our POCOs; something that is challenging for libraries that rely on object tracking, like LINQ to SQL.

```c#
from p in db.Product
orderby p.Name descending
select new Product
{
  Name = p.Name
};
```

### Composing queries

Rather than concatenating strings we can 'compose' LINQ expressions.  In the example below the final SQL will be different if `onlyActive` is true or false, or if `searchFor` is not null.

```c#
public static Product[] GetProducts(bool onlyActive, string searchFor)
{
  using var db = new DbNorthwind();
  var products = from p in db.Product 
                   select p;

  if (onlyActive)
  {
    products = from p in products 
               where !p.Discontinued 
               select p;
  }

  if (searchFor != null)
  {
    products = from p in products 
                 where p.Name.Contains(searchFor) 
                 select p;
  }

  return products.ToArray();
}
```

### Paging

A lot of times we need to write code that returns only a subset of the entire dataset. We expand on the previous example to show what a product search function could look like.

Keep in mind that the code below will query the database twice. Once to find out the total number of records, something that is required by many paging controls, and once to return the actual data.

```c#
public static List<Product> Search(
                  string  searchFor,
                  int     currentPage,
                  int     pageSize,
                  out int totalRecords)
{
  using var db = new DbNorthwind();
  var products = from p in db.Product 
                   select p;

  if (searchFor != null)
  {
    products = from p in products 
               where p.Name.Contains(searchFor) 
               select p;
  }

  totalRecords = products.Count();

  return products.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();
}
```

### Joins

This assumes we added a `Category` class, just like we did with the `Product` class, defined all the fields, and defined table access property in our `DbNorthwind` data access class. We can now write an **INNER JOIN** query like this:

```c#
from p in db.Product
join c in db.Category on p.CategoryID equals c.CategoryID
select new Product
{
  Name = p.Name,
  Category = c
};
```

and a **LEFT JOIN** query like this:

```c#
from p in db.Product
from c in db.Category.Where(q => q.CategoryID == p.CategoryID).DefaultIfEmpty()
select new Product
{
  Name = p.Name,
  Category = c
};
```

[More samples are here](https://linq2db.github.io/articles/sql/Join-Operators.html)

### Creating your POCOs

In the previous example we assign an entire `Category` object to our product, but what if we want all the fields in our `Product` class, but we don't want to specify every field by hand? Unfortunately, we **cannot** write this:

```c#
from p in db.Product
from c in db.Category.Where(q => q.CategoryID == p.CategoryID).DefaultIfEmpty()
select new Product(c);
```

The query above assumes the Product class has a constructor that takes in a `Category` object. The query above won't work, but we **can** work around that with the following query:

```c#
from p in db.Product
from c in db.Category.Where(q => q.CategoryID == p.CategoryID).DefaultIfEmpty()
select Product.Build(p, c);
```

For this to work, we need a function in the `Product` class that looks like this:

```c#
public static Product Build(Product? product, Category category)
{
  if (product != null)
  {
    product.Category = category;
  }
  return product;
}
```

One caveat with this approach is that if you're using it with composed queries (see example above) the `select Build` part has to come only in the final select.

### Insert

At some point we will need to add a new `Product` to the database. One way would be to call the `Insert` extension method found in the `LinqToDB` namespace; so make sure you import that.

```c#
using LinqToDB;

using var db = new DbNorthwind();
db.Insert(product);
```

This inserts all the columns from our `Product` class, but without retrieving the generated identity value. To do that we can use `InsertWith*Identity` methods, like this:

```c#
using LinqToDB;

using var db = new DbNorthwind();
product.ProductID = db.InsertWithInt32Identity(product);
```

There is also `InsertOrReplace` that updates a database record if it was found by primary key or adds it otherwise.

If you need to insert only certain fields, or use values generated by the database, you could write:

```c#
using LinqToDB;

using var db = new DbNorthwind();
db.Product
  .Value(p => p.Name, product.Name)
  .Value(p => p.UnitPrice, 10.2m)
  .Value(p => p.Added, () => Sql.CurrentTimestamp)
  .Insert();
```

Use of this method also allows us to build insert statements like this:

```c#
using LinqToDB;

using var db = new DbNorthwind();
var statement = db.Product
                    .Value(p => p.Name, product.Name)
                    .Value(p => p.UnitPrice, 10.2m);

if (storeAdded) statement.Value(p => p.Added, () => Sql.CurrentTimestamp);

statement.Insert();
```

### Update

Updating records follows similar pattern to `Insert`. We have an extension method that updates all the columns in the database:

```c#
using LinqToDB;

using var db = new DbNorthwind();
db.Update(product);
```

And we also have a lower level update mechanism:

```c#
using LinqToDB;

using var db = new DbNorthwind();
db.Product
  .Where(p => p.ProductID == product.ProductID)
  .Set(p => p.Name, product.Name)
  .Set(p => p.UnitPrice, product.UnitPrice)
  .Update();
```

Similarly, we can break an update query into multiple pieces if needed:

```c#
using LinqToDB;

using var db = new DbNorthwind();
var statement = db.Product
                    .Where(p => p.ProductID == product.ProductID)
                    .Set(p => p.Name, product.Name);

if (updatePrice) statement = statement.Set(p => p.UnitPrice, product.UnitPrice);

statement.Update();
```

You're not limited to a single record update. For example, we could discontinue all the products that are no longer in stock:

```c#
using LinqToDB;

using var db = new DbNorthwind();
db.Product
  .Where(p => p.UnitsInStock == 0)
  .Set(p => p.Discontinued, true)
  .Update();
```

### Delete

Similar to how you update records, you can also delete records:

```c#
using LinqToDB;

using var db = new DbNorthwind();
db.Product
  .Where(p => p.Discontinued)
  .Delete();
```

### Bulk Copy

Bulk copy feature supports the transfer of large amounts of data into a table from another data source. For more details read this [article](https://linq2db.github.io/articles/sql/Bulk-Copy.html).

```c#
using LinqToDB.Data;

[Table("ProductsTemp")]
public class ProductTemp
{
  [PrimaryKey]
  public int ProductID { get; set; }

  [Column("ProductName"), NotNull]
  public string Name { get; set; }

  // ... other columns ...
}

var list = new List<ProductTemp>();

// ... populate list ...

using var db = new DbNorthwind();
db.BulkCopy(list);
```

### Transactions

Using database transactions is easy. All you have to do is call `BeginTransaction()` on your `DataConnection`, run one or more queries, and then commit the changes by calling `CommitTransaction()`. If something happened and you need to roll back your changes you can either call `RollbackTransaction()` or throw an exception.

```c#
using var db = new DbNorthwind();
db.BeginTransaction();
// or
//using var tr = db.BeginTransaction();
  
  // ... select / insert / update / delete ...

if (somethingIsNotRight)
{
  db.RollbackTransaction();
  // or
  // tr.Rollback();
}
else
{
  db.CommitTransaction();
  // or
  // tr.Commit();
}
```

Also, you can use .NET built-in `TransactionScope` class:

```c#
using var transaction = new TransactionScope();
// or for async code
// using var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled);
using var db = new DbNorthwind();
...
transaction.Complete();
```

It should be noted that there are two base classes for your "context" class: `LinqToDB.Data.DataConnection` and `LinqToDB.DataContext`. The key difference between them is in connection retention behaviour. `DataConnection` opens connection with first query and holds it open until dispose happens. `DataContext` behaves the way you might used to with Entity Framework: it opens connection per query and closes it right after query is done.

This difference in behavior matters when used with `TransactionScope`:

```c#
using var db = new LinqToDB.Data.DataConnection("provider name", "connection string");

var product = db.GetTable<Product>()
  .FirstOrDefault(); // connection opened here

var scope = new TransactionScope();
// this transaction was not attached to connection
// because it was opened earlier

product.Name = "Lollipop";
db.Update(product);

scope.Dispose();

// no transaction rollback happed, "Lollipop" has been saved
```

A `DataConnection` is attached with ambient transaction in moment it is opened. Any `TransactionScope`s created after the connection is created will no effect on that connection. Replacing `DataConnection` with `DataContext` in code shown earlier will make transaction scope work as expected: the created record will be discarded with the transaction.

Although, `DataContext` appears to be the right class to choose, it is strongly recommended to use `DataConnection` instead. It's default behaviour might be changed with setting `CloseAfterUse` property to `true`:

```c#
public class DbNorthwind : LinqToDB.Data.DataConnection
{
  public DbNorthwind() : base("Northwind")
  {
    (this as IDataContext).CloseAfterUse = true;
  }
}
```

### Merge

[Here](https://linq2db.github.io/articles/sql/merge/Merge-API.html) you can read about SQL MERGE support.

### Window (Analytic) Functions

[Here](https://linq2db.github.io/articles/sql/Window-Functions-%28Analytic-Functions%29.html) you can read about Window (Analytic) Functions support.

## MiniProfiler

If you would like to use [MiniProfiler](https://github.com/MiniProfiler/dotnet) or other profiling tool that wraps ADO.NET provider classes, you need to configure our regular `DataConnection` to use wrapped connection.

```c#
// example of SQL Server-backed data connection with MiniProfiler enabled for debug builds
public class DbDataContext : DataConnection
{
// let's use profiler only for debug builds
#if !DEBUG

  // regular non-profiled constructor
  public DbDataContext() : base("Northwind") {}
  
#else
  public DbDataContext()
      : base(
          new DataOptions()
            .UseSqlServer(connectionString, SqlServerVersion.v2012)
            .UseConnectionFactory(GetConnection)
            .UseInterceptor(new UnwrapProfilerInterceptor()))
  {
  }

  // wrap connection into profiler wrapper
  private static DbConnection GetConnection(DataOptions options)
  {
     // create provider-specific connection instance. SqlConnection in our case
     var dbConnection = new SqlConnection(options.ConnectionOptions.ConnectionString);

     // wrap it by profiler's connection implementation
     return new StackExchange.Profiling.Data.ProfiledDbConnection(
                                                 dbConnection,
                                                 MiniProfiler.Current);
  }

  // define UnwrapDataObjectInterceptor
  sealed class UnwrapProfilerInterceptor : UnwrapDataObjectInterceptor
  {
    public override DbConnection UnwrapConnection(IDataContext dataContext, DbConnection connection)
    {
      return connection is ProfiledDbConnection c ? c.WrappedConnection : connection;
    }

    public override DbTransaction UnwrapTransaction(IDataContext dataContext, DbTransaction transaction)
    {
       return transaction is ProfiledDbTransaction t ? t.WrappedTransaction : transaction;
    }

    public override DbCommand UnwrapCommand(IDataContext dataContext, DbCommand command)
    {
      return command is ProfiledDbCommand c ? c.InternalCommand : command;
    }

    public override DbDataReader UnwrapDataReader(IDataContext dataContext, DbDataReader dataReader)
    {
      return dataReader is ProfiledDbDataReader dr ? dr.WrappedReader : dataReader;
    }
  }
#endif
}
```

## More

Still have questions left? Check out our [documentation site](https://linq2db.github.io) and [FAQ](https://linq2db.github.io/articles/FAQ.html)
