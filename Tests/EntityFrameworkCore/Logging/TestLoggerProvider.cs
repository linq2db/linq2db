using System;
using System.Collections.Concurrent;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.Options;

namespace LinqToDB.EntityFrameworkCore.Tests.Logging
{
	/// <summary>
	/// A provider of console logger instances.
	/// </summary>
	[ProviderAlias("Console")]
	public sealed class TestLoggerProvider : ILoggerProvider, ISupportExternalScope
	{

		private readonly IOptionsMonitor<ConsoleLoggerOptions> _options;
		private readonly ConcurrentDictionary<string, TestLogger> _loggers;

		private readonly IDisposable? _optionsReloadToken;
		private IExternalScopeProvider _scopeProvider = NullExternalScopeProvider.Instance;

		/// <summary>
		/// Creates an instance of <see cref="TestLoggerProvider"/>.
		/// </summary>
		/// <param name="options">The options to create console logger instances with.</param>
		public TestLoggerProvider(IOptionsMonitor<ConsoleLoggerOptions> options)
		{
			_options = options;
			_loggers = new ConcurrentDictionary<string, TestLogger>();

			ReloadLoggerOptions(options.CurrentValue);
			_optionsReloadToken = _options.OnChange(ReloadLoggerOptions);
		}

		private void ReloadLoggerOptions(ConsoleLoggerOptions options)
		{
			foreach (var logger in _loggers)
			{
				logger.Value.Options = options;
			}
		}

		/// <inheritdoc />
		public ILogger CreateLogger(string categoryName)
		{
			return _loggers.GetOrAdd(categoryName, loggerName => new TestLogger(loggerName)
			{
				Options = _options.CurrentValue,
				ScopeProvider = _scopeProvider
			});
		}

		/// <inheritdoc />
		public void Dispose()
		{
			_optionsReloadToken?.Dispose();
		}

		/// <inheritdoc />
		public void SetScopeProvider(IExternalScopeProvider scopeProvider)
		{
			_scopeProvider = scopeProvider;

			foreach (var logger in _loggers)
			{
				logger.Value.ScopeProvider = _scopeProvider;
			}

		}
	}

}
