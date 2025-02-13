using System;

using LinqToDB.Remote.Http.Client;

using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

namespace HttpDemo.Client
{
	using DataModel;

	internal class Program
	{
		static async Task Main(string[] args)
		{
			var builder = WebAssemblyHostBuilder.CreateDefault(args);



			// Add linq2db HttpClient service.
			//
			builder.Services.AddLinqToDBHttpDataContext<IDemoDataModel>(
				builder.HostEnvironment.BaseAddress,
				client => new DemoData(client));



			var app = builder.Build();



			// Initialize linq2db HttpClient.
			await app.Services.GetRequiredService<IDemoDataModel>().InitAsync();



			await app.RunAsync();
		}
	}
}
