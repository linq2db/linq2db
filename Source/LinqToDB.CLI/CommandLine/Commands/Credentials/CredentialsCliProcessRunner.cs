using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Starts a credentials CLI process: resolves the program before every run, passes the configured argument string and
	/// the verb as arguments and the request on standard input, bounds the run with a timeout and an output cap, and kills
	/// the process tree on timeout. Secrets are written only to standard input and read only from standard output.
	/// </summary>
	internal sealed class CredentialsCliProcessRunner : ICredentialsCliRunner
	{
		public const string InteractiveVariable = "LINQ2DB_CREDENTIAL_INTERACTIVE";
		public const int    MaxOutputBytes      = 1024 * 1024;
		public const int    MaxErrorLineLength  = 200;

		const int MaxErrorBytes         = 4096;
		const int ErrorBadExeFormat     = 193; // Windows: not a valid application
		const int ErrorNoExecutable     = 8;   // ENOEXEC: no '#!' line
		const int ErrorPermissionDenied = 13;  // EACCES: no execute bit

		static readonly TimeSpan _breakerDuration = TimeSpan.FromSeconds(60);
		static readonly TimeSpan _drainTimeout    = TimeSpan.FromSeconds(5);

		/// <summary>Programs that timed out in non-interactive mode, with the time until which they are not started again.</summary>
		static readonly ConcurrentDictionary<string, DateTimeOffset> _breaker = new(StringComparer.Ordinal);

		readonly CredentialsCliSettings                _settings;
		readonly bool                                  _interactive;
		readonly IReadOnlyDictionary<string, string?>? _environment;

		public CredentialsCliProcessRunner(
			CredentialsCliSettings                settings,
			bool                                  interactive,
			IReadOnlyDictionary<string, string?>? environment = null)
		{
			_settings    = settings;
			_interactive = interactive;
			_environment = environment;
		}

		/// <summary>Timeout when nobody can answer a prompt (MCP server, redirected input).</summary>
		public TimeSpan     Timeout            { get; init; } = TimeSpan.FromSeconds(10);
		/// <summary>Timeout when a user at the console can answer a prompt the program raises (keyring unlock, gpg pinentry).</summary>
		public TimeSpan     InteractiveTimeout { get; init; } = TimeSpan.FromSeconds(60);
		/// <summary>Clock for the fail-fast window after a timeout.</summary>
		public TimeProvider TimeProvider       { get; init; } = TimeProvider.System;

		public string DisplayName => _settings.DisplayName;

		public string Describe()
		{
			var program = TryResolve(out var path, out _) ? path : _settings.Program;
			return _settings.Arguments.Length == 0 ? program : $"{program} {_settings.Arguments}";
		}

		public CredentialsCliRunResult Run(string verb, byte[] request)
		{
			// ICredentialStore is synchronous; the program's pipes are pumped asynchronously underneath.
			return Task.Run(() => RunAsync(verb, request)).GetAwaiter().GetResult();
		}

		async Task<CredentialsCliRunResult> RunAsync(string verb, byte[] request)
		{
			if (!TryResolve(out var path, out var error))
				return CredentialsCliRunResult.Failed(error!);

			// The fail-fast window is per program and argument string ("dotnet run a.cs" and "dotnet run b.cs" differ).
			var breakerKey = path + "\0" + _settings.Arguments;

			if (!_interactive && _breaker.TryGetValue(breakerKey, out var until) && TimeProvider.GetUtcNow() < until)
			{
				var seconds = Math.Ceiling((until - TimeProvider.GetUtcNow()).TotalSeconds).ToString(CultureInfo.InvariantCulture);
				return CredentialsCliRunResult.Failed($"Credentials CLI '{DisplayName}' did not answer recently and is not started again for {seconds} seconds. Unlock the credential store (keyring or gpg-agent) and retry.");
			}

			var launch = CredentialsCliCommandResolver.GetLaunch(path, _settings.Arguments, verb, OperatingSystem.IsWindows(), OperatingSystem.IsWindows() ? Environment.SystemDirectory : null);

			var startInfo = new ProcessStartInfo(launch.FileName)
			{
				Arguments              = launch.Arguments,
				UseShellExecute        = false,
				RedirectStandardInput  = true,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				CreateNoWindow         = true,
				WorkingDirectory       = GetWorkingDirectory(),
			};

			startInfo.Environment[InteractiveVariable] = _interactive ? "1" : "0";
			// A program started through "dotnet" must not print the first-run welcome text on standard output.
			startInfo.Environment["DOTNET_NOLOGO"] = "1";

			if (_environment != null)
				foreach (var variable in _environment)
					startInfo.Environment[variable.Key] = variable.Value;

			// When a user can answer, any verb may wait for a prompt, not only store.
			var timeout = _interactive ? InteractiveTimeout : Timeout;

			Process process;

			try
			{
				process = Process.Start(startInfo) ?? throw new InvalidOperationException("The process did not start.");
			}
			catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
			{
				return CredentialsCliRunResult.Failed($"Cannot start credentials CLI '{DisplayName}' ({path}): {ex.Message}{GetStartHint(ex, path)}");
			}

			using (process)
			using (var cancellation = new CancellationTokenSource())
			{
				// The pipes are pumped concurrently while the process runs; every task is awaited (or abandoned with the
				// killed process) below.
#pragma warning disable LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
				var outputTask = ReadCappedAsync(process.StandardOutput.BaseStream, MaxOutputBytes, stopAtLimit: true,  cancellation.Token);
				var errorTask  = ReadCappedAsync(process.StandardError .BaseStream, MaxErrorBytes,  stopAtLimit: false, cancellation.Token);
				var inputTask  = WriteInputAsync(process.StandardInput .BaseStream, request, cancellation.Token);
				var exitTask   = process.WaitForExitAsync(cancellation.Token);
				var delayTask  = Task.Delay(timeout, cancellation.Token);
#pragma warning restore LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.

				try
				{
					// Stops early when the output cap is hit: the program would otherwise block on a full pipe until the timeout.
					var first = await Task.WhenAny(exitTask, outputTask, delayTask).ConfigureAwait(false);

					if (first == outputTask && (await outputTask.ConfigureAwait(false)).Overflow)
						return Abandon(process, await outputTask.ConfigureAwait(false), $"Credentials CLI '{DisplayName}' wrote more than {MaxOutputBytes.ToString(CultureInfo.InvariantCulture)} bytes to standard output; the process was stopped.");

					if (first != exitTask && await Task.WhenAny(exitTask, delayTask).ConfigureAwait(false) != exitTask)
					{
						TripBreaker(breakerKey);

						return Abandon(process, null, $"Credentials CLI '{DisplayName}' did not answer within {timeout.TotalSeconds.ToString("0", CultureInfo.InvariantCulture)} seconds (a locked keyring or a prompt waiting for input?); the process was stopped.");
					}

					// The pipes close when the program and everything it started have exited. It must not leave a
					// background process holding them.
#pragma warning disable LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
					var drained = Task.WhenAll(outputTask, errorTask);
#pragma warning restore LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.

					if (await Task.WhenAny(drained, Task.Delay(_drainTimeout, cancellation.Token)).ConfigureAwait(false) != drained)
					{
						// A process that outlived the program cannot be stopped from here (it is no longer in its
						// process tree); fail fast for a while instead of piling more of them up.
						TripBreaker(breakerKey);
						return Abandon(process, null, $"Credentials CLI '{DisplayName}' exited but left a background process holding its output open.");
					}

					var output = await outputTask.ConfigureAwait(false);

					if (output.Overflow)
						return Abandon(process, output, $"Credentials CLI '{DisplayName}' wrote more than {MaxOutputBytes.ToString(CultureInfo.InvariantCulture)} bytes to standard output.");

					// The request must have been read; a background process holding standard input without reading it would
					// otherwise block the write forever.
					if (await Task.WhenAny(inputTask, Task.Delay(_drainTimeout, cancellation.Token)).ConfigureAwait(false) != inputTask)
					{
						TripBreaker(breakerKey);
						return Abandon(process, output, $"Credentials CLI '{DisplayName}' exited without reading its whole request, and a background process still holds its input.");
					}

					var errors = await errorTask.ConfigureAwait(false);

					_breaker.TryRemove(breakerKey, out _);

					return new CredentialsCliRunResult(null, process.ExitCode, output.Data, Encoding.UTF8.GetString(errors.Data))
					{
						ErrorOutputTruncated = errors.Overflow,
					};
				}
				finally
				{
					await cancellation.CancelAsync().ConfigureAwait(false);
				}
			}
		}

		void TripBreaker(string breakerKey)
		{
			if (!_interactive)
				_breaker[breakerKey] = TimeProvider.GetUtcNow() + _breakerDuration;
		}

		static CredentialsCliRunResult Abandon(Process process, (byte[] Data, bool Overflow)? output, string message)
		{
			try
			{
				process.Kill(entireProcessTree: true);
			}
			catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or NotSupportedException)
			{
			}

			if (output != null)
				CryptographicOperations.ZeroMemory(output.Value.Data);

			return CredentialsCliRunResult.Failed(message);
		}

		static string GetStartHint(Exception exception, string path)
		{
			if (exception is not Win32Exception win32)
				return string.Empty;

			if (OperatingSystem.IsWindows())
			{
				return win32.NativeErrorCode == ErrorBadExeFormat
					? " " + CredentialsCliCommandResolver.GetInterpreterHint(path)
					: string.Empty;
			}

			return win32.NativeErrorCode is ErrorNoExecutable or ErrorPermissionDenied
				? " A script needs the execute bit (chmod u+x) and a '#!' line, for example #!/bin/sh."
				: string.Empty;
		}

		bool TryResolve(out string path, out string? error)
		{
			return CredentialsCliCommandResolver.TryResolve(
				_settings.Program,
				_settings.BaseDirectory,
				OperatingSystem.IsWindows(),
				Environment.GetEnvironmentVariable("PATH"),
				Environment.GetEnvironmentVariable("PATHEXT"),
				File.Exists,
				out path,
				out error);
		}

		static string GetWorkingDirectory()
		{
			var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
			return Directory.Exists(home) ? home : Path.GetTempPath();
		}

		static async Task WriteInputAsync(Stream input, byte[] request, CancellationToken cancellationToken)
		{
			try
			{
				await using (input.ConfigureAwait(false))
				{
					await input.WriteAsync(request, cancellationToken).ConfigureAwait(false);
					await input.FlushAsync(cancellationToken).ConfigureAwait(false);
				}
			}
			catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
			{
				// The program exited or closed its input without reading the whole request, or the run was abandoned.
			}
		}

		/// <summary>
		/// Reads a pipe, keeping at most <paramref name="limit"/> bytes. Standard output stops at the limit (the caller
		/// then stops the program); standard error keeps being drained and the rest is discarded, so a program that writes a lot
		/// of diagnostics does not block on a full pipe.
		/// </summary>
		static async Task<(byte[] Data, bool Overflow)> ReadCappedAsync(Stream stream, int limit, bool stopAtLimit, CancellationToken cancellationToken)
		{
			var data  = new byte[limit + 1];
			var total = 0;

			try
			{
				while (total <= limit)
				{
					var read = await stream.ReadAsync(data.AsMemory(total), cancellationToken).ConfigureAwait(false);

					if (read == 0)
						break;

					total += read;
				}

				if (!stopAtLimit && total > limit)
				{
					var discard = new byte[16 * 1024];

					while (await stream.ReadAsync(discard, cancellationToken).ConfigureAwait(false) > 0)
					{
					}
				}
			}
			catch (Exception ex) when (ex is IOException or ObjectDisposedException or OperationCanceledException)
			{
			}

			var result = data.AsSpan(0, Math.Min(total, limit)).ToArray();
			CryptographicOperations.ZeroMemory(data);
			return (result, total > limit);
		}

		/// <summary>
		/// Returns the first non-empty line of a program's error output, with control characters replaced and the length
		/// capped, for an error message. Secrets must be redacted from <paramref name="text"/> before this call: trimming
		/// and truncation would otherwise leave a fragment that no longer matches.
		/// </summary>
		internal static string? GetFirstLine(string text)
		{
			foreach (var line in text.Split('\n'))
			{
				var trimmed = line.Trim();

				if (trimmed.Length == 0)
					continue;

				trimmed = new string(trimmed.Select(static c => char.IsControl(c) ? ' ' : c).ToArray());

				return trimmed.Length > MaxErrorLineLength ? string.Concat(trimmed.AsSpan(0, MaxErrorLineLength), "...") : trimmed;
			}

			return null;
		}
	}
}
