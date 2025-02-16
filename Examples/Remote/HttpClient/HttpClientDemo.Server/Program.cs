using System;

using HttpClientDemo.Client.DataModel;
using HttpClientDemo.Server.Components;
using HttpClientDemo.Server.DataModel;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using LinqToDB.Remote.HttpClient.Server;

namespace HttpClientDemo.Server
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

			builder.Services
				.AddControllers()
				// Add linq2db HttpClient controller.
				//
				.AddLinqToDBController<IDemoDataModel>()
				;

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
				.AddAdditionalAssemblies(typeof(Client._Imports).Assembly)
				;

			// Map controllers including linq2db HttpClient controller.
			//
			app.MapControllers();

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
