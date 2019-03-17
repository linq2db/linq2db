# Managing Data Connection

Since connecting to a database is an expensive operation, .NET database providers use connection pools to minimize this cost. They take a connection from the pool, use it, and then release the connection back to the pool so it could be reused.

If you don't release database connections back to the pool then:

* Your application would create more and more connections to the database. This is because from connection pool's point of view there are no _available_ connections to reuse.
* When the pool size limit is reached then your application would fail to obtain a new connection.

To avoid connection leaks you should pay attention to how you are creating and disposing connections. There are to ways to query a database with linq2db:

1. Using the `DataConnection` class you can make several queries using a single database connection. This way you do not have the overhead of opening and closing database connections for each query. You should follow these simple rules:
    * **Always** dispose the `DataConnection` instance. We recommend the C#'s `using` block.
    * Your query should be executed **before** the `DataConnection` object is disposed. Starting with version 1.8.0 we have added checks to catch improper usages; you would get `ObjectDisposedException` when trying to perform a query on a disposed `DataConnection` instance.
2. Using the `DataContext` class opens and closes an actual connection for **each** query!
    * Be careful with the `DataContext.KeepConnectionAlive` property. If you set it to `true` it would work the same way as `DataConnection`! So we do not recommend that you to set this property to `true`.

## Done Right

```cs
using (var db = new DataConnection())
{
    // your code here
}

public IEnumerable<Person> GetPersons()
{
    using (var db = new DataConnection())
    {
        // The ToList call sends the query to the database while we are still in the using block
        return db.GetTable<Person>().ToList();
    }
}

public IEnumerable<Person> GetPersons()
{
    // The ToList call sends the query to the database and then DataContext releases the connection
    return new DataContext().GetTable<Person>().ToList();
}

public IQuerable<Person> GetPersons()
{
    // If this example the query is not sent to the database. It will be executed later
    // when we enumerate IQuerable. DataContext will handle the connection release properly.
    return new DataContext().GetTable<Person>();
}

public async Task<IEnumerable<Person>> GetPersons()
{
    using (var db = new DataConnection())
    {
        // Here await would suspend the execution inside of the using block while waiting 
        // for the query results from ToListAsync(). After that the execution would
        // continue and `DataConnection` would be properly disposed.
        return await db.GetTable<Person>().ToListAsync(); 
    }
}

```

## Done Wrong

```cs
public IEnumerable<Person> GetPersons()
{
    using (var db = new DataConnection())
    {
        // The query would be executed only when we enumerate, meaning after this function
        // exits. By that time DataConnection would have already been disposed and the 
        // database connection returned to the pool.
        return db.GetTable<Person>();
    }
}

// By the time we call .ToList() the DataConnection would be already disposed.
// Starting with version 1.8.0 this would fail with ObjectDisposedException.
// Versions prior to 1.8.0 would execute the query (if there are any free connections
// left) and leak the connection.
var persons = GetPersons().ToList();
```

```cs
public async Task<IEnumerable<Person>> GetPersons()
{
    using (var db = new DataConnection())
    {
        // The awaitable task would be returned immediately creating a race condition.
        return db.GetTable<Person>().ToListAsync();
    }
}

// The query execution would be called on a disposed DataConnection
var persons = await GetPersons();
```
