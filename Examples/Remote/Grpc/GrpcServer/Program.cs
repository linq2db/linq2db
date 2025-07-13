using System;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace Server
{
	public static class Program
	{
		public static async Task Main(string[] args)
		{
			var hb = CreateHostBuilder(args).Build();
			await hb.StartAsync();

			Console.WriteLine("Press Enter to stop server");
			Console.ReadLine();

			await hb.StopAsync();

			hb.Dispose();
		}

		public static IHostBuilder CreateHostBuilder(string[] args) =>
			Host.CreateDefaultBuilder(args)
				.ConfigureWebHostDefaults(webBuilder =>
				{
					webBuilder.UseStartup<Startup>();
					webBuilder.UseUrls("https://localhost:15001");
				});
	}
}
