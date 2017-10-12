# Managing data connection

.NET database providers use connection pooling to work with database connections, where they take connection from pool, use it, and then release connection back to connection pool so it could be reused. When connection is not released correctly after use, connection pool will consider it still used, which will lead to two consequesnces:
- your application will create more and more connections to database, because there are no _free_ connections to reuse from connection pool manager point of view
- at some point your application will fail to obtain connection from pool, because pool size limit reached

To avoid collection leaks you should care about how you are creating and disposing connections. There are to ways to query database with linq2db:
* using `DataConnection` class. Using `DataConnection` you can make several queries in one physical database connection, so you do not have overhead on opening and closing database connection. You should follow few simple rules:
  * **always** dispose `DataConnection` instance (it is recommended to use `using` c# statement);
  * query should be executed **before** `DataConnection` object is disposed. From version 1.8.0 we have introduced protection from wrong usage, and you will get `ObjectDisposedException` trying to perform query on disposed `DataConnection` instance.
* using `DataContext` class. `DataContext` opens and closes physical connection for **each** query! 
   * Be careful with `DataContext.KeepConnectionAlive` property, if you set it `true`, it would work the same way as `DataConnection`! So we do not recommend you to set this property to `true`.

## Done right
```cs
using (var db = new DataConnection())
{
// your code here
}

public IEnumerable<Person> GetPersons()
{
    using (var db = new DataConnection())
    {
        // ToList call sends query to database while we are still in using
        return db.GetTable<Person>().ToList();
    }
}

public IEnumerable<Person> GetPersons()
{
    // ToList call sends query to database and DataContext releases connection
    return new DataContext().GetTable<Person>().ToList();
}

public IQuerable<Person> GetPersons()
{
    // query is not sent to database here
    // it will be executed later when user will enumerate results of method
    // but DataContext will handle it properly
    return new DataContext().GetTable<Person>();
}

public async Task<IEnumerable<Person>> GetPersons()
{
    using (var db = new DataConnection())
    {
        // await will suspend execution inside of using waiting for query results from ToListAsync()
        // after that execution will continue and dispose `DataConnection` instance
        return await db.GetTable<Person>().ToListAsync(); 
    }
}

```

## Done wrong
```cs
public IEnumerable<Person> GetPersons()
{
    using (var db = new DataConnection())
    {
        // query will be executed only when user will enumerate method results
        return db.GetTable<Person>();
    }
}

// DataConnection already disposed here
// starting from linq2db 1.8.0 it will fail with ObjectDisposedException
// versions prior to 1.8.0 will execute query (if there are free connectons left) and will create leaked connection
var persons = GetPersons().ToList();
```

```cs
public async Task<IEnumerable<Person>> GetPersons()
{
    using (var db = new DataConnection())
    {
        // no suspension point here, awaitable task will be returned immediately from method
        // creating race conditions
        return db.GetTable<Person>().ToListAsync();
    }
}

// query execution will be called on disposed DataConnection
var persons = await GetPersons();
```
