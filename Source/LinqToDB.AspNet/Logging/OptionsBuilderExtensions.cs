using System;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinqToDB.AspNet.Logging
{
	using Configuration;

	public static class OptionsBuilderExtensions
	{
		/// <summary>
		/// Configures the connection to use the <see cref="ILoggerFactory"/> resolved from the container.
		/// </summary>
		/// <param name="builder">Builder to configure.</param>
		/// <param name="provider">Container used to resolve the factory.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseDefaultLogging(this LinqToDbConnectionOptionsBuilder builder,
			IServiceProvider provider)
		{
			var factory = provider.GetRequiredService<ILoggerFactory>();
			return UseLoggerFactory(builder, factory);
		}

		/// <summary>
		/// Configures the connection to use the <see cref="ILoggerFactory"/> passed in.
		/// </summary>
		/// <param name="builder">Builder to configure.</param>
		/// <param name="factory">Factory used to resolve loggers.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static LinqToDbConnectionOptionsBuilder UseLoggerFactory(this LinqToDbConnectionOptionsBuilder builder,
			ILoggerFactory factory)
		{
			var adapter = new LinqToDbLoggerFactoryAdapter(factory);
			return builder.WithTraceLevel(TraceLevel.Verbose).WriteTraceWith(adapter.OnTrace);
		}
	}
}
