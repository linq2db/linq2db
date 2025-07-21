using System;
using System.Diagnostics;

using LinqToDB.Data;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinqToDB.Extensions.Logging
{
	public static class OptionsBuilderExtensions
	{
		/// <summary>
		/// Configures the connection to use the <see cref="ILoggerFactory"/> resolved from the container.
		/// </summary>
		/// <param name="options">Builder to configure.</param>
		/// <param name="provider">Container used to resolve the factory.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseDefaultLogging(this DataOptions options, IServiceProvider provider)
		{
			var factory = provider.GetRequiredService<ILoggerFactory>();
			return UseLoggerFactory(options, factory);
		}

		/// <summary>
		/// Configures the connection to use the <see cref="ILoggerFactory"/> passed in.
		/// </summary>
		/// <param name="options">Builder to configure.</param>
		/// <param name="factory">Factory used to resolve loggers.</param>
		/// <returns>The builder instance so calls can be chained.</returns>
		public static DataOptions UseLoggerFactory(this DataOptions options, ILoggerFactory factory)
		{
			var adapter = new LinqToDBLoggerFactoryAdapter(factory);
			return options.WithOptions<QueryTraceOptions>(o => o with { TraceLevel = TraceLevel.Verbose, WriteTrace = (m, c, l) => adapter.OnTrace(m, l) });
		}
	}
}
