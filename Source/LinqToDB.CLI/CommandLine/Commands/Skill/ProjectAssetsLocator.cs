using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Finds project files and reads the linq2db package they resolved from the NuGet assets file.
	/// </summary>
	internal static class ProjectAssetsLocator
	{
		private const string TimeoutVariable      = "LINQ2DB_CLI_MSBUILD_TIMEOUT_SECONDS";
		private const int    DefaultTimeoutSeconds = 120;

		private static readonly string[] _projectExtensions = [".csproj", ".fsproj", ".vbproj"];
		private static readonly string[] _skippedDirectories = ["bin", "obj", "node_modules"];

		public static bool IsProjectFile(string path)
		{
			return _projectExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);
		}

		/// <summary>
		/// Projects of a directory: the ones directly inside it or, when there are none, the ones below it.
		/// </summary>
		public static IReadOnlyList<string> FindProjects(string directory)
		{
			var direct = EnumerateProjects(directory).ToList();

			if (direct.Count > 0)
				return direct;

			var found = new List<string>();
			var queue = new Queue<string>();

			queue.Enqueue(directory);

			while (queue.Count > 0)
			{
				var current = queue.Dequeue();

				foreach (var child in Directory.EnumerateDirectories(current).OrderBy(static d => d, StringComparer.Ordinal))
				{
					var name = Path.GetFileName(child);

					if (name.StartsWith('.') || _skippedDirectories.Contains(name, StringComparer.OrdinalIgnoreCase))
						continue;

					// a link can loop back to an ancestor or lead out of the repository into unrelated projects
					var info = new DirectoryInfo(child);

					if (info.LinkTarget != null || info.Attributes.HasFlag(FileAttributes.ReparsePoint))
						continue;

					found.AddRange(EnumerateProjects(child));
					queue.Enqueue(child);
				}
			}

			return found;
		}

		private static IEnumerable<string> EnumerateProjects(string directory)
		{
			return Directory.EnumerateFiles(directory)
				.Where(IsProjectFile)
				.OrderBy(static f => f, StringComparer.Ordinal);
		}

		/// <summary>
		/// Asks MSBuild where the project's assets file lives (it moves with <c>UseArtifactsOutput</c>,
		/// <c>BaseIntermediateOutputPath</c> and <c>MSBuildProjectExtensionsPath</c>), so nothing is assumed about <c>obj</c>.
		/// A multi-targeted project reports nothing from its outer evaluation, so it is evaluated again for its first target
		/// framework; the assets file is shared by all of them.
		/// </summary>
		public static async Task<(string? AssetsFile, string? Error)> GetAssetsFile(ICliEnvironment environment, string project, CancellationToken cancellationToken)
		{
			var (properties, error) = await EvaluateProperties(environment, project, null, cancellationToken).ConfigureAwait(false);

			if (properties == null)
				return (null, error);

			var assetsFile = properties.GetValueOrDefault("ProjectAssetsFile");

			if (string.IsNullOrEmpty(assetsFile))
			{
				var frameworks = properties.GetValueOrDefault("TargetFrameworks")?.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

				if (frameworks is { Length: > 0 })
				{
					(properties, error) = await EvaluateProperties(environment, project, frameworks[0], cancellationToken).ConfigureAwait(false);

					if (properties == null)
						return (null, error);

					assetsFile = properties.GetValueOrDefault("ProjectAssetsFile") ?? string.Empty;
				}
			}

			return !string.IsNullOrEmpty(assetsFile)
				? (assetsFile, null)
				: (null, $"MSBuild did not report a project assets file for '{project}'.");
		}

		private static async Task<(Dictionary<string, string>? Properties, string? Error)> EvaluateProperties(ICliEnvironment environment, string project, string? targetFramework, CancellationToken cancellationToken)
		{
			var startInfo = new ProcessStartInfo(GetDotnetHost(environment))
			{
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
			};

			startInfo.ArgumentList.Add("msbuild");
			startInfo.ArgumentList.Add(project);
			startInfo.ArgumentList.Add("-getProperty:ProjectAssetsFile,TargetFrameworks");
			startInfo.ArgumentList.Add("-nologo");

			if (targetFramework != null)
				startInfo.ArgumentList.Add("-p:TargetFramework=" + targetFramework);

			var seconds = int.TryParse(environment.GetEnvironmentVariable(TimeoutVariable), NumberStyles.None, CultureInfo.InvariantCulture, out var configured) && configured > 0
				? configured
				: DefaultTimeoutSeconds;

			using var timer   = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
			using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timer.Token);

			try
			{
				using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start 'dotnet msbuild'.");

				try
				{
					var texts = await Task.WhenAll(
						process.StandardOutput.ReadToEndAsync(timeout.Token),
						process.StandardError .ReadToEndAsync(timeout.Token)).ConfigureAwait(false);

					await process.WaitForExitAsync(timeout.Token).ConfigureAwait(false);

					if (process.ExitCode != 0)
						return (null, $"Cannot evaluate project '{project}' with MSBuild: {(texts[0] + Environment.NewLine + texts[1]).Trim()}");

					using var document = JsonDocument.Parse(texts[0]);

					return (document.RootElement.GetProperty("Properties").EnumerateObject()
						.ToDictionary(static p => p.Name, static p => p.Value.GetString() ?? string.Empty, StringComparer.Ordinal), null);
				}
				finally
				{
					// a timeout, a cancellation or an error must not leave MSBuild (and the processes it started) running
					try
					{
						if (!process.HasExited)
							process.Kill(true);
					}
					catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
					{
					}
				}
			}
			catch (OperationCanceledException) when (timer.IsCancellationRequested)
			{
				return (null, $"Evaluating project '{project}' with MSBuild did not finish in {seconds.ToString(CultureInfo.InvariantCulture)} seconds.");
			}
			catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception or JsonException or KeyNotFoundException)
			{
				return (null, $"Cannot run 'dotnet msbuild' for project '{project}': {ex.Message}");
			}
		}

		/// <summary>
		/// Reads every linq2db package version the assets file resolved (a project may resolve several, for example
		/// through per-target-framework conditions). The list is empty when the project does not use linq2db.
		/// </summary>
		public static (IReadOnlyList<ProjectLinq2db> Linq2db, string? Error) ReadLinq2db(string project, string assetsFile)
		{
			try
			{
				using var document = JsonDocument.Parse(File.ReadAllText(assetsFile));
				var root           = document.RootElement;

				if (!root.TryGetProperty("libraries", out var libraries) || libraries.ValueKind != JsonValueKind.Object)
					return ([], $"'{assetsFile}' is not a NuGet assets file.");

				var folders = new List<string>();

				if (root.TryGetProperty("packageFolders", out var packageFolders) && packageFolders.ValueKind == JsonValueKind.Object)
					folders.AddRange(packageFolders.EnumerateObject().Select(static p => p.Name));

				var result = new List<ProjectLinq2db>();

				foreach (var library in libraries.EnumerateObject())
				{
					var slash = library.Name.IndexOf('/', StringComparison.Ordinal);

					if (slash < 0 || !string.Equals(library.Name.Substring(0, slash), "linq2db", StringComparison.OrdinalIgnoreCase))
						continue;

					if (library.Value.TryGetProperty("type", out var type) && !string.Equals(type.GetString(), "package", StringComparison.Ordinal))
						continue;

					var version = library.Name.Substring(slash + 1);
					var path    = library.Value.TryGetProperty("path", out var pathValue) ? pathValue.GetString() : null;

					result.Add(new ProjectLinq2db(project, version, folders.Select(f => Path.Combine(f, path ?? library.Name)).ToList()));
				}

				return (result, null);
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				return ([], $"Cannot read assets file '{assetsFile}': {ex.Message}");
			}
		}

		private static string GetDotnetHost(ICliEnvironment environment)
		{
			var fromEnvironment = environment.GetEnvironmentVariable("DOTNET_HOST_PATH");

			if (!string.IsNullOrEmpty(fromEnvironment))
				return fromEnvironment;

			var processPath = Environment.ProcessPath;

			if (processPath != null && string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
				return processPath;

			return "dotnet";
		}
	}
}
