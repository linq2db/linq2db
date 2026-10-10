This example is a Blazor WebAssembly application that demonstrates how to configure and use **LinqToDB** over **Single/R**.
The project demonstrates how to set up a Blazor WebAssembly app with server-side Signal/R hubs and LinqToDB for database operations.

# How to run

1. Build
2. Run

# Configuration

Client and Server applications are configured differently.
The client application is a Blazor WebAssembly app, while the server application is an ASP.NET Core app.

## Client

Use the following code to configure the client application:

```csharp
static async Task Main(string[] args)
{
    var builder = WebAssemblyHostBuilder.CreateDefault(args);

    // Add linq2db Signal/R service. All contexts share one connection, which starts with the first query
    // and is started again by the next query after it was lost.
    //
    builder.Services.AddLinqToDBSignalRDataContext<IDemoDataModel>(
        new Uri(new Uri(builder.HostEnvironment.BaseAddress), "/hub/linq2db"),
        client => new DemoClientData(client),
        options =>
        {
            // Access token, headers, transports.
            //
            // options.ConfigureHttpConnection = http => http.AccessTokenProvider = GetAccessTokenAsync;
        });

    var app = builder.Build();

    // Optional: start the connection and load the server's configuration up front.
    //
    await app.Services.InitSignalRAsync<IDemoDataModel>();

    await app.RunAsync();
}
```

Cancelling a query (the `CancellationToken` of `ToListAsync` and other async methods) cancels it on the server
and in the database. Queries are never sent again after a lost connection: they fail, and the application decides
whether to retry.

## Server

Use the following code to configure the server application:
```csharp

public static void Main(string[] args)
{
    var builder = WebApplication.CreateBuilder(args);

    // ...

    // Add linq2db data context.
    //
    DataOptions? options = null;
    builder.Services.AddLinqToDBContext<IDemoDataModel>(provider => new DemoDB(options ??= new DataOptions()
        .UseSQLite("Data Source=:memory:;Mode=Memory;Cache=Shared")
        .UseDefaultLogging(provider)),
        ServiceLifetime.Transient);

    // Let up to 8 queries of one client connection run at the same time (by default one at a time), and send
    // server errors to the client in development only.
    //
    builder.Services.Configure<LinqToDBHubOptions>(options =>
    {
        options.MaxConcurrentCallsPerConnection   = 8;
        options.TransferInternalExceptionToClient = builder.Environment.IsDevelopment();
    });

    builder.Services
        .AddLinqToDBService<IDemoDataModel>()
        .AddSignalR()
        // The largest request the hub accepts (default 32 KB). Size it to the largest expected query.
        //
        .AddHubOptions<LinqToDBHub<IDemoDataModel>>(hubOptions => hubOptions.MaximumReceiveMessageSize = 1024 * 1024)
        ;

    // ...

    var app = builder.Build();

    // ...

    // Register linq2db SignalR Hub.
    //
    app.MapHub<LinqToDBHub<IDemoDataModel>>("/hub/linq2db");

    // ...
}
```
