using System;

using HttpClientClient.DataModel;

using LinqToDB.Remote.HttpClient.Client;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace HttpClientClient
{
	internal class Program
	{
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
	}
}
