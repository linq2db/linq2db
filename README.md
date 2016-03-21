LINQ to DB
=========

LINQ to DB is the fastest LINQ database access library offering a simple, light, fast, and type-safe layer between your POCO objects and your database. 

Architecturally it is one step above micro-ORMs like Dapper, Massive, or PetaPoco, in that you work with LINQ expressions, not with magic strings, while maintaining a thin abstraction layer between your code and the database. Your queries are checked by the C# compiler and allow for easy refactoring.

However, it's not as heavy as LINQ to SQL or Entity Framework. There is no change-tracking, so you have to manage that yourself, but on the plus side you get more control and faster access to your data.

See [Wiki](https://github.com/linq2db/linq2db/wiki) for more details.

Code examples and demos can be found [here] (https://github.com/linq2db/examples).

Let's get started
-----------------

From **NuGet**: `Install-Package linq2db`

In your `web.config` or `app.config` make sure you have a connection string:

```xml
<connectionStrings>
  <add name="Northwind" 
    connectionString = "Server=.\;Database=Northwind;Trusted_Connection=True;Enlist=False;" 
    providerName     = "SqlServer" />
</connectionStrings>
```

Now let's create a **POCO** class:

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

  public ITable<Product> Product { get { return GetTable<Product>(); } }
  public ITable<Category> Category { get { return GetTable<Category>(); } }
  
  // ... other tables ...
}
```

We call the base constructor with the "Northwind" parameter. This parameter has to match the name="Northwind" we defined above in our connection string. We also have to register our `Product` class we defined above to allows us to write LINQ queries.

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

Make sure you **always** wrap your `DataConnection` class (in our case `DbNorthwind`) in a `using` statement. This is required for proper resource management, like releasing the database connections back into the pool.

Selecting Columns
-----------------

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

Composing queries
------

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

Paging
-----

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

Joins
-----

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

Creating your POCOs
----------------

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

Insert
------

At some point we will need to add a new `Product` to the database. One way would be to call the `Insert` extension method found in the `LinqToDB` namespace; so make sure you import that.

```c#
using (var db = new DbNorthwind())
{
  db.Insert(product);
}
```

This inserts all the columns from our Product class, but without retrieving the generated identity value. To do that we can use `InsertWithIdentity`, like this:

```c#
using (var db = new DbNorthwind())
{
  product.ProductID = Convert.ToInt32(db.InsertWithIdentity(product));
}
```

We need to convert the returned value to an integer since an identity field could be something other than an integer, like a GUID for example. There is also `InsertOrReplace` that updates a database record if found or adds it otherwise.

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

Using this method also allows us to build insert statements like this:

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

Update
------

Updating records follows a similar pattern to Insert. We have an extension method that updates all the columns in the database:

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

You're not limited to updating a single field. For example, we could discontinue all the products that are no longer in stock:

```c#
using (var db = new DbNorthwind())
{
  db.Product
    .Where(p => p.UnitsInStock == 0)
    .Set(p => p.Discontinued, true)
    .Update();
}
```

Delete
------

Similar to how you update records, you can also delete records:

```c#
using (var db = new DbNorthwind())
{
  db.Product
    .Where(p => p.Discontinued)
    .Delete();
}
```

Bulk Copy
------

Bulk copy feature supports the transfer of large amounts of data into a table from another data source. For faster data inserting DO NOT use a transaction. If you use a transaction an adhoc implementation of the bulk copy feature has been added in order to insert multiple lines at once. You get faster results then inserting lines one by one, but it's still slower than the database provider bulk copy. So, DO NOT use transactions whenever you can (Take care of unicity constraints, primary keys, etc. since bulk copy ignores them at insertion)

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

Transactions
------

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
// don't forget isolation level is serializable by default
using (var transaction = new TransactionScope()) 
{
  using (var db = new DbNorthwind())
  {
    ...
  }
  transaction.Complete();
}
```

MiniProfiler
------

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
    return new SqlServerDataProvider("", SqlServerVersion.v2012);
  }

  private static IDbConnection GetConnection()
  {
    LinqToDB.Common.Configuration.AvoidSpecificDataProviderAPI = true;

    var dbConnection = new SqlConnection(@"Server=.\SQL;Database=Northwind;Trusted_Connection=True;Enlist=False;");
    return new StackExchange.Profiling.Data.ProfiledDbConnection(dbConnection, MiniProfiler.Current);
  }
#endif
}
```

This assumes that you only want to use MiniProfiler while in DEBUG mode and that you are using SQL Server for your database. If you're using a different database you would need to change GetDataProvider() to return the appropriate IDataProvider. For example, if using MySql you would use:

```c#
private static IDataProvider GetDataProvider()
{
  return new LinqToDB.DataProvider.MySql.MySqlDataProvider();
}
```        
