using LinqToDB;
using LinqToDB.Configuration;
using LinqToDB.Data;
using LinqToDB.Remote;
using LinqToDB.Remote.Grpc;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ProtoBuf.Grpc.Server;

namespace Server
{
	public class Startup
	{
		public void ConfigureServices(IServiceCollection services)
		{
			// Set up Linq2DB connection
			DataConnection.DefaultSettings = new LinqToDBSettings(
				"Northwind",
				ProviderName.SqlServer,
				"Server=.;Database=Northwind;Trusted_Connection=True"
				);

			services.AddGrpc();
			services.AddCodeFirstGrpc();
			services.AddSingleton(p =>
				new GrpcLinqService(
					new LinqService()
					{
						AllowUpdates = true
					},
					true
				));
		}

		public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGrpcService<GrpcLinqService>();
			});
		}
	}

}
