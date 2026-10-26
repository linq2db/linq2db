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
	public sealed record OperationResult(string Name, string? Result, string? ErrorType, string? ErrorMessage, string? Error);

	public sealed record ConfigurationReport(string Configuration, IReadOnlyList<OperationResult> Operations, IReadOnlyList<string> ResolveAttempts);

	public sealed record ModeRun(string Mode, IReadOnlyDictionary<string, ConfigurationReport> Reports, int ExitCode, string Output);

	/// <summary>Runs a published Tests.Wasm.App bundle under Node and collects its per-configuration reports.</summary>
	public static class WasmBundleRunner
	{
		public const string Debug            = nameof(Debug);
		public const string TrimPartial      = nameof(TrimPartial);
		public const string TrimFull         = nameof(TrimFull);
		public const string NoReflectionJson = nameof(NoReflectionJson);

		const string ReportPrefix = "L2DB-WASM-REPORT ";

		static readonly TimeSpan _timeout = TimeSpan.FromMinutes(5);

		public static string RepoRoot { get; } = FindRepoRoot();

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

			using (var cts = new CancellationTokenSource(_timeout))
			{
				try
				{
					await process.WaitForExitAsync(cts.Token).ConfigureAwait(false);
				}
				catch (OperationCanceledException)
				{
					process.Kill(entireProcessTree: true);
					await process.WaitForExitAsync().ConfigureAwait(false);
				}
			}

			var output  = await stdout.ConfigureAwait(false);
			var reports = new Dictionary<string, ConfigurationReport>(StringComparer.Ordinal);

			foreach (var line in output.Split('\n'))
			{
				if (!line.StartsWith(ReportPrefix, StringComparison.Ordinal))
					continue;

				var report = Parse(line.Substring(ReportPrefix.Length));

				reports[report.Configuration] = report;
			}

			var log = new StringBuilder()
				.AppendLine($"node exit code: {process.ExitCode}")
				.AppendLine("--- stdout (without report lines) ---")
				.AppendLine(string.Join("\n", output.Split('\n').Where(l => !l.StartsWith(ReportPrefix, StringComparison.Ordinal))))
				.AppendLine("--- stderr ---")
				.AppendLine(await stderr.ConfigureAwait(false))
				.ToString();

			return new ModeRun(mode, reports, process.ExitCode, log);
		}

		static ConfigurationReport Parse(string json)
		{
			using var document = JsonDocument.Parse(json);

			var root = document.RootElement;

			var operations = root
				.GetProperty("operations")
				.EnumerateArray()
				.Select(o => new OperationResult(
					GetString(o, "name")!,
					GetString(o, "result"),
					GetString(o, "errorType"),
					GetString(o, "errorMessage"),
					GetString(o, "error")))
				.ToList();

			var resolveAttempts = root
				.GetProperty("resolveAttempts")
				.EnumerateArray()
				.Select(a => a.GetString()!)
				.ToList();

			return new ConfigurationReport(GetString(root, "configuration")!, operations, resolveAttempts);
		}

		static string? GetString(JsonElement element, string name) =>
			element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;

		static string FindRepoRoot()
		{
			for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir != null; dir = dir.Parent)
				if (File.Exists(Path.Combine(dir.FullName, "linq2db.slnx")))
					return dir.FullName;

			throw new InvalidOperationException($"linq2db.slnx not found above {AppContext.BaseDirectory}.");
		}
	}
}
