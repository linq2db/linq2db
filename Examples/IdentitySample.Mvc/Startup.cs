using System.Data.SqlClient;
using System.IO;
using IdentitySample.Models;
using IdentitySample.Services;
using LinqToDB;
using LinqToDB.Data;
using LinqToDB.DataProvider.SqlServer;
using LinqToDB.Identity;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace IdentitySample
{
	public class Startup
	{
		public Startup(IHostingEnvironment env)
		{
			// Set up configuration sources.
			var builder = new ConfigurationBuilder()
				.SetBasePath(Directory.GetCurrentDirectory())
				.AddJsonFile("appsettings.json")
				.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);

			//if (env.IsDevelopment())
			//	builder.AddUserSecrets();

			builder.AddEnvironmentVariables();
			Configuration = builder.Build();
		}

		public IConfigurationRoot Configuration { get; set; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			// Set connection configuration
			DataConnection
				.AddConfiguration(
					"Default",
					Configuration["Data:DefaultConnection:ConnectionString"],
					new SqlServerDataProvider("Default", SqlServerVersion.v2012, SqlServerProvider.SystemDataSqlClient));

			DataConnection.DefaultConfiguration = "Default";

			services.AddIdentity<ApplicationUser, LinqToDB.Identity.IdentityRole>()
				.AddLinqToDBStores(new DefaultConnectionFactory())
				.AddDefaultTokenProviders();

			services.AddAuthentication()
				.AddCookie(options =>
				{
					options.Cookie.Name = "Interop";
					options.DataProtectionProvider =
						DataProtectionProvider.Create(new DirectoryInfo("C:\\Github\\Identity\\artifacts"));

				});

			services.AddMvc();

			// Add application services.
			services.AddTransient<IEmailSender, AuthMessageSender>();
			services.AddTransient<ISmsSender, AuthMessageSender>();
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
		{
			loggerFactory.AddConsole(Configuration.GetSection("Logging"));
			loggerFactory.AddDebug();

			if (env.IsDevelopment())
				app.UseDeveloperExceptionPage();
			else
				app.UseExceptionHandler("/Home/Error");

			var connectionString = new SqlConnectionStringBuilder(Configuration["Data:DefaultConnection:ConnectionString"])
			{
				InitialCatalog = "master"
			}.ConnectionString;

			using (var db = new DataConnection(SqlServerTools.GetDataProvider(), connectionString))
			{
				try
				{
					var sql = "create database [" +
					          new SqlConnectionStringBuilder(Configuration["Data:DefaultConnection:ConnectionString"])
						          .InitialCatalog + "]";
					db.Execute(sql);
				}
				catch
				{
					//
				}
			}

			// Try to create tables
			using (var db = new ApplicationDataConnection())
			{
				TryCreateTable<ApplicationUser>(db);
				TryCreateTable<LinqToDB.Identity.IdentityRole>(db);
				TryCreateTable<LinqToDB.Identity.IdentityUserClaim<string>>(db);
				TryCreateTable<LinqToDB.Identity.IdentityRoleClaim<string>>(db);
				TryCreateTable<LinqToDB.Identity.IdentityUserLogin<string>>(db);
				TryCreateTable<LinqToDB.Identity.IdentityUserRole<string>>(db);
				TryCreateTable<LinqToDB.Identity.IdentityUserToken<string>>(db);
			}

			app.UseStaticFiles();

			app.UseAuthentication();
			// To configure external authentication please see http://go.microsoft.com/fwlink/?LinkID=532715

			app.UseMvc(routes =>
			{
				routes.MapRoute(
					"default",
					"{controller=Home}/{action=Index}/{id?}");
			});
		}

		private void TryCreateTable<T>(ApplicationDataConnection db)
			where T : class
		{
			try
			{
				db.CreateTable<T>();
			}
			catch
			{
				//
			}
		}
	}
}