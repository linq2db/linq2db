using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Tests.Wasm
{
	sealed record OperationResult(string Name, string? Result, string? ErrorType, string? ErrorMessage, string? Error);

	sealed record ConfigurationReport(string Configuration, IReadOnlyList<OperationResult> Operations, IReadOnlyList<string> ResolveAttempts);

	sealed record ModeRun(string Mode, IReadOnlyDictionary<string, ConfigurationReport> Reports, int ExitCode, bool TimedOut, string Output);

	/// <summary>Runs a published Tests.Wasm.App bundle under Node and collects its per-configuration reports.</summary>
	static class WasmBundleRunner
	{
		public const string Debug            = nameof(Debug);
		public const string TrimPartial      = nameof(TrimPartial);
		public const string TrimFull         = nameof(TrimFull);
		public const string NoReflectionJson = nameof(NoReflectionJson);

		public static readonly TimeSpan Timeout = TimeSpan.FromMinutes(2);

		// A harness that cannot find its bundles must say so, not look somewhere else.
		public static string RepoRoot { get; } = TestUtils.TryFindRepoRoot()
			?? throw new InvalidOperationException($"linq2db.slnx not found above {AppContext.BaseDirectory}.");

		public static string GetBundleEntry(string mode) => Path.Combine(RepoRoot, ".build", "wasm", mode, "wwwroot", "main.mjs");

		public static async Task<ModeRun> RunAsync(string mode, Uri server, IReadOnlyList<string> configurations)
		{
			var entry = GetBundleEntry(mode);

			if (!File.Exists(entry))
				throw new InvalidOperationException(
					$"WASM bundle '{mode}' is missing ({entry}). Publish it first: pwsh -NoProfile -File Tests/Tests.Wasm/publish-wasm-bundles.ps1");

			var startInfo = new ProcessStartInfo("node")
			{
				WorkingDirectory       = Path.GetDirectoryName(entry)!,
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
			};

			startInfo.ArgumentList.Add(Path.GetFileName(entry));
			startInfo.ArgumentList.Add(server.ToString());
			startInfo.ArgumentList.Add(ProviderMatrix.RoundTripConfiguration);

			foreach (var configuration in configurations)
				startInfo.ArgumentList.Add(configuration);

			using var process = Process.Start(startInfo)
				?? throw new InvalidOperationException("Could not start 'node'; Node.js must be on PATH.");

			var stdout = process.StandardOutput.ReadToEndAsync();
			var stderr = process.StandardError.ReadToEndAsync();

			var timedOut = false;

			using (var cts = new CancellationTokenSource(Timeout))
			{
				try
				{
					await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					timedOut = true;

					process.Kill(entireProcessTree: true);
					await process.WaitForExitAsync().ConfigureAwait(false);
				}
			}

			var output  = await stdout.ConfigureAwait(false);
			var reports = new Dictionary<string, ConfigurationReport>(StringComparer.Ordinal);

			foreach (var line in output.Split('\n'))
			{
				if (!line.StartsWith(WasmReport.Prefix, StringComparison.Ordinal))
					continue;

				var report = Parse(line.Substring(WasmReport.Prefix.Length));

				reports[report.Configuration] = report;
			}

			var log = new StringBuilder()
				.AppendLine(timedOut ? $"node timed out after {Timeout} and was killed" : $"node exit code: {process.ExitCode}")
				.AppendLine("--- stdout (without report lines) ---")
				.AppendLine(string.Join("\n", output.Split('\n').Where(l => !l.StartsWith(WasmReport.Prefix, StringComparison.Ordinal))))
				.AppendLine("--- stderr ---")
				.AppendLine(await stderr.ConfigureAwait(false))
				.ToString();

			return new ModeRun(mode, reports, process.ExitCode, timedOut, log);
		}

		static ConfigurationReport Parse(string json)
		{
			using var document = JsonDocument.Parse(json);

			var root = document.RootElement;

			var operations = root
				.GetProperty(WasmReport.Property.Operations)
				.EnumerateArray()
				.Select(o => new OperationResult(
					GetString(o, WasmReport.Property.Name)!,
					GetString(o, WasmReport.Property.Result),
					GetString(o, WasmReport.Property.ErrorType),
					GetString(o, WasmReport.Property.ErrorMessage),
					GetString(o, WasmReport.Property.Error)))
				.ToList();

			var resolveAttempts = root
				.GetProperty(WasmReport.Property.ResolveAttempts)
				.EnumerateArray()
				.Select(a => a.GetString()!)
				.ToList();

			return new ConfigurationReport(GetString(root, WasmReport.Property.Configuration)!, operations, resolveAttempts);
		}

		static string? GetString(JsonElement element, string name) =>
			element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
	}
}
