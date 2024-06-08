using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace LinqToDB.Extensions.Logging
{
	using Data;

	public class LinqToDBLoggerFactoryAdapter
	{
		private readonly ILoggerFactory          _loggerFactory;
		private readonly ILogger<DataConnection> _logger;

		public LinqToDBLoggerFactoryAdapter(ILoggerFactory loggerFactory)
		{
			_loggerFactory = loggerFactory;
			_logger        = _loggerFactory.CreateLogger<DataConnection>();
		}

		public void OnTrace(string? message, string? category, TraceLevel level)
		{
			var logLevel = level switch
			{
				TraceLevel.Error   => LogLevel.Error,
				TraceLevel.Info    => LogLevel.Information,
				TraceLevel.Verbose => LogLevel.Trace,
				TraceLevel.Warning => LogLevel.Warning,
				_                  => LogLevel.None,
			};

			_logger.Log(logLevel, 0, message, null, (s, exception) => s ?? string.Empty);
		}
	}
}
