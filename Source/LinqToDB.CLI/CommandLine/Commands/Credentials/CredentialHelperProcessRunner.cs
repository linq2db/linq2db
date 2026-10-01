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
	/// Starts a credential helper process: resolves the command before every run, passes the verb as the only
	/// argument and the request on standard input, bounds the run with a timeout and an output cap, and kills the process
	/// tree on timeout. Secrets are written only to standard input and read only from standard output.
	/// </summary>
	internal sealed class CredentialHelperProcessRunner : ICredentialHelperRunner
	{
		public const string InteractiveVariable = "LINQ2DB_CREDENTIAL_INTERACTIVE";
		public const int    MaxOutputBytes      = 1024 * 1024;
		public const int    MaxErrorLineLength  = 200;

		const int MaxErrorBytes = 4096;

		static readonly TimeSpan _breakerDuration = TimeSpan.FromSeconds(60);
		static readonly TimeSpan _drainTimeout    = TimeSpan.FromSeconds(5);

		/// <summary>Helpers that timed out in non-interactive mode, with the time until which they are not started again.</summary>
		static readonly ConcurrentDictionary<string, DateTimeOffset> _breaker = new(StringComparer.Ordinal);

		readonly CredentialHelperSettings              _settings;
		readonly bool                                  _interactive;
		readonly IReadOnlyDictionary<string, string?>? _environment;

		public CredentialHelperProcessRunner(
			CredentialHelperSettings              settings,
			bool                                  interactive,
			IReadOnlyDictionary<string, string?>? environment = null)
		{
			_settings    = settings;
			_interactive = interactive;
			_environment = environment;
		}

		/// <summary>Timeout when nobody can answer a prompt (MCP server, redirected input).</summary>
		public TimeSpan     Timeout            { get; init; } = TimeSpan.FromSeconds(10);
		/// <summary>Timeout when a user at the console can answer a prompt the helper raises (keyring unlock, gpg pinentry).</summary>
		public TimeSpan     InteractiveTimeout { get; init; } = TimeSpan.FromSeconds(60);
		/// <summary>Clock for the fail-fast window after a timeout.</summary>
		public TimeProvider TimeProvider       { get; init; } = TimeProvider.System;

		public string DisplayName => _settings.Arguments.Count == 0 ? _settings.Command : $"{_settings.Command} {string.Join(' ', _settings.Arguments.ToArray())}";

		public string Describe()
		{
			var command = TryResolve(out var path, out _) ? path : _settings.Command;
			return _settings.Arguments.Count == 0 ? command : $"{command} {string.Join(' ', _settings.Arguments.ToArray())}";
		}

		public CredentialHelperRunResult Run(string verb, byte[] request)
		{
			// ICredentialStore is synchronous; the helper's pipes are pumped asynchronously underneath.
			return Task.Run(() => RunAsync(verb, request)).GetAwaiter().GetResult();
		}

		async Task<CredentialHelperRunResult> RunAsync(string verb, byte[] request)
		{
			if (!TryResolve(out var path, out var error))
				return CredentialHelperRunResult.Failed(error!);

			// The fail-fast window is per helper: the program together with its arguments ("dotnet run a.cs" and
			// "dotnet run b.cs" are different helpers).
			var breakerKey = _settings.Arguments.Count == 0 ? path : path + "\0" + string.Join('\0', _settings.Arguments.ToArray());

			if (!_interactive && _breaker.TryGetValue(breakerKey, out var until) && TimeProvider.GetUtcNow() < until)
			{
				var seconds = Math.Ceiling((until - TimeProvider.GetUtcNow()).TotalSeconds).ToString(CultureInfo.InvariantCulture);
				return CredentialHelperRunResult.Failed($"Credential helper '{DisplayName}' did not answer recently and is not started again for {seconds} seconds. Unlock the credential store (keyring or gpg-agent) and retry.");
			}

			if (!CredentialHelperCommandResolver.TryGetLaunch(path, _settings.Arguments, verb, OperatingSystem.IsWindows(), OperatingSystem.IsWindows() ? Environment.SystemDirectory : null, out var launch, out error))
				return CredentialHelperRunResult.Failed(error!);

			var startInfo = new ProcessStartInfo(launch.FileName)
			{
				UseShellExecute        = false,
				RedirectStandardInput  = true,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				CreateNoWindow         = true,
				WorkingDirectory       = GetWorkingDirectory(),
			};

			if (launch.RawArguments != null)
				startInfo.Arguments = launch.RawArguments;
			else
				foreach (var argument in launch.Arguments)
					startInfo.ArgumentList.Add(argument);

			startInfo.Environment[InteractiveVariable] = _interactive ? "1" : "0";

			if (_environment != null)
				foreach (var variable in _environment)
					startInfo.Environment[variable.Key] = variable.Value;

			// When a user can answer, any verb may wait for a prompt, not only store. A configured timeout replaces both.
			var timeout = _settings.Timeout ?? (_interactive ? InteractiveTimeout : Timeout);

			Process process;

			try
			{
				process = Process.Start(startInfo) ?? throw new InvalidOperationException("The process did not start.");
			}
			catch (Exception ex) when (ex is Win32Exception or InvalidOperationException or IOException or UnauthorizedAccessException)
			{
				return CredentialHelperRunResult.Failed($"Cannot start credential helper '{DisplayName}' ({path}): {ex.Message}");
			}

			using (process)
			using (var cancellation = new CancellationTokenSource())
			{
				// The pipes are pumped concurrently while the process runs; every task is awaited (or abandoned with the
				// killed process) below.
#pragma warning disable LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
				var outputTask = ReadCappedAsync(process.StandardOutput.BaseStream, MaxOutputBytes, stopAtLimit: true,  cancellation.Token);
				var errorTask  = ReadCappedAsync(process.StandardError .BaseStream, MaxErrorBytes,  stopAtLimit: false, cancellation.Token);
				var inputTask  = WriteInputAsync(process.StandardInput .BaseStream, request);
				var exitTask   = process.WaitForExitAsync(cancellation.Token);
				var delayTask  = Task.Delay(timeout, cancellation.Token);
#pragma warning restore LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.

				try
				{
					// Stops early when the output cap is hit: the helper would otherwise block on a full pipe until the timeout.
					var first = await Task.WhenAny(exitTask, outputTask, delayTask).ConfigureAwait(false);

					if (first == outputTask && (await outputTask.ConfigureAwait(false)).Overflow)
						return Abandon(process, await outputTask.ConfigureAwait(false), $"Credential helper '{DisplayName}' wrote more than {MaxOutputBytes.ToString(CultureInfo.InvariantCulture)} bytes to standard output; the helper process was stopped.");

					if (first != exitTask && await Task.WhenAny(exitTask, delayTask).ConfigureAwait(false) != exitTask)
					{
						if (!_interactive)
							_breaker[breakerKey] = TimeProvider.GetUtcNow() + _breakerDuration;

						return Abandon(process, null, $"Credential helper '{DisplayName}' did not answer within {timeout.TotalSeconds.ToString("0", CultureInfo.InvariantCulture)} seconds (a locked keyring or a prompt waiting for input?); the helper process was stopped.");
					}

					// The pipes close when the helper and everything it started have exited. A helper must not leave a
					// background process holding them.
#pragma warning disable LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.
					var drained = Task.WhenAll(outputTask, errorTask);
#pragma warning restore LindhartAnalyserMissingAwaitWarningVariable // Possible unwanted Task returned from method.

					if (await Task.WhenAny(drained, Task.Delay(_drainTimeout, cancellation.Token)).ConfigureAwait(false) != drained)
						return Abandon(process, null, $"Credential helper '{DisplayName}' exited but left a background process holding its output open.");

					var output = await outputTask.ConfigureAwait(false);

					if (output.Overflow)
						return Abandon(process, output, $"Credential helper '{DisplayName}' wrote more than {MaxOutputBytes.ToString(CultureInfo.InvariantCulture)} bytes to standard output.");

					await inputTask.ConfigureAwait(false);

					_breaker.TryRemove(breakerKey, out _);

					return new CredentialHelperRunResult(null, process.ExitCode, output.Data, Encoding.UTF8.GetString((await errorTask.ConfigureAwait(false)).Data));
				}
				finally
				{
					await cancellation.CancelAsync().ConfigureAwait(false);
				}
			}
		}

		static CredentialHelperRunResult Abandon(Process process, (byte[] Data, bool Overflow)? output, string message)
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

			return CredentialHelperRunResult.Failed(message);
		}

		bool TryResolve(out string path, out string? error)
		{
			return CredentialHelperCommandResolver.TryResolve(
				_settings.Command,
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

		static async Task WriteInputAsync(Stream input, byte[] request)
		{
			await using (input.ConfigureAwait(false))
			{
				try
				{
					await input.WriteAsync(request).ConfigureAwait(false);
					await input.FlushAsync().ConfigureAwait(false);
				}
				catch (IOException)
				{
					// The helper exited or closed its input without reading the whole request.
				}
			}
		}

		/// <summary>
		/// Reads a pipe, keeping at most <paramref name="limit"/> bytes. Standard output stops at the limit (the caller
		/// then stops the helper); standard error keeps being drained and the rest is discarded, so a helper that writes a lot
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
		/// Returns the first non-empty line of a helper's error output, with control characters replaced and the length
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
