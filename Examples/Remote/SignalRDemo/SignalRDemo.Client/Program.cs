using System;

using LinqToDB.Remote.SignalR;

using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

using SignalRDemo.Client.DataModel;

namespace SignalRDemo.Client
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

			await app.Services.GetRequiredService<Container<HubConnection>>().Object.StartAsync();

			// Initialize linq2db Signal/R.
			// This is required to be able to use linq2db Signal/R service.
			//
			await app.Services.GetRequiredService<IDemoDataModel>().InitSignalRAsync();

			await app.RunAsync();
		}
	}
}
