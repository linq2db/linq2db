using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading;

using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Tests
{
	/// <summary>
	/// Assembly-level NUnit action that emits a live progress heartbeat (a small JSON file) for long test runs,
	/// so an external observer can see which test is running, how many remain, and the running pass/fail tally
	/// without scraping console output.
	/// <para>
	/// Opt-in: the reporter is a no-op unless the <c>LINQ2DB_TEST_PROGRESS</c> environment variable is set. When set
	/// to a flag value (<c>1/true/on/yes</c>) the file is written to
	/// <c>&lt;repoRoot&gt;/.build/.claude/test-progress.&lt;tfm&gt;.&lt;pid&gt;.json</c>; when set to a directory or
	/// <c>*.json</c> path that path is used instead.
	/// </para>
	/// See <c>.claude/docs/testing.md</c> → "Monitoring a long run".
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly)]
	public sealed class TestProgressReporterAttribute : NUnitAttribute, ITestAction
	{
		ActionTargets ITestAction.Targets => ActionTargets.Test | ActionTargets.Suite;

		void ITestAction.BeforeTest(ITest test) => TestProgressTracker.OnBefore(test);
		void ITestAction.AfterTest (ITest test) => TestProgressTracker.OnAfter (test);
	}

	internal static class TestProgressTracker
	{
		// Resolved at compile time per target framework so the moniker is always exact (avoids net462 API gaps).
#if NET462
		const string Tfm = "net462";
#elif NET8_0
		const string Tfm = "net8.0";
#elif NET9_0
		const string Tfm = "net9.0";
#elif NET10_0
		const string Tfm = "net10.0";
#else
		const string Tfm = "unknown";
#endif

		const int MaxRecentFailures = 20;
		const int WriteThrottleMs   = 1000;  // at most ~1 file write/sec for the common (passing) case

		static readonly char[] _pathSeparators = ['/', '\\'];

		static readonly Lock                                _sync     = new();
		static readonly Stopwatch                           _clock    = new();
		static readonly List<(string Test, string Message)> _failures = new();

		static bool    _initialized;
		static bool    _enabled;
		static string? _file;
		static int     _pid;

		static long     _total;
		static long     _started;
		static long     _completed;
		static long     _passed;
		static long     _failed;
		static long     _skipped;
		static string?  _current;
		static bool     _done;
		static DateTime _startedUtc;
		static long     _lastWriteMs = -WriteThrottleMs;

		public static void OnBefore(ITest test)
		{
			lock (_sync)
			{
				if (!EnsureInit())
					return;

				if (test.IsSuite)
				{
					// The assembly (root) suite has the largest case count → that is the run total.
					// Note: under a --filter this is the discovered (pre-filter) count, so it over-counts.
					if (test.TestCaseCount > _total)
						_total = test.TestCaseCount;
					return;
				}

				_started++;
				_current = test.FullName;
				Write(force: false);
			}
		}

		public static void OnAfter(ITest test)
		{
			lock (_sync)
			{
				if (!EnsureInit())
					return;

				if (test.IsSuite)
				{
					// Suite teardown fires bottom-up; the root suite (no parent) finishing means the run is done.
					if (!_done && test.Parent == null)
					{
						_done    = true;
						_current = null;
						Write(force: true);
					}

					return;
				}

				_completed++;
				// Deliberately do NOT clear _current here: with throttled writes, nulling between every test
				// makes the on-disk snapshot almost always catch the gap (showing no current test). Keep the
				// most-recently-started test as "current" until the run completes — during a run that is the
				// in-flight (or just-finished) test, which is what a watcher wants to see.

				var force = false;

				switch (TestContext.CurrentContext.Result.Outcome.Status)
				{
					case TestStatus.Passed : _passed++;  break;
					case TestStatus.Skipped: _skipped++; break;
					case TestStatus.Failed :
						_failed++;
						force = true;
						if (_failures.Count < MaxRecentFailures)
							_failures.Add((test.FullName, Trim(TestContext.CurrentContext.Result.Message)));
						break;
					// Inconclusive / Warning — count as completed but neither pass nor fail bucket.
					default: break;
				}

				// For unfiltered runs total is exact, so completed reaching it marks the run done even if the
				// root-suite teardown ordering surprises us.
				if (!_done && _total > 0 && _completed >= _total)
				{
					_done    = true;
					_current = null;
					force    = true;
				}

				Write(force);
			}
		}

		static bool EnsureInit()
		{
			if (_initialized)
				return _enabled;

			_initialized = true;

			// Disabled when unset, empty, or an explicit falsy token. The /test-progress skill turns the trace
			// off by setting the value to "0" (not by removing the key), because Claude Code propagates a changed
			// env value into the running session but does not un-apply a removed one until restart.
			var env = Environment.GetEnvironmentVariable("LINQ2DB_TEST_PROGRESS");
			if (IsDisabledValue(env))
				return _enabled = false;

			try
			{
#if NET462
				_pid        = Process.GetCurrentProcess().Id;
#else
				_pid        = Environment.ProcessId;
#endif
				_file       = ResolvePath(env!.Trim());
				_startedUtc = DateTime.UtcNow;

				var dir = Path.GetDirectoryName(_file);
				if (!string.IsNullOrEmpty(dir))
					Directory.CreateDirectory(dir!);

				_clock.Start();
				_enabled = true;
			}
			catch
			{
				// Never let progress-reporting setup break a test run.
				_enabled = false;
			}

			return _enabled;
		}

		static string ResolvePath(string env)
		{
			var fileName = $"test-progress.{Tfm}.{_pid}.json";

			if (!IsFlag(env))
			{
				if (Directory.Exists(env))
					return Path.Combine(env, fileName);

				if (env.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
					|| env.IndexOfAny(_pathSeparators) >= 0)
					return env;
			}

			return Path.Combine(FindRepoRoot(), ".build", ".claude", fileName);
		}

		static bool IsDisabledValue(string? value)
		{
			if (string.IsNullOrWhiteSpace(value))
				return true;

			var v = value!.Trim();

			return v.Equals("0",        StringComparison.OrdinalIgnoreCase)
				|| v.Equals("false",    StringComparison.OrdinalIgnoreCase)
				|| v.Equals("off",      StringComparison.OrdinalIgnoreCase)
				|| v.Equals("no",       StringComparison.OrdinalIgnoreCase)
				|| v.Equals("disable",  StringComparison.OrdinalIgnoreCase)
				|| v.Equals("disabled", StringComparison.OrdinalIgnoreCase);
		}

		static bool IsFlag(string value) =>
			   value.Equals("1",       StringComparison.OrdinalIgnoreCase)
			|| value.Equals("true",    StringComparison.OrdinalIgnoreCase)
			|| value.Equals("on",      StringComparison.OrdinalIgnoreCase)
			|| value.Equals("yes",     StringComparison.OrdinalIgnoreCase)
			|| value.Equals("enable",  StringComparison.OrdinalIgnoreCase)
			|| value.Equals("enabled", StringComparison.OrdinalIgnoreCase);

		static string FindRepoRoot()
		{
			try
			{
				for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
					if (File.Exists(Path.Combine(dir.FullName, "linq2db.slnx")))
						return dir.FullName;
			}
			catch
			{
				// fall through to base directory
			}

			return AppContext.BaseDirectory;
		}

		static void Write(bool force)
		{
			var nowMs = _clock.ElapsedMilliseconds;
			if (!force && nowMs - _lastWriteMs < WriteThrottleMs)
				return;

			_lastWriteMs = nowMs;

			var elapsedSec = _clock.Elapsed.TotalSeconds;
			var rate       = elapsedSec > 0 ? _completed / elapsedSec : 0d;
			var eta        = rate > 0 && _total > _completed ? (_total - _completed) / rate : (double?)null;

			var sb = new StringBuilder(512);

			sb.Append('{');
			sb.Append("\"tfm\":")        .Append(JsonString(Tfm))                       .Append(',');
			sb.Append("\"pid\":")        .Append(Int(_pid))                             .Append(',');
			sb.Append("\"startedUtc\":") .Append(JsonString(Iso(_startedUtc)))          .Append(',');
			sb.Append("\"updatedUtc\":") .Append(JsonString(Iso(DateTime.UtcNow)))      .Append(',');
			sb.Append("\"done\":")       .Append(_done ? "true" : "false")              .Append(',');
			sb.Append("\"total\":")      .Append(Long(_total))                          .Append(',');
			sb.Append("\"completed\":")  .Append(Long(_completed))                      .Append(',');
			sb.Append("\"started\":")    .Append(Long(_started))                        .Append(',');
			sb.Append("\"passed\":")     .Append(Long(_passed))                         .Append(',');
			sb.Append("\"failed\":")     .Append(Long(_failed))                         .Append(',');
			sb.Append("\"skipped\":")    .Append(Long(_skipped))                        .Append(',');
			sb.Append("\"currentTest\":").Append(JsonString(_current))                  .Append(',');
			sb.Append("\"elapsedSec\":") .Append(Num(elapsedSec))                       .Append(',');
			sb.Append("\"testsPerSec\":").Append(Num(rate))                             .Append(',');
			sb.Append("\"etaSec\":")     .Append(eta.HasValue ? Num(eta.Value) : "null").Append(',');
			sb.Append("\"recentFailures\":[");

			for (var i = 0; i < _failures.Count; i++)
			{
				if (i > 0)
					sb.Append(',');

				sb.Append("{\"test\":").Append(JsonString(_failures[i].Test))
				  .Append(",\"message\":").Append(JsonString(_failures[i].Message)).Append('}');
			}

			sb.Append("]}");

			WriteFile(sb.ToString());
		}

		static void WriteFile(string content)
		{
			try
			{
				var tmp = _file + ".tmp";

				File.WriteAllText(tmp, content, new UTF8Encoding(false));

				// Replace atomically so a concurrent reader never sees a half-written file.
				if (File.Exists(_file))
					File.Replace(tmp, _file, null);
				else
					File.Move(tmp, _file!);
			}
			catch
			{
				// Best effort — IO contention or a locked file must never fail the test run.
				try { File.WriteAllText(_file!, content, new UTF8Encoding(false)); }
				catch { /* give up silently */ }
			}
		}

		static string Iso(DateTime value) => value.ToString("o", CultureInfo.InvariantCulture);
		static string Int(int     value)  => value.ToString(CultureInfo.InvariantCulture);
		static string Long(long   value)  => value.ToString(CultureInfo.InvariantCulture);
		static string Num(double  value)  => value.ToString("0.###", CultureInfo.InvariantCulture);

		static string Trim(string? message)
		{
			if (string.IsNullOrEmpty(message))
				return "";

			return message!.Length > 500 ? message.Substring(0, 500) : message;
		}

		static string JsonString(string? value)
		{
			if (value == null)
				return "null";

			var sb = new StringBuilder(value.Length + 2);
			sb.Append('"');

			foreach (var c in value)
			{
				switch (c)
				{
					case '"' : sb.Append("\\\""); break;
					case '\\': sb.Append("\\\\"); break;
					case '\b': sb.Append("\\b");  break;
					case '\f': sb.Append("\\f");  break;
					case '\n': sb.Append("\\n");  break;
					case '\r': sb.Append("\\r");  break;
					case '\t': sb.Append("\\t");  break;
					default:
						if (c < ' ')
							sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
						else
							sb.Append(c);
						break;
				}
			}

			sb.Append('"');
			return sb.ToString();
		}
	}
}
