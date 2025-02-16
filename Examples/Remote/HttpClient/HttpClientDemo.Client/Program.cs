using System;

using LinqToDB.Remote.HttpClient.Client;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace HttpClientDemo.Client
{
	using DataModel;

	internal class Program
	{
		static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);

			// <LinqToDB.Remote.HttpClient Settings>
			// Add linq2db HttpClient service.
			//
			builder.Services.AddLinqToDBHttpClientDataContext<IDemoDataModel>(
				builder.HostEnvironment.BaseAddress,
				client => new DemoClientData(client));
			// </LinqToDB.Remote.HttpClient Settings>

			var app = builder.Build();

			// <LinqToDB.Remote.HttpClient Settings>
			// Initialize linq2db HttpClient.
			// This is required to be able to use linq2db HttpClient service.
			//
			await app.Services.GetRequiredService<IDemoDataModel>().InitHttpClientAsync();
			// </LinqToDB.Remote.HttpClient Settings>

			await app.RunAsync();
		}
	}
}
