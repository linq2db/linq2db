using System;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using LinqToDB.Data;
using LinqToDB.Remote;
using LinqToDB.Remote.HttpClient.Server;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Tests.Wasm
{
	/// <summary>
	/// The remote server the WASM client talks to: Kestrel on loopback, one controller, and a <see cref="LinqService"/>
	/// that maps each configuration name to its matrix row's provider instead of the global configuration registry.
	/// </summary>
	sealed class WasmServer : IAsyncDisposable
	{
		readonly WebApplication _app;
		readonly DataConnection _sqlite;

		WasmServer(WebApplication app, DataConnection sqlite, LinqService service, Uri address)
		{
			_app    = app;
			_sqlite = sqlite;
			Service = service;
			Address = address;
		}

		public Uri         Address { get; }
		public LinqService Service { get; }

		public static async Task<WasmServer> StartAsync()
		{
			// Shared-cache in-memory database, kept alive by this connection for the round trip.
			var sqlite = CreateDataConnection(ProviderMatrix.Find(ProviderMatrix.RoundTripConfiguration)!);

			await sqlite.CreateTableAsync<WasmPerson>().ConfigureAwait(false);
			await sqlite.InsertAsync(new WasmPerson { Id = 1, Name = "John", BirthDate = new DateTime(2001, 1, 1) }).ConfigureAwait(false);
			await sqlite.InsertAsync(new WasmPerson { Id = 2, Name = "Jane", BirthDate = new DateTime(2002, 2, 2) }).ConfigureAwait(false);

			var service = new MatrixLinqService();
			var builder = WebApplication.CreateSlimBuilder();

			builder.Logging.ClearProviders();
			builder.WebHost.UseUrls("http://127.0.0.1:0");

			builder.Services.AddSingleton<ILinqService>(service);

			builder.Services
				.AddControllers()
				.ConfigureApplicationPartManager(apm =>
				{
					// Only this assembly's controller: referenced test assemblies carry LinqToDBController subclasses of their own.
					apm.ApplicationParts.Clear();
					apm.ApplicationParts.Add(new AssemblyPart(typeof(WasmServer).Assembly));
				});

			var app = builder.Build();

			app.MapControllers();

			await app.StartAsync().ConfigureAwait(false);

			var address = app.Services
				.GetRequiredService<IServer>()
				.Features
				.Get<IServerAddressesFeature>()!
				.Addresses
				.First();

			return new WasmServer(app, sqlite, service, new Uri(address));
		}

		public static DataConnection CreateDataConnection(MatrixRow row)
		{
			var connectionString = row.Configuration == ProviderMatrix.RoundTripConfiguration
				? ProviderMatrix.SqliteConnectionString
				: ProviderMatrix.DummyConnectionString;

			var options = new DataOptions()
				.UseConnectionString(row.CreateProvider(), connectionString);

			return new DataConnection(options);
		}

		public async ValueTask DisposeAsync()
		{
			await _app.StopAsync().ConfigureAwait(false);
			await _app.DisposeAsync().ConfigureAwait(false);
			await _sqlite.DisposeAsync().ConfigureAwait(false);
		}

		sealed class MatrixLinqService : LinqService
		{
			public override DataConnection CreateDataContext(string? configuration)
			{
				var row = ProviderMatrix.Find(configuration ?? "")
					?? throw new InvalidOperationException($"Configuration '{configuration}' is not in the provider matrix.");

				return CreateDataConnection(row);
			}
		}
	}

	/// <summary>The remote endpoint of <see cref="WasmServer"/>; public because ASP.NET Core discovers public controllers only.</summary>
	[Route(WasmReport.Route)]
	public sealed class WasmLinqController(ILinqService linqService) : LinqToDBController
	{
		protected override ILinqService CreateLinqService() => linqService;
	}
}
