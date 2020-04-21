using System;
using System.Diagnostics;
using LinqToDB.Data;
using Microsoft.Extensions.Logging;

namespace LinqToDB.AspNet.Logging
{
	public class LinqToDbLoggerFactoryAdapter
	{
		private readonly ILoggerFactory _loggerFactory;
		private readonly ILogger<DataConnection> _logger;

		public LinqToDbLoggerFactoryAdapter(ILoggerFactory loggerFactory)
		{
			_loggerFactory = loggerFactory;
			_logger = _loggerFactory.CreateLogger<DataConnection>();

		}

		public void OnTrace(string message, string displayName, TraceLevel level)
		{
			LogLevel logLevel;
			switch (level)
			{
				case TraceLevel.Error: logLevel = LogLevel.Error;
					break;
				case TraceLevel.Info: logLevel = LogLevel.Information;
					break;
				case TraceLevel.Verbose: logLevel = LogLevel.Trace;
					break;
				case TraceLevel.Warning: logLevel = LogLevel.Warning;
					break;
				default:
					logLevel = LogLevel.None;
					break;
			}
			_logger.Log(logLevel, (EventId)0, message, null, (s, exception) => s);
		}
	}
}
