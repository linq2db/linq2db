using System;
using System.Linq.Expressions;

using SignalRClient.DataModel;
using SignalRServer.Components;
using SignalRServer.DataModel;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.ClickHouse;
using LinqToDB.DataProvider.Informix;
using LinqToDB.DataProvider.Oracle;
using LinqToDB.Extensions.DependencyInjection;
using LinqToDB.Extensions.Logging;
using LinqToDB.Remote.SignalR;

namespace SignalRServer
{
	static class Program
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
				//.UseSQLite("Data Source=:memory:;Mode=Memory;Cache=Shared")
				//.UseSqlServer("Server=localhost;Database=DemoData;Integrated Security=SSPI;TrustServerCertificate=true")
				//.UseClickHouse("Host=localhost;Port=9000;Database=testdb1;User=testuser;Password=testuser", ClickHouseProvider.Octonica)
				//.UseClickHouse("Host=localhost;Port=8123;Database=testdb2;Username=testuser;Password=testuser;UseSession=true", ClickHouseProvider.ClickHouseDriver)
				//.UseClickHouse("Host=localhost;Port=9004;Database=testdb3;Username=testuser;Password=testuser;Pooling=false;", ClickHouseProvider.MySqlConnector)
				//.UseDB2("Server=localhost:50000;Database=testdb;UID=db2inst1;PWD=Password12!")
				//.UseFirebird("DataSource=localhost;Port=3025;Database=/firebird/data/testdb25.fdb;User Id=sysdba;Password=masterkey;charset=UTF8")
				//.UseInformix("Server=localhost:9189;Database=testdatadb2;userid=informix;password=in4mix", InformixProvider.DB2)
				//.UseOracle("Data Source=(DESCRIPTION=(ADDRESS=(PROTOCOL=TCP)(HOST=localhost)(PORT=1531))(CONNECT_DATA=(SERVICE_NAME=XE)));User Id=system;Password=password;", OracleVersion.AutoDetect, OracleProvider.Managed)
				//.UsePostgreSQL("Server=localhost;Port=5492;Database=testdata;User Id=postgres;Password=Password12!;Pooling=true;MinPoolSize=10;MaxPoolSize=100;")
				.UseAse("Data Source=localhost;Port=5000;Database=TestDataCore;Uid=sa;Password=myPassword;ConnectionTimeout=300;EnableBulkLoad=1;MinPoolSize=100;MaxPoolSize=200;AnsiNull=1;")
				.UseDefaultLogging(provider)),
				ServiceLifetime.Transient);

			builder.Services
				// Adds SignalR services and configures the SignalR options.
				//
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

			db.DropTable  <WeatherForecast>(tableOptions : TableOptions.CheckExistence);
			db.CreateTable<WeatherForecast>(tableOptions : TableOptions.CheckExistence);

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
