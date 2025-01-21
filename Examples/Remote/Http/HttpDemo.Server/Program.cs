using System;

using HttpDemo.Client.DataModel;
using HttpDemo.Server.Components;

using LinqToDB;
using LinqToDB.Data;

namespace HttpDemo.Server
{
	public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddRazorComponents()
				.AddInteractiveServerComponents()
				.AddInteractiveWebAssemblyComponents();



			// Initialize linq2db options.
			//
			var linq2dbOptions = new DataOptions().UseSQLite("Data Source=:memory:;Mode=Memory;Cache=Shared");

			// Add linq2db service.
			//
			builder.Services.AddTransient<IDemoDataModel>(_ => new DemoDB(linq2dbOptions));

			// Add linq2db controller.
			//
			builder.Services
				.AddControllers()
				.AddApplicationPart(typeof(LinqToDB.Remote.Http.Server.LinqToDBController).Assembly)
				;



			var app = builder.Build();

			// Configure the HTTP request pipeline.
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
				.AddAdditionalAssemblies(typeof(Client._Imports).Assembly);



			// Map controllers including linq2db controller.
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
