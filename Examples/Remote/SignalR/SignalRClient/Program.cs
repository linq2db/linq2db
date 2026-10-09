using System;

using LinqToDB.Remote.SignalR;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using SignalRClient.DataModel;

namespace SignalRClient
{
	internal class Program
	{
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

			// Start the connection and load the server's configuration up front. Optional: the first query
			// does the same, and a query after the connection was lost starts it again.
			//
			await app.Services.InitSignalRAsync<IDemoDataModel>();

			await app.RunAsync();
		}
	}
}
