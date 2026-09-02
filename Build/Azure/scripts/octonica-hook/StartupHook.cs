// DOTNET_STARTUP_HOOKS assembly. Attaches a FirstChanceException listener to the linq2db test
// process so the exception Octonica discards in ClickHouseDataReader.Close(disposing: true) becomes
// visible. That catch does:
//
//     catch (Exception ex) { State = Broken; await _session.SetFailed(ex, false, async); if (disposing) return; }
//
// so nothing ever reaches linq2db and the only visible symptom is "The connection is closed." on the
// next command. A first-chance listener sees the exception before that catch runs.
//
// The runtime resolves this type by name, so it must stay in the global namespace, be called
// StartupHook, and expose a public static void Initialize().

using System.Diagnostics;
using System.Runtime.ExceptionServices;
using System.Text;

internal sealed class StartupHook
{
	private const int MaxReports = 200;

	private static readonly object    Gate     = new();
	private static          string?   _logPath;
	private static          int       _reported;

	public static void Initialize()
	{
		_logPath = Environment.GetEnvironmentVariable("OCTONICA_HOOK_LOG");

		AppDomain.CurrentDomain.FirstChanceException += OnFirstChance;

		Report($"octonica-hook attached, pid {Environment.ProcessId}");
	}

	private static void OnFirstChance(object? sender, FirstChanceExceptionEventArgs e)
	{
		if (Volatile.Read(ref _reported) >= MaxReports)
			return;

		var exception = e.Exception;
		var type      = exception.GetType();

		// Server-side errors are the test suite's own negative cases - hundreds of them per run.
		if (type.FullName == "Octonica.ClickHouseClient.Exceptions.ClickHouseServerException")
			return;

		// Cheap pre-filter before paying for a stack walk.
		var ns = type.Namespace ?? string.Empty;
		var candidate =
			   ns.StartsWith("Octonica", StringComparison.Ordinal)
			|| exception is NotSupportedException
			|| exception is InvalidCastException
			|| exception is IndexOutOfRangeException
			|| exception is ArgumentOutOfRangeException
			|| exception is FormatException
			|| exception is OverflowException
			|| exception is ObjectDisposedException
			|| exception is EndOfStreamException
			|| exception is IOException
			|| exception is System.Net.Sockets.SocketException;

		if (!candidate)
			return;

		string stack;
		try
		{
			stack = new StackTrace(2, true).ToString();
		}
		catch
		{
			return;
		}

		// Only interested in exceptions raised inside the client.
		if (!stack.Contains("Octonica", StringComparison.Ordinal))
			return;

		var builder = new StringBuilder()
			.AppendLine("---- octonica first-chance ----")
			.AppendLine($"time   : {DateTime.UtcNow:HH:mm:ss.fff}  thread {Environment.CurrentManagedThreadId}")
			.AppendLine($"type   : {type.FullName}")
			.AppendLine($"message: {exception.Message}")
			.AppendLine(stack.TrimEnd());

		Report(builder.ToString());
	}

	private static void Report(string text)
	{
		// Provider lanes and test fixtures run in parallel, so every write needs the lock.
		lock (Gate)
		{
			if (_reported >= MaxReports)
				return;

			_reported++;

			Console.WriteLine(text);

			if (!string.IsNullOrEmpty(_logPath))
			{
				try
				{
					File.AppendAllText(_logPath, text + Environment.NewLine);
				}
				catch
				{
					// A diagnostic must never break the run it is observing.
				}
			}
		}
	}
}
