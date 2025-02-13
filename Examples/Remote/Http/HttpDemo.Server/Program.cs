using System;

using HttpDemo.Client.DataModel;
using HttpDemo.Server.Components;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using LinqToDB.Remote;
using LinqToDB.Remote.Http.Server;

namespace HttpDemo.Server
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
			builder.Services.AddLinqToDBContext<IDemoDataModel>(provider => new DemoDB(new DataOptions()
				.UseSQLite("Data Source=:memory:;Mode=Memory;Cache=Shared")
				.UseDefaultLogging(provider)),
				ServiceLifetime.Transient);

			// Add linq2db HttpClient controller.
			//
			builder.Services
//				.AddScoped<LinqService<IDemoDataModel>>(provider =>
//					new LinqService<IDemoDataModel>(provider.GetRequiredService<IDataContextFactory<IDemoDataModel>>()) { AllowUpdates = true })
				.AddControllers()
				// By default, linq2db controller endpoints are mapped to 'api/linq2db'.
				// If you need to change it, you have to override LinqToDBController class and set it using RouteAttribute on the class.
				//.AddApplicationPart(typeof(LinqToDB.Remote.Http.Server.LinqToDBController).Assembly)
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
