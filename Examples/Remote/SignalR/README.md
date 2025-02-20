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

    // Add linq2db Signal/R service.
    //
    builder.Services.AddLinqToDBSignalRDataContext<IDemoDataModel>(
        builder.HostEnvironment.BaseAddress,
        //"/hub/linq2db",
        client => new DemoClientData(client));

    var app = builder.Build();

    await app.Services.GetRequiredService<Container<HubConnection>>().Object.StartAsync();

    // Initialize linq2db Signal/R.
    // This is required to be able to use linq2db Signal/R service.
    //
    await app.Services.GetRequiredService<IDemoDataModel>().InitSignalRAsync();

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
        // Adds SignalR services and configures the SignalR options.
        //
        .AddLinqToDBService<IDemoDataModel>()
        .AddSignalR(hubOptions =>
        {
            hubOptions.ClientTimeoutInterval               = TimeSpan.FromSeconds(60);
            hubOptions.HandshakeTimeout                    = TimeSpan.FromSeconds(30);
            hubOptions.MaximumParallelInvocationsPerClient = 30;
            hubOptions.EnableDetailedErrors                = true;
            hubOptions.MaximumReceiveMessageSize           = 1024 * 1024 * 1024;
        })
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

