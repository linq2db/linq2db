# LINQ to DB Dependency Injection and Logging Extensions<!-- omit in toc -->

[![License](https://img.shields.io/github/license/linq2db/linq2db)](MIT-LICENSE.txt)

Main [article](https://linq2db.github.io/articles/get-started/asp-dotnet-core/index.html).

- [Setup Walkthrough](#setup-walkthrough)
  - [Create a new project](#create-a-new-project)
  - [Install LINQ To DB](#install-linq-to-db)
  - [Custom Data Connection](#custom-data-connection)
  - [Add Connection String](#add-connection-string)
  - [Configure Dependency injection](#configure-dependency-injection)
  - [Simple Entity Configuration](#simple-entity-configuration)
  - [Add table property to the data connection](#add-table-property-to-the-data-connection)
- [Inject the connection into a controller](#inject-the-connection-into-a-controller)
  - [Quick start for people already familiar with LINQ To DB](#quick-start-for-people-already-familiar-with-linq-to-db)

## Setup Walkthrough

### Create a new project

First thing we're going to do is create a new ASP.NET Core application using the dotnet CLI

```txt
dotnet new webapp -o gettingStartedLinqToDBAspNet
```

### Install LINQ To DB

We can now use the CLI to install LINQ To DB and database provider (SQLite in this walkthrough)

```txt
dotnet add package linq2db.Extensions
dotnet add package System.Data.SQLite.Core
```

### Custom Data Connection

We're going to create a custom data connection to use to access LINQ To DB, create a class like this:

```C#
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using LinqToDB.Data;

public class AppDataConnection: DataConnection
{
    public AppDataConnection(DataOptions<AppDataConnection> options)
        :base(options.Options)
    {

    }
}
```

> [!TIP]  
> Note here our `AppDataConnection` inherits from `LinqToDB.Data.DataConnection` which is the base class for the `Linq To DB` connection.
>
> [!TIP]  
>a public constructor that accepts `DataOptions<AppDataConnection>` and passes the options on to the base constructor is required.

### Add Connection String

For this example we're going to use SQLite in memory mode, for production you'll want to use something else, but it's pretty easy to change.

First you want to add the connection string to `appsettings.Development.json`, something like this:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft": "Warning",
      "Microsoft.Hosting.Lifetime": "Information"
    }
  },
  //add this
  "ConnectionStrings": {
    "Default": ":memory:" //<-- connection string, used in the next step
  }
}
```

### Configure Dependency injection

inside `Startup.cs` you want register the data connection like this:

```C#
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        //...
        //using LinqToDB.Configuration
        services.AddLinqToDBContext<AppDataConnection>((provider, options)
            => options
                //will configure the AppDataConnection to use
                //sqite with the provided connection string
                //there are methods for each supported database
                .UseSQLite(Configuration.GetConnectionString("Default"), SQLiteProvider.Microsoft)
                //default logging will log everything using the ILoggerFactory configured in the provider
                .UseDefaultLogging(provider));
        //...
    }
}
```

> [!TIP]  
> There's plenty of other configuration options available, if you are familiar with `LINQ To DB` already, you can convert your existing application over to use the new `DataOptions` class as every configuration method is supported
>
> [!TIP]  
> Use `AddLinqToDBContext<TContext, TContextImplementation>` if you would like to resolve an interface or base class instead of the concrete class in your controllers

By default this will configure the service provider to create a new `AppDataConnection` for each HTTP Request, and will dispose of it once the request is finished. This can be configured with the last parameter to `AddLinqToDBContext(... ServiceLifetime lifetime)`, more information about lifetimes [here](https://docs.microsoft.com/en-us/aspnet/core/fundamentals/dependency-injection#service-lifetimes)

### Simple Entity Configuration

Let's create this simple entity in our project

```C#
using System;
using LinqToDB.Mapping;

public class Person
{
    [PrimaryKey]
    public Guid     Id       { get; set; } = Guid.NewGuid();
    public string   Name     { get; set; }
    public DateTime Birthday { get; set; }
}
```

### Add table property to the data connection

Open up our `AppDataConnection` and add this property

```C#
public class AppDataConnection: DataConnection
{
    //...
    public ITable<Person> People => GetTable<Person>();
    //...
}
```

Now we can inject our data connection into a controller and query and insert/update/delete using the `ITable<Person>` interface.

> [!TIP]
> side note, since we don't have anything to create the actual database, we need to add this code into the configure method in `Startup.cs`
>
>```C#
>public class Startup
>{
>    //...
>    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
>    {
>        //...
>        using (var scope = app.ApplicationServices.CreateScope())
>        {
>            var dataConnection = scope.ServiceProvider.GetService<AppDataConnection>();
>            dataConnection.CreateTable<Person>();
>        }
>        //...
>    }
>}
>//...
>```

## Inject the connection into a controller

In order to actually access the database we're going to want to use it from a controller, here's a sample controller to get you started with a few examples.

```C#
public class PeopleController : Controller
{
    private readonly AppDataConnection _connection;

    public PeopleController(AppDataConnection connection)
    {
        _connection = connection;
    }

    [HttpGet]
    public Task<Person[]> ListPeople()
    {
        return _connection.People.ToArrayAsync();
    }

    [HttpGet("{id}")]
    public Task<Person?> GetPerson(Guid id)
    {
        return _connection.People.SingleOrDefaultAsync(person => person.Id == id);
    }

    [HttpDelete("{id}")]
    public Task<int> DeletePerson(Guid id)
    {
        return _connection.People.Where(person => person.Id == id).DeleteAsync();
    }

    [HttpPatch]
    public Task<int> UpdatePerson(Person person)
    {
        return _connection.UpdateAsync(person);
    }

    [HttpPatch("{id}/new-name")]
    public Task<int> UpdatePersonName(Guid id, string newName)
    {
        return _connection.People.Where(person => person.Id == id)
            .Set(person => person.Name, newName)
            .UpdateAsync();
    }

    [HttpPut]
    public Task<int> InsertPerson(Person person)
    {
        return _connection.InsertAsync(person);
    }
}
```

### Quick start for people already familiar with LINQ To DB

LINQ To DB now has support for Dependency injection. Here's a simple example of how to add it to dependency injection

```C#
public class Startup
{
    public IConfiguration Configuration { get; }

    public void ConfigureServices(IServiceCollection services)
    {
        //...
        //using LinqToDB.Configuration
        services.AddLinqToDBContext<AppDataConnection>((provider, options)
            => options
                //will configure the AppDataConnection to use
                //SqlServer with the provided connection string
                //there are methods for each supported database
                .UseSqlServer(Configuration.GetConnectionString("Default"))

                //default logging will log everything using
                //an ILoggerFactory configured in the provider
                .UseDefaultLogging(provider));
        //...
    }
}
```

We've done our best job to allow any existing use case to be migrated to using the new configuration options, please create an issue if something isn't supported. There are also some methods to setup tracing and mapping schema.

You'll need to update your data connection to accept the new options class too.

```C#
public class AppDataConnection: DataConnection
{
    public AppDataConnection(DataOptions<AppDataConnection> options)
        :base(options.Options)
    {

    }
}
```

`DataConnection` will use the options passed into the base constructor to setup the connection.
