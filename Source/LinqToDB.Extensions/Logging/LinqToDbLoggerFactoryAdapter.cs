using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

using LinqToDB.Data;

using Microsoft.Extensions.Logging;

namespace LinqToDB.Extensions.Logging
{
	public class LinqToDBLoggerFactoryAdapter(ILoggerFactory loggerFactory)
	{
		private readonly ILogger<DataConnection> _logger = loggerFactory.CreateLogger<DataConnection>();

		[SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Maybe we will use category in future")]
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
