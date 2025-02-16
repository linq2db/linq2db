using System;

using SignalRClient.DataModel;
using SignalRServer.Components;
using SignalRServer.DataModel;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using LinqToDB.Remote;
using LinqToDB.Remote.SignalR;

namespace SignalRServer
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			builder.Services.AddRazorComponents()
				.AddInteractiveServerComponents()
				.AddInteractiveWebAssemblyComponents();

			// Add linq2db data context.
			//
			DataOptions? options = null;
			builder.Services.AddLinqToDBContext<IDemoDataModel>(provider => new DemoDB(options ??= new DataOptions()
				.UseSQLite("Data Source=:memory:;Mode=Memory;Cache=Shared")
				.UseDefaultLogging(provider)),
				ServiceLifetime.Transient);

			// Adds SignalR services and configures the SignalR options.
			//
			builder.Services
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

			builder.Services.AddScoped<ILinqService<IDemoDataModel>>(
				provider => new LinqService<IDemoDataModel>(provider.GetRequiredService<IDataContextFactory<IDemoDataModel>>()));

			var app = builder.Build();

			if (app.Environment.IsDevelopment())
			{
				app.UseWebAssemblyDebugging();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseAntiforgery();

			app.MapStaticAssets();
			app.MapRazorComponents<App>()
				.AddInteractiveServerRenderMode()
				.AddInteractiveWebAssemblyRenderMode()
				.AddAdditionalAssemblies(typeof(SignalRClient._Imports).Assembly)
				;

			// Register linq2db SignalR Hub.
			//
			app.MapHub<LinqToDBHub<IDemoDataModel>>("/hub/linq2db");

			InitDemoDatabase(app.Services);

			app.Run();
		}

		static void InitDemoDatabase(IServiceProvider services)
		{
			var db = services.GetRequiredService<IDemoDataModel>();

			db.CreateTable<WeatherForecast>();

			var startDate = DateOnly.FromDateTime(DateTime.Now);
			var summaries = new[] { "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching" };
			var forecasts = Enumerable.Range(1, 5)
				.Select(index => new WeatherForecast
				{
					WeatherForecastID = index,
					Date              = startDate.AddDays(index),
					TemperatureC      = Random.Shared.Next(-20, 55),
					Summary           = summaries[Random.Shared.Next(summaries.Length)]
				})
				.ToArray();

			db.Model.WeatherForecasts.BulkCopy(forecasts);
		}
	}
}
