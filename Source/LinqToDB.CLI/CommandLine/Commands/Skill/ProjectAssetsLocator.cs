using System;
using System.Collections.Generic;
using System.Diagnostics;
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
		/// </summary>
		public static async Task<(string? AssetsFile, string? Error)> GetAssetsFile(string project, CancellationToken cancellationToken)
		{
			var startInfo = new ProcessStartInfo(GetDotnetHost())
			{
				RedirectStandardOutput = true,
				RedirectStandardError  = true,
				UseShellExecute        = false,
			};

			startInfo.ArgumentList.Add("msbuild");
			startInfo.ArgumentList.Add(project);
			startInfo.ArgumentList.Add("-getProperty:ProjectAssetsFile");
			startInfo.ArgumentList.Add("-nologo");

			try
			{
				using var process = Process.Start(startInfo) ?? throw new InvalidOperationException("Cannot start 'dotnet msbuild'.");

				var texts = await Task.WhenAll(
					process.StandardOutput.ReadToEndAsync(cancellationToken),
					process.StandardError .ReadToEndAsync(cancellationToken)).ConfigureAwait(false);

				await process.WaitForExitAsync(cancellationToken).ConfigureAwait(false);

				var output = texts[0].Trim();

				if (process.ExitCode != 0)
				{
					var details = (output + Environment.NewLine + texts[1]).Trim();
					return (null, $"Cannot evaluate project '{project}' with MSBuild: {details}");
				}

				return output.Length == 0
					? (null, $"MSBuild did not report a project assets file for '{project}'.")
					: (output, null);
			}
			catch (Exception ex) when (ex is InvalidOperationException or System.ComponentModel.Win32Exception)
			{
				return (null, $"Cannot run 'dotnet msbuild' for project '{project}': {ex.Message}");
			}
		}

		/// <summary>
		/// Reads the resolved linq2db package from an assets file. Returns <see langword="null"/> as the package when the
		/// project does not use linq2db.
		/// </summary>
		public static (ProjectLinq2db? Linq2db, string? Error) ReadLinq2db(string project, string assetsFile)
		{
			try
			{
				using var document = JsonDocument.Parse(File.ReadAllText(assetsFile));
				var root           = document.RootElement;

				if (!root.TryGetProperty("libraries", out var libraries) || libraries.ValueKind != JsonValueKind.Object)
					return (null, $"'{assetsFile}' is not a NuGet assets file.");

				var folders = new List<string>();

				if (root.TryGetProperty("packageFolders", out var packageFolders) && packageFolders.ValueKind == JsonValueKind.Object)
					folders.AddRange(packageFolders.EnumerateObject().Select(static p => p.Name));

				foreach (var library in libraries.EnumerateObject())
				{
					var slash = library.Name.IndexOf('/', StringComparison.Ordinal);

					if (slash < 0 || !string.Equals(library.Name.Substring(0, slash), "linq2db", StringComparison.OrdinalIgnoreCase))
						continue;

					if (library.Value.TryGetProperty("type", out var type) && !string.Equals(type.GetString(), "package", StringComparison.Ordinal))
						continue;

					var version = library.Name.Substring(slash + 1);
					var path    = library.Value.TryGetProperty("path", out var pathValue) ? pathValue.GetString() : null;

					return (new ProjectLinq2db(
						project,
						version,
						folders.Select(f => Path.Combine(f, path ?? library.Name)).ToList()), null);
				}

				return (null, null);
			}
			catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
			{
				return (null, $"Cannot read assets file '{assetsFile}': {ex.Message}");
			}
		}

		private static string GetDotnetHost()
		{
			var fromEnvironment = Environment.GetEnvironmentVariable("DOTNET_HOST_PATH");

			if (!string.IsNullOrEmpty(fromEnvironment))
				return fromEnvironment;

			var processPath = Environment.ProcessPath;

			if (processPath != null && string.Equals(Path.GetFileNameWithoutExtension(processPath), "dotnet", StringComparison.OrdinalIgnoreCase))
				return processPath;

			return "dotnet";
		}
	}
}
