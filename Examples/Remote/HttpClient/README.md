This example is a Blazor WebAssembly application that demonstrates how to configure and use **LinqToDB** over **HttpClient**.
The project demonstrates how to set up a Blazor WebAssembly app with server-side AppController and LinqToDB for database operations.

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

    // Add linq2db HttpClient service.
    //
    builder.Services.AddLinqToDBHttpClientDataContext<IDemoDataModel>(
        builder.HostEnvironment.BaseAddress,
        //"api/linq2db",
        client => new DemoClientData(client));

    var app = builder.Build();

    // Initialize linq2db HttpClient.
    // This is required to be able to use linq2db HttpClient service.
    //
    await app.Services.GetRequiredService<IDemoDataModel>().InitHttpClientAsync();

    await app.RunAsync();
}
```

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

    builder.Services
        .AddControllers()
        // Add linq2db HttpClient controller.
        //
        .AddLinqToDBController<IDemoDataModel>(
            //"api/linq2db"
            )
        ;

    var app = builder.Build();

    // ...

    // Map controllers including linq2db HttpClient controller.
    //
    app.MapControllers();

    // ...
}
```

# Notes

