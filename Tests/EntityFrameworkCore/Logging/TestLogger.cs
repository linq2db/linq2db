using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;

using Tests;

namespace LinqToDB.EntityFrameworkCore.Tests.Logging
{
	internal sealed class TestLogger : ILogger
	{
		private const string _logLevelPadding = ": ";
		private static readonly string _messagePadding = new (' ', GetLogLevelString(LogLevel.Critical).Length + _logLevelPadding.Length);
		private static readonly string _newLineWithMessagePadding = Environment.NewLine + _messagePadding;

		private readonly ConsoleColor DefaultConsoleColor = ConsoleColor.Black;

		private readonly string _name;

		internal TestLogger(string name)
		{
			ArgumentNullException.ThrowIfNull(name);

			_name = name;
		}

		internal IExternalScopeProvider? ScopeProvider { get; set; }

		internal ConsoleLoggerOptions? Options { get; set; }

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
		{
			if (!IsEnabled(logLevel))
			{
				return;
			}

			ArgumentNullException.ThrowIfNull(formatter);

			LogBaselines(state);

			var message = formatter(state, exception);

			if (!string.IsNullOrEmpty(message) || exception != null)
			{
				WriteMessage(logLevel, _name, eventId.Id, message, exception);
			}
		}

		private void LogBaselines<TState>(TState message)
		{
			string? parameters = null;
			string? commandType = null;
			string? commandText = null;
			foreach (var kvp in (IEnumerable<KeyValuePair<string, object>>)(object)message!)
			{
				if (kvp.Key == "parameters")
				{
					parameters = kvp.Value.ToString();
				}
				else if (kvp.Key == "commandType")
				{
					commandType = kvp.Value.ToString();
				}
				else if (kvp.Key == "commandText")
				{
					commandText = kvp.Value.ToString();
				}
			}
			if (!string.IsNullOrEmpty(parameters))
			{
				BaselinesManager.LogQuery($@"Parameters:
{parameters}
");
			}
			if (!string.IsNullOrEmpty(commandType) && commandType != "Text")
			{
				BaselinesManager.LogQuery($@"Command Type: {commandType}

");
			}
			if (!string.IsNullOrEmpty(commandText))
			{
				BaselinesManager.LogQuery($@"{commandText}

");
			}
		}

		public void WriteMessage(LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
		{
			var format = Options?.FormatterName;
			Debug.Assert(format is ConsoleFormatterNames.Simple or ConsoleFormatterNames.Systemd);

			LogMessageEntry entry;
			if (format == ConsoleFormatterNames.Simple)
			{
				entry = CreateDefaultLogMessage(new StringBuilder(), logLevel, logName, eventId, message, exception);
			}
			else if (format == ConsoleFormatterNames.Systemd)
			{
				entry = CreateSystemdLogMessage(new StringBuilder(), logLevel, logName, eventId, message, exception);
			}
			else
			{
				entry = default;
			}
			EnqueueMessage(entry);
		}

		private void EnqueueMessage(LogMessageEntry entry)
		{
			WriteMessage(entry);
		}

		internal void WriteMessage(LogMessageEntry message)
		{
			if (message.TimeStamp != null)
			{
				Console.Write(message.TimeStamp, message.MessageColor, message.MessageColor);
			}

			if (message.LevelString != null)
			{
				Console.Write(message.LevelString);
			}

			Console.WriteLine(message.Message);
		}

		private LogMessageEntry CreateDefaultLogMessage(StringBuilder logBuilder, LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
		{
			// Example:
			// INFO: ConsoleApp.Program[10]
			//       Request received

			var logLevelColors = GetLogLevelConsoleColors(logLevel);
			var logLevelString = GetLogLevelString(logLevel);
			// category and event id
			logBuilder.Append(_logLevelPadding)
				.Append(logName)
				.Append('[')
				.Append(eventId)
				.AppendLine("]");

			// scope information
			GetScopeInformation(logBuilder, multiLine: true);

			if (!string.IsNullOrEmpty(message))
			{
				// message
				logBuilder.Append(_messagePadding);

				var len = logBuilder.Length;
				logBuilder.AppendLine(message);
				logBuilder.Replace(Environment.NewLine, _newLineWithMessagePadding, len, message.Length);
			}

			// Example:
			// System.InvalidOperationException
			//    at Namespace.Class.Function() in File:line X
			if (exception != null)
			{
				// exception message
				logBuilder.AppendLine(exception.ToString());
			}

#pragma warning disable CS0618 // Type or member is obsolete
			var timestampFormat = Options?.TimestampFormat;
#pragma warning restore CS0618 // Type or member is obsolete

			return new LogMessageEntry(
				Message: logBuilder.ToString(),
				TimeStamp: timestampFormat != null ? FormattableString.Invariant($"{DateTime.Now:timestampFormat}") : null,
				LevelString: logLevelString,
				LevelBackground: logLevelColors.Background,
				LevelForeground: logLevelColors.Foreground,
				MessageColor: DefaultConsoleColor,
				LogAsError: logLevel >= Options?.LogToStandardErrorThreshold
			);
		}

		private LogMessageEntry CreateSystemdLogMessage(StringBuilder logBuilder, LogLevel logLevel, string logName, int eventId, string message, Exception? exception)
		{
			// systemd reads messages from standard out line-by-line in a '<pri>message' format.
			// newline characters are treated as message delimiters, so we must replace them.
			// Messages longer than the journal LineMax setting (default: 48KB) are cropped.
			// Example:
			// <6>ConsoleApp.Program[10] Request received

			// log level
			var logLevelString = GetSyslogSeverityString(logLevel);
			logBuilder.Append(logLevelString);

			// timestamp
#pragma warning disable CS0618 // Type or member is obsolete
			var timestampFormat = Options?.TimestampFormat;
#pragma warning restore CS0618 // Type or member is obsolete
			if (timestampFormat != null)
			{
				logBuilder.Append(FormattableString.Invariant($"{DateTime.Now:timestampFormat}"));
			}

			// category and event id
			logBuilder.Append(logName)
				.Append('[')
				.Append(eventId)
				.Append(']');

			// scope information
			GetScopeInformation(logBuilder, multiLine: false);

			// message
			if (!string.IsNullOrEmpty(message))
			{
				logBuilder.Append(' ');
				// message
				AppendAndReplaceNewLine(logBuilder, message);
			}

			// exception
			// System.InvalidOperationException at Namespace.Class.Function() in File:line X
			if (exception != null)
			{
				logBuilder.Append(' ');
				AppendAndReplaceNewLine(logBuilder, exception.ToString());
			}

			// newline delimiter
			logBuilder.Append(Environment.NewLine);

			return new LogMessageEntry(
				Message: logBuilder.ToString(),
				LogAsError: logLevel >= Options?.LogToStandardErrorThreshold
			);

			static void AppendAndReplaceNewLine(StringBuilder sb, string message)
			{
				var len = sb.Length;
				sb.Append(message);
				sb.Replace(Environment.NewLine, " ", len, message.Length);
			}
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel != LogLevel.None;
		}

		public IDisposable BeginScope<TState>(TState state)
			where TState : notnull
			=> ScopeProvider?.Push(state) ?? NullScope.Instance;

		private static string GetLogLevelString(LogLevel logLevel)
		{
			return logLevel switch
			{
				LogLevel.Trace       => "trace",
				LogLevel.Debug       => "debug",
				LogLevel.Information => "info",
				LogLevel.Warning     => "warn",
				LogLevel.Error       => "fail",
				LogLevel.Critical    => "critical",
				_                    => throw new ArgumentOutOfRangeException(nameof(logLevel)),
			};
		}

		private static string GetSyslogSeverityString(LogLevel logLevel)
		{
			// 'Syslog Message Severities' from https://tools.ietf.org/html/rfc5424.
			return logLevel switch
			{
				LogLevel.Trace or LogLevel.Debug => "<7>",// debug-level messages
				LogLevel.Information             => "<6>",// informational messages
				LogLevel.Warning                 => "<4>",// warning conditions
				LogLevel.Error                   => "<3>",// error conditions
				LogLevel.Critical                => "<2>",// critical conditions
				_                                => throw new ArgumentOutOfRangeException(nameof(logLevel)),
			};
		}

		private ConsoleColors GetLogLevelConsoleColors(LogLevel logLevel)
		{
#pragma warning disable CS0618 // Type or member is obsolete
			if (Options?.DisableColors == true)
#pragma warning restore CS0618 // Type or member is obsolete
			{
				return new ConsoleColors(null, null);
			}

			// We must explicitly set the background color if we are setting the foreground color,
			// since just setting one can look bad on the users console.
			return logLevel switch
			{
				LogLevel.Critical    => new ConsoleColors(ConsoleColor.White, ConsoleColor.Red),
				LogLevel.Error       => new ConsoleColors(ConsoleColor.Black, ConsoleColor.Red),
				LogLevel.Warning     => new ConsoleColors(ConsoleColor.Yellow, ConsoleColor.Black),
				LogLevel.Information => new ConsoleColors(ConsoleColor.DarkGreen, ConsoleColor.Black),
				LogLevel.Debug       => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
				LogLevel.Trace       => new ConsoleColors(ConsoleColor.Gray, ConsoleColor.Black),
				_                    => new ConsoleColors(DefaultConsoleColor, DefaultConsoleColor),
			};
		}

		private void GetScopeInformation(StringBuilder stringBuilder, bool multiLine)
		{
			var scopeProvider = ScopeProvider;
#pragma warning disable CS0618 // Type or member is obsolete
			if (Options?.IncludeScopes == true && scopeProvider != null)
#pragma warning restore CS0618 // Type or member is obsolete
			{
				var initialLength = stringBuilder.Length;

				scopeProvider.ForEachScope((scope, state) =>
				{
					var (builder, paddingAt) = state;
					var addPadding = paddingAt == builder.Length;
					if (addPadding)
					{
						builder.Append(_messagePadding);
						builder.Append("=> ");
					}
					else
					{
						builder.Append(" => ");
					}
					builder.Append(scope);
				}, (stringBuilder, multiLine ? initialLength : -1));

				if (stringBuilder.Length > initialLength && multiLine)
				{
					stringBuilder.AppendLine();
				}
			}
		}

		private readonly record struct ConsoleColors(ConsoleColor? Foreground, ConsoleColor? Background);
	}
}
