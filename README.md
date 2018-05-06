# LINQ to DB

LINQ to DB is the fastest LINQ database access library offering a simple, light, fast, and type-safe layer between your POCO objects and your database. 

Architecturally it is one step above micro-ORMs like Dapper, Massive, or PetaPoco, in that you work with LINQ expressions, not with magic strings, while maintaining a thin abstraction layer between your code and the database. Your queries are checked by the C# compiler and allow for easy refactoring.

However, it's not as heavy as LINQ to SQL or Entity Framework. There is no change-tracking, so you have to manage that yourself, but on the positive side you get more control and faster access to your data.

In other words **LINQ to DB is type-safe SQL**.

Visit our [blog](http://blog.linq2db.com/) and see [Github.io documentation](https://linq2db.github.io/index.html) for more details.

Code examples and demos can be found [here](https://github.com/linq2db/examples) or in [tests](https://github.com/linq2db/linq2db/tree/master/Tests/Linq).

T4 model generation help is [here](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB.Templates/README.md).

## How to help the project

No, this is not the donate link. We do need something really more valuable - your **time**. If you really want to help us please read this [post](https://linq2db.github.io/articles/How-can-i-help.html).

## Project Build Status

--------------------
| |Appveyor|Travis
-----|-------|--------
|master|[![Build status](https://ci.appveyor.com/api/projects/status/4au5v7xm5gi19o8m/branch/master?svg=true)](https://ci.appveyor.com/project/igor-tkachev/linq2db/branch/master)|[![Build Status](https://travis-ci.org/linq2db/linq2db.svg?branch=master)](https://travis-ci.org/linq2db/linq2db)
|latest|[![Build status](https://ci.appveyor.com/api/projects/status/4au5v7xm5gi19o8m?svg=true)](https://ci.appveyor.com/project/igor-tkachev/linq2db)| |

## Feeds

* NuGet [![NuGet](https://img.shields.io/nuget/vpre/linq2db.svg)](https://www.nuget.org/packages?q=linq2db)
* MyGet [![MyGet](https://img.shields.io/myget/linq2db/vpre/linq2db.svg)](https://www.myget.org/gallery/linq2db)
  * V2 `https://www.myget.org/F/linq2db/api/v2`
  * V3 `https://www.myget.org/F/linq2db/api/v3/index.json`

## Let's get started

From **NuGet**:
* `Install-Package linq2db` - .NET
* `Install-Package linq2db.core` - .NET Core (for versions prior to 2.0)

## Configuring connection strings

### .NET

In your `web.config` or `app.config` make sure you have a connection string (check [this file](https://github.com/linq2db/linq2db/blob/master/Source/LinqToDB/ProviderName.cs) for supported providers):

```xml
<connectionStrings>
  <add name="Northwind" 
    connectionString = "Server=.\;Database=Northwind;Trusted_Connection=True;Enlist=False;" 
    providerName     = "SqlServer" />
</connectionStrings>
```

### .NET Core

.Net Core does not support `System.Configuration` so to configure connection strings you should implement `ILinqToDBSettings`, for example:

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
                    Name = "SqlServer",
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

You can also use same for regular .NET.

## Now let's create a **POCO** class

Important: you also can generate those classes from your database using [T4 templates](https://github.com/linq2db/linq2db/tree/master/Source/LinqToDB.Templates#t4-models). Demonstration video could be found [here](https://github.com/linq2db/linq2db/wiki).

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

Make sure you **always** wrap your `DataConnection` class (in our case `DbNorthwind`) in a `using` statement. This is required for proper resource management, like releasing the database connections back into the pool. [More details](https://linq2db.github.io/articles/Managing-data-connection.html)

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

[More samples are here](https://linq2db.github.io/articles/Join-Operators.html)

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
using (var db = new DbNorthwind())
{
  db.Insert(product);
}
```

This inserts all the columns from our Product class, but without retrieving the generated identity value. To do that we can use `InsertWith*Identity` methods, like this:

```c#
using (var db = new DbNorthwind())
{
  product.ProductID = db.InsertWithInt32Identity(product);
}
```

There is also `InsertOrReplace` that updates a database record if it was found by primary key or adds it otherwise.

If you need to insert only certain fields, or use values generated by the database, you could write:

```c#
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
using (var db = new DbNorthwind())
{
  db.Update(product);
}
```

And we also have a lower level update mechanism:

```c#
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
using (var db = new DbNorthwind())
{
  db.Product
    .Where(p => p.Discontinued)
    .Delete();
}
```

## Bulk Copy

Bulk copy feature supports the transfer of large amounts of data into a table from another data source. For faster data inserting DO NOT use a transaction. If you use a transaction an adhoc implementation of the bulk copy feature has been added in order to insert multiple lines at once. You get faster results then inserting lines one by one, but it's still slower than the database provider bulk copy. So, DO NOT use transactions whenever you can (Take care of unique constraints, primary keys, etc. since bulk copy ignores them at insertion).

```c#
[Table(Name = "ProductsTemp")]
public class ProductTemp
{
  public int ProductID { get; set; }

  [Column(Name = "ProductName"), NotNull]
  public string Name { get; set; }

  // ... other columns ...
}

list = List<ProductTemp>

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
{
  using (var db = new DbNorthwind())
  {
    ...
  }
  transaction.Complete();
}
```

## Merge

[Here](https://linq2db.github.io/articles/Merge-API.html) you can read about MERGE support.

## Window (Analytic) Functions

[Here](https://linq2db.github.io/articles/Window-Functions-%28Analytic-Functions%29.html) you can read about Window (Analytic) Functions support.

## MiniProfiler

If you would like to use MiniProfiler from StackExchange you'd need to wrap ProfiledDbConnection around our regular DataConnection.

```c#
public class DbDataContext : DataConnection
{
#if !DEBUG
  public DbDataContext() : base("Northwind") { }
#else
  public DbDataContext() : base(GetDataProvider(), GetConnection()) { }

  private static IDataProvider GetDataProvider()
  {
    // you can move this line to other place, but it should be
    // allways set before LINQ to DB provider instance creation
    LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = true;

    return new SqlServerDataProvider("", SqlServerVersion.v2012);
  }

  private static IDbConnection GetConnection()
  {
    var dbConnection = new SqlConnection(@"Server=.\SQL;Database=Northwind;Trusted_Connection=True;Enlist=False;");
    return new StackExchange.Profiling.Data.ProfiledDbConnection(dbConnection, MiniProfiler.Current);
  }
#endif
}
```

This assumes that you only want to use MiniProfiler while in DEBUG mode and that you are using SQL Server for your database. If you're using a different database you would need to change GetDataProvider() to return the appropriate IDataProvider. For example for MySql you would use:

```c#
private static IDataProvider GetDataProvider()
{
  return new LinqToDB.DataProvider.MySql.MySqlDataProvider();
}
```
