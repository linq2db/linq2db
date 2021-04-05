## LINQ to DB

<a href="https://dotnetfoundation.org/projects/linq2db">
<img align="right" alt=".NET Foundation Logo" src="https://github.com/dotnet/swag/blob/master/logo/dotnetfoundation_v4_horizontal.png" width="250px" ></a>

[![NuGet Version and Downloads count](https://buildstats.info/nuget/linq2db?includePreReleases=true)](https://www.nuget.org/profiles/LinqToDB) [![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

[![Master branch build](https://img.shields.io/azure-devops/build/linq2db/linq2db/5/master?label=build%20(master))](https://dev.azure.com/linq2db/linq2db/_build?definitionId=5&_a=summary) [![Latest build](https://img.shields.io/azure-devops/build/linq2db/linq2db/5?label=build%20(latest))](https://dev.azure.com/linq2db/linq2db/_build?definitionId=5&_a=summary)

[![StackOverflow questions](https://img.shields.io/stackexchange/stackoverflow/t/linq2db.svg?label=stackoverflow)](https://stackoverflow.com/questions/tagged/linq2db) [![Follow @linq2db](https://img.shields.io/twitter/follow/linq2db.svg)](https://twitter.com/linq2db) [!["good first issue" tasks](https://img.shields.io/github/issues/linq2db/linq2db/good%20first%20issue.svg)](https://github.com/linq2db/linq2db/issues?q=is%3Aopen+is%3Aissue+label%3A%22good+first+issue%22)

LINQ to DB is the fastest LINQ database access library offering a simple, light, fast, and type-safe layer between your POCO objects and your database. 

Architecturally it is one step above micro-ORMs like Dapper, Massive, or PetaPoco, in that you work with LINQ expressions, not with magic strings, while maintaining a thin abstraction layer between your code and the database. Your queries are checked by the C# compiler and allow for easy refactoring.

However, it's not as heavy as LINQ to SQL or Entity Framework. There is no change-tracking, so you have to manage that yourself, but on the positive side you get more control and faster access to your data.

In other words **LINQ to DB is type-safe SQL**.

linq2db is a [.NET Foundation](https://dotnetfoundation.org/) project.

Development version nuget [feed](https://pkgs.dev.azure.com/linq2db/linq2db/_packaging/linq2db/nuget/v3/index.json) ([how to use](https://docs.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio#package-sources))


## Standout Features

 - Rich Querying API:
   - [Explicit Join Syntax](https://linq2db.github.io/articles/sql/Join-Operators.html) (In addition to standard LINQ join syntax.)
   - [CTE Support](https://linq2db.github.io/articles/sql/CTE.html)
   - [Bulk Copy/Insert](https://linq2db.github.io/articles/sql/Bulk-Copy.html)
   - [Window/Analytic Functions](https://linq2db.github.io/articles/sql/Window-Functions-%28Analytic-Functions%29.html)
   - [Merge API](https://linq2db.github.io/articles/sql/merge/Merge-API-Description.html)
 - Extensibility:
   - [Ability to Map Custom SQL to Static Functions](https://github.com/linq2db/linq2db/tree/master/Source/LinqToDB/Sql/)

Visit our [blog](http://blog.linq2db.com/) and see [Github.io documentation](https://linq2db.github.io/index.html) for more details.

Code examples and demos can be found [here](https://github.com/linq2db/examples) or in [tests](https://github.com/linq2db/linq2db/tree/master/Tests/Linq).

[Release Notes](https://github.com/linq2db/linq2db/wiki/Releases-and-Roadmap) page.

### Related linq2db and 3rd-party projects
- [linq2db.EntityFrameworkCore](https://github.com/linq2db/linq2db.EntityFrameworkCore) (adds support for linq2db functionality in EF.Core projects)
- [LINQPad Driver](https://github.com/linq2db/linq2db.LINQPad)
- [DB2 iSeries Provider](https://github.com/LinqToDB4iSeries/Linq2DB4iSeries)
- [ASP.NET CORE 2 Template](https://github.com/David-Mawer/LINQ2DB-MVC-Core-2/tree/master/LINQ2DB-MVC-Core-2)
- [ASP.NET CORE 3 Template with Angular](https://github.com/David-Mawer/LINQ2DB-AngularWebApp-Core-3)
- [ASP.NET CORE 5 Template](https://github.com/David-Mawer/LINQ2DB-MVC-Core-5)
- [PostGIS extensions for linq2db](https://github.com/apdevelop/linq2db-postgis-extensions)


Notable open-source users:
- [nopCommerce](https://github.com/nopSolutions/nopCommerce) (starting from v4.30) - popular open-source e-commerce solution
- [OdataToEntity](https://github.com/voronov-maxim/OdataToEntity) - library to create OData service from database context
- [SunEngine](https://github.com/sunengine/SunEngine) - site, blog and forum engine

Unmantained projects:
- [LinqToDB.Identity](https://github.com/linq2db/LinqToDB.Identity) - ASP.NET Core Identity provider using linq2db
- [IdentityServer4.LinqToDB](https://github.com/linq2db/IdentityServer4.LinqToDB) - IdentityServer4 persistence layer using linq2db


## How to help the project

No, this is not the donate link. We do need something really more valuable - your **time**. If you really want to help us please read this [post](https://linq2db.github.io/articles/project/How-can-i-help.html).

## Let's get started

From **NuGet**:
* `Install-Package linq2db`

## Configuring connection strings


### Using Connection Options Builder

You can configure connection options from code using [`LinqToDbConnectionOptionsBuilder`](https://linq2db.github.io/api/LinqToDB.Configuration.LinqToDbConnectionOptionsBuilder.html) class (check class for available options):

```cs
// create options builder
var builder = new LinqToDbConnectionOptionsBuilder();

// configure connection string
builder.UseSqlServer(connectionString);

// or using custom connection factory
b.UseConnectionFactory(
    SqlServerTools.GetDataProvider(
        SqlServerVersion.v2017,
        SqlServerProvider.MicrosoftDataSqlClient),
    () =>
    {
        var cn = new SqlConnection(connectionString);
        cn.AccessToken = accessToken;
        return cn;
    });

// pass configured options to data connection constructor
var dc = new DataConnection(builder.Build());
```


### Using Config File (.NET Framework)

In your `web.config` or `app.config` make sure you have a connection string (check [this file](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/ProviderName.cs) for supported providers):

```xml
<connectionStrings>
  <add name="Northwind" 
    connectionString = "Server=.\;Database=Northwind;Trusted_Connection=True;Enlist=False;" 
    providerName     = "SqlServer" />
</connectionStrings>
```

### Using Connection String Settings Provider

.Net Core does not support `System.Configuration` until 3.0 so to configure connection strings you should implement `ILinqToDBSettings`, for example:

```cs
public class ConnectionStringSettings : IConnectionStringSettings
{
    public string ConnectionString { get; set; }
    public string Name { get; set; }
    public string ProviderName { get; set; }
    public bool IsGlobal => false;
}

public class MySettings : ILinqToDBSettings
{
    public IEnumerable<IDataProviderSettings> DataProviders => Enumerable.Empty<IDataProviderSettings>();

    public string DefaultConfiguration => "SqlServer";
    public string DefaultDataProvider => "SqlServer";

    public IEnumerable<IConnectionStringSettings> ConnectionStrings
    {
        get
        {
            yield return
                new ConnectionStringSettings
                {
                    Name = "Northwind",
                    ProviderName = "SqlServer",
                    ConnectionString = @"Server=.\;Database=Northwind;Trusted_Connection=True;Enlist=False;"
                };
        }
    }
}

```

And later just set on program startup before the first query is done (Startup.cs for example):

```cs
DataConnection.DefaultSettings = new MySettings();
```

### ASP.NET Core

See [article](https://linq2db.github.io/articles/get-started/asp-dotnet-core/index.html).

## Now let's create a **POCO** class

Important: you also can generate those classes from your database using [T4 templates](https://linq2db.github.io/articles/T4.html). Demonstration video could be found [here](https://linq2db.github.io/articles/general/Video.html).

```c#
using System;
using LinqToDB.Mapping;

[Table(Name = "Products")]
public class Product
{
  [PrimaryKey, Identity]
  public int ProductID { get; set; }

  [Column(Name = "ProductName"), NotNull]
  public string Name { get; set; }

  // ... other columns ...
}
```

At this point LINQ to DB doesn't know how to connect to our database or which POCOs go with what database. All this mapping is done through a `DataConnection` class:

```c#
public class DbNorthwind : LinqToDB.Data.DataConnection
{
  public DbNorthwind() : base("Northwind") { }

  public ITable<Product> Product => GetTable<Product>();
  public ITable<Category> Category => GetTable<Category>();

  // ... other tables ...
}
```

We call the base constructor with the "Northwind" parameter. This parameter (called `configuration name`) has to match the name="Northwind" we defined above in our connection string. We also have to register our `Product` class we defined above to allow us to write LINQ queries.

And now let's get some data:

```c#
using LinqToDB;
using LinqToDB.Common;

public static List<Product> All()
{
  using (var db = new DbNorthwind())
  {
    var query = from p in db.Product
                where p.ProductID > 25
                orderby p.Name descending
                select p;
    return query.ToList();
  }
}
```

Make sure you **always** wrap your `DataConnection` class (in our case `DbNorthwind`) in a `using` statement. This is required for proper resource management, like releasing the database connections back into the pool. [More details](https://linq2db.github.io/articles/general/Managing-data-connection.html)

## Selecting Columns

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

## Composing queries

Rather than concatenating strings we can 'compose' LINQ expressions.  In the example below the final SQL will be different if `onlyActive` is true or false, or if `searchFor` is not null.

```c#
public static List<Product> All(bool onlyActive, string searchFor)
{
  using (var db = new DbNorthwind())
  {
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

    return products.ToList();
  }
}
```

## Paging

A lot of times we need to write code that returns only a subset of the entire dataset. We expand on the previous example to show what a product search function could look like. 

Keep in mind that the code below will query the database twice. Once to find out the total number of records, something that is required by many paging controls, and once to return the actual data.

```c#
public static List<Product> Search(string searchFor, int currentPage, int pageSize, out int totalRecords)
{
  using (var db = new DbNorthwind())
  {
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
}
```

## Joins

This assumes we added a `Category` class, just like we did with the `Product` class, defined all the fields, and registered it in our `DbNorthwind` data access class. We can now write an **INNER JOIN** query like this:

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

## Creating your POCOs

In the previous example we assign an entire `Category` object to our product, but what if we want all the fields in our `Product` class, but we don't want to specify every field by hand? Unfortunately, we **cannot** write this:

```c#
from p in db.Product
from c in db.Category.Where(q => q.CategoryID == p.CategoryID).DefaultIfEmpty()
select new Product(c);
```

The query above assumes the Product class has a constructor that takes in a Category object. The query above won't work, but we **can** work around that with the following query:

```c#
from p in db.Product
from c in db.Category.Where(q => q.CategoryID == p.CategoryID).DefaultIfEmpty()
select Product.Build(p, c);
```

For this to work, we need a function in the `Product` class that looks like this:

```c#
public static Product Build(Product product, Category category)
{
  if (product != null)
  {
    product.Category = category;
  }
  return product;
}
```

One caveat with this approach is that if you're using it with composed queries (see example above) the select Build part has to come only in the final select.

## Insert

At some point we will need to add a new `Product` to the database. One way would be to call the `Insert` extension method found in the `LinqToDB` namespace; so make sure you import that.

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  db.Insert(product);
}
```

This inserts all the columns from our Product class, but without retrieving the generated identity value. To do that we can use `InsertWith*Identity` methods, like this:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  product.ProductID = db.InsertWithInt32Identity(product);
}
```

There is also `InsertOrReplace` that updates a database record if it was found by primary key or adds it otherwise.

If you need to insert only certain fields, or use values generated by the database, you could write:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  db.Product
    .Value(p => p.Name, product.Name)
    .Value(p => p.UnitPrice, 10.2m)
    .Value(p => p.Added, () => Sql.CurrentTimestamp)
    .Insert();
}
```

Use of this method also allows us to build insert statements like this:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  var statement = db.Product
                    .Value(p => p.Name, product.Name)
                    .Value(p => p.UnitPrice, 10.2m);

  if (storeAdded) statement.Value(p => p.Added, () => Sql.CurrentTimestamp);

  statement.Insert();
}
```

## Update

Updating records follows similar pattern to Insert. We have an extension method that updates all the columns in the database:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  db.Update(product);
}
```

And we also have a lower level update mechanism:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  db.Product
    .Where(p => p.ProductID == product.ProductID)
    .Set(p => p.Name, product.Name)
    .Set(p => p.UnitPrice, product.UnitPrice)
    .Update();
}
```

Similarly, we can break an update query into multiple pieces if needed:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  var statement = db.Product
                    .Where(p => p.ProductID == product.ProductID)
                    .Set(p => p.Name, product.Name);

  if (updatePrice) statement = statement.Set(p => p.UnitPrice, product.UnitPrice);

  statement.Update();
}
```

You're not limited to updating a single record. For example, we could discontinue all the products that are no longer in stock:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  db.Product
    .Where(p => p.UnitsInStock == 0)
    .Set(p => p.Discontinued, true)
    .Update();
}
```

## Delete

Similar to how you update records, you can also delete records:

```c#
using LinqToDB;

using (var db = new DbNorthwind())
{
  db.Product
    .Where(p => p.Discontinued)
    .Delete();
}
```

## Bulk Copy

Bulk copy feature supports the transfer of large amounts of data into a table from another data source. For more details read this [article](https://linq2db.github.io/articles/sql/Bulk-Copy.html).

```c#
using LinqToDB.Data;

[Table(Name = "ProductsTemp")]
public class ProductTemp
{
  [PrimaryKey]
  public int ProductID { get; set; }

  [Column(Name = "ProductName"), NotNull]
  public string Name { get; set; }

  // ... other columns ...
}

var list = new List<ProductTemp>();
// populate list

using (var db = new DbNorthwind())
{
  db.BulkCopy(list);
}
```

## Transactions

Using database transactions is easy. All you have to do is call BeginTransaction() on your DataConnection, run one or more queries, and then commit the changes by calling CommitTransaction(). If something happened and you need to roll back your changes you can either call RollbackTransaction() or throw an exception.

```c#
using (var db = new DbNorthwind())
{
  db.BeginTransaction();
  
  // ... select / insert / update / delete ...

  if (somethingIsNotRight)
  {
    db.RollbackTransaction();
  }
  else
  {
    db.CommitTransaction();
  }
}
```

Also, you can use .NET built-in TransactionScope class:

```c#
// don't forget that isolation level is serializable by default
using (var transaction = new TransactionScope())
// or for async code
// using (var transaction = new TransactionScope(TransactionScopeAsyncFlowOption.Enabled))
{
  using (var db = new DbNorthwind())
  {
    ...
  }
  transaction.Complete();
}
```

## Merge

[Here](https://linq2db.github.io/articles/sql/merge/Merge-API.html) you can read about MERGE support.

## Window (Analytic) Functions

[Here](https://linq2db.github.io/articles/sql/Window-Functions-%28Analytic-Functions%29.html) you can read about Window (Analytic) Functions support.

## MiniProfiler

If you would like to use [MiniProfiler](https://github.com/MiniProfiler/dotnet) or other profiling tool that wraps ADO.NET provider classes, you need to configure our regular DataConnection to use wrapped connection.

```c#
// example of SQL Server-backed data connection with MiniProfiler enabled for debug builds
public class DbDataContext : DataConnection
{
// let's use profiler only for debug builds
#if !DEBUG
  public DbDataContext() : base("Northwind")
  {
    // this is important part:
    // here we tell linq2db how to access underlying ADO.NET classes of used provider
    // if you don't configure those mappings, linq2db will be unable to use provider-specific functionality
    // which could lead to loss or unavailability of some functionality when profiled connection enabled
    MappingSchema.SetConvertExpression<ProfiledDbConnection,  DbConnection> (db => db.WrappedConnection);
    MappingSchema.SetConvertExpression<ProfiledDbDataReader,  DbDataReader> (db => db.WrappedReader);
    MappingSchema.SetConvertExpression<ProfiledDbTransaction, DbTransaction>(db => db.WrappedTransaction);
    MappingSchema.SetConvertExpression<ProfiledDbCommand,     DbCommand>    (db => db.InternalCommand);
  }
#else
  public DbDataContext() : base(GetDataProvider(), GetConnection()) { }

  private static IDataProvider GetDataProvider()
  {
     // create provider instance (SQL Server 2012 provider in our case)
     return new SqlServerDataProvider("", SqlServerVersion.v2012);
  }

  private static DbConnection GetConnection()
  {
     // create provider-specific connection instance. SqlConnection in our case
     var dbConnection = new SqlConnection(@"Server=.\SQL;Database=Northwind;Trusted_Connection=True;Enlist=False;");

     // wrap it by profiler's connection implementation
     return new StackExchange.Profiling.Data.ProfiledDbConnection(dbConnection, MiniProfiler.Current);
  }
#endif
}
```

# More
Still have questions left? Check out our [documentation site](https://linq2db.github.io) and [FAQ](https://linq2db.github.io/articles/FAQ.html)
