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

			// Initialize linq2db Signal/R.
			// This is required to be able to use linq2db Signal/R service.
			//
			await app.Services.InitSignalRAsync<IDemoDataModel>();

			await app.RunAsync();
		}
	}
}
