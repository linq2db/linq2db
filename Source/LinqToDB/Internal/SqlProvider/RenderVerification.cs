using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;

using LinqToDB.Internal.SqlQuery;

namespace LinqToDB.Internal.SqlProvider
{
	/// <summary>
	/// Test-only render verification mode. When the <c>LINQ2DB_RENDER_VERIFY</c> environment variable names a log file,
	/// every parameter-dependent statement rendered by <see cref="Data.DataConnection"/> is rendered again in the other
	/// ways and the command text and parameter list (names, types, values) are compared. Differences are appended to the
	/// log and, unless <c>LINQ2DB_RENDER_VERIFY_THROW</c> is <c>0</c>, fail the query with an exception. A per-provider
	/// summary is appended to the log when the process exits.
	/// </summary>
	internal static class RenderVerification
	{
		static readonly string? _logPath = Environment.GetEnvironmentVariable("LINQ2DB_RENDER_VERIFY") is { Length: > 0 } path ? path : null;
		static readonly bool    _throw   = !string.Equals(Environment.GetEnvironmentVariable("LINQ2DB_RENDER_VERIFY_THROW"), "0", StringComparison.Ordinal);

		static readonly Lock                                  _sync     = new();
		static readonly Dictionary<string, (long, long)>      _counters = new(StringComparer.Ordinal);
		static          long                                  _differences;
		static          long                                  _verified;
		static readonly string                                _instance = Guid.NewGuid().ToString("N").Substring(0, 8);
		static          int                                   _summaryRegistered;

		public static bool IsEnabled => _logPath != null;

		public static string? Compare(string expected, string actual)
		{
			return string.Equals(expected, actual, StringComparison.Ordinal) ? null : actual;
		}

		public static void DescribeParameter(StringBuilder sb, SqlParameter parameter, IReadOnlyParameterValues? parameterValues)
		{
			var value = parameter.GetParameterValue(parameterValues);

			sb
				.Append("  ").Append(parameter.Name)
				.Append(" : ").Append(parameter.Type.ToString())
				.Append(parameter.IsQueryParameter ? "" : " (inline)")
				.Append(" = ");

			AppendValue(sb, value.ProviderValue);

			sb.Append(" as ").Append(value.DbDataType.ToString()).AppendLine();
		}

		static void AppendValue(StringBuilder sb, object? value)
		{
			switch (value)
			{
				case null:
					sb.Append("null");
					break;
				case string s:
					sb.Append('"').Append(s).Append('"');
					break;
				case byte[] bytes:
					sb.Append("0x");
					foreach (var b in bytes)
						sb.Append(b.ToString("X2", CultureInfo.InvariantCulture));
					break;
				case IEnumerable items:
				{
					sb.Append('[');
					var first = true;
					foreach (var item in items)
					{
						if (!first)
							sb.Append(", ");
						first = false;
						AppendValue(sb, item);
					}

					sb.Append(']');
					break;
				}

				case IFormattable formattable:
					sb.Append(value.GetType().Name).Append(':').Append(formattable.ToString(null, CultureInfo.InvariantCulture));
					break;
				default:
					sb.Append(value.GetType().Name).Append(':').AppendFormat(CultureInfo.InvariantCulture, "{0}", value);
					break;
			}
		}

		public static void Report(string provider, string expected, string? withSecondPass, string? previousPipeline)
		{
			if (Interlocked.Exchange(ref _summaryRegistered, 1) == 0)
				AppDomain.CurrentDomain.ProcessExit += static (_, _) => WriteSummary();

			var failed = withSecondPass != null || previousPipeline != null;

			lock (_sync)
			{
				_counters.TryGetValue(provider, out var counter);
				_counters[provider] = (counter.Item1 + 1, counter.Item2 + (failed ? 1 : 0));
			}

			// The exit handler is not guaranteed to run (or to run in every load context that loaded this assembly),
			// so keep the counters on disk as well.
			if (Interlocked.Increment(ref _verified) % 500 == 0)
				WriteCounters();

			if (!failed)
				return;

			var number = Interlocked.Increment(ref _differences);
			var sb     = new StringBuilder();

			sb
				.Append("=== DIFFERENCE #").Append(number)
				.Append(" provider=").Append(provider)
				.Append(" instance=").Append(_instance)
				.Append(" kind=").Append(withSecondPass != null ? "second-pass" : "").Append(withSecondPass != null && previousPipeline != null ? "," : "").Append(previousPipeline != null ? "previous-pipeline" : "")
				.AppendLine();

			sb.AppendLine("--- caller");
			foreach (var line in Environment.StackTrace.Split('\n').Select(l => l.Trim()).Where(l => l.StartsWith("at Tests.", StringComparison.Ordinal)).Take(3))
				sb.Append("  ").AppendLine(line);

			sb.AppendLine("--- rendered (single pass)").Append(expected);

			if (withSecondPass != null)
				sb.AppendLine("--- with the per-element convert").Append(withSecondPass);

			if (previousPipeline != null)
				sb.AppendLine("--- previous pipeline").Append(previousPipeline);

			lock (_sync)
				File.AppendAllText(_logPath!, sb.ToString());

			if (_throw)
				throw new InvalidOperationException($"Render verification: difference #{number.ToString(CultureInfo.InvariantCulture)} ({provider}), see {_logPath}.");
		}

		static string FormatCounters()
		{
			var sb = new StringBuilder();

			foreach (var pair in _counters.OrderBy(p => p.Key, StringComparer.Ordinal))
			{
				sb
					.Append("SUMMARY provider=").Append(pair.Key)
					.Append(" verified=").Append(pair.Value.Item1.ToString(CultureInfo.InvariantCulture))
					.Append(" differences=").Append(pair.Value.Item2.ToString(CultureInfo.InvariantCulture))
					.AppendLine();
			}

			return sb.ToString();
		}

		static void WriteCounters()
		{
			lock (_sync)
				File.WriteAllText(_logPath + "." + _instance + ".counts", FormatCounters());
		}

		static void WriteSummary()
		{
			WriteCounters();

			lock (_sync)
				File.AppendAllText(_logPath!, FormatCounters());
		}
	}
}
