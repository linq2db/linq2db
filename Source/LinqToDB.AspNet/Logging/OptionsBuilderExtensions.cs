using System;
using System.Diagnostics;
using LinqToDB.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace LinqToDB.AspNet.Logging
{
	public static class OptionsBuilderExtensions
	{
		public static LinqToDbConnectionOptionsBuilder UseDefaultLogging(this LinqToDbConnectionOptionsBuilder builder,
			IServiceProvider provider)
		{
			var factory = provider.GetRequiredService<ILoggerFactory>();
			return UseLoggerFactory(builder, factory);
		}
		public static LinqToDbConnectionOptionsBuilder UseLoggerFactory(this LinqToDbConnectionOptionsBuilder builder,
			ILoggerFactory factory)
		{
			var adapter = new LinqToDbLoggerFactoryAdapter(factory);
			return builder.WithTraceLevel(TraceLevel.Verbose).WriteTraceWith(adapter.OnTrace);
		}
	}
}
