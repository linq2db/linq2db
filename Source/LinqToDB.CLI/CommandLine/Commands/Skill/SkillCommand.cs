using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Skill
{
	/// <summary>
	/// Prints agent-oriented Markdown instructions for linq2db CLI usage, or installs the linq2db agent skills into a repository.
	/// </summary>
	internal sealed class SkillCommand : CliCommand
	{
		private const string InstallOperation   = "install";
		private const string UninstallOperation = "uninstall";

		private static readonly string[] _skillRoots = [".agents/skills", ".claude/skills"];

		private static readonly OptionCategory _installOptions = new(1, "Install", "Options of 'skill install'", "install");

		private static readonly CliOption _project = new StringCliOption(
			"project",
			null,
			false,
			false,
			"project file or directory whose linq2db package version selects the library skill (default: current directory)",
			"The linq2db version is read from the project's NuGet assets file, so the project must be restored. A directory is searched for project files; when they reference different linq2db versions the command fails and lists them. When no restored project references linq2db, the copy of the skill embedded in the tool is installed.");

		private static readonly CliOption _root = new StringCliOption(
			"root",
			null,
			false,
			false,
			"repository root that receives .agents/skills and .claude/skills (default: the git work tree root above the project, else the project directory)");

		private static readonly CliOption _check = new BooleanCliOption(
			"check",
			null,
			false,
			"write nothing; exit with a non-zero code when installed skills are missing, stale or edited (for CI)",
			null,
			null,
			null,
			false,
			false);

		private static readonly CliOption _force = new BooleanCliOption(
			"force",
			null,
			false,
			"overwrite files that were edited locally or that this tool did not install (folders managed by dotnet-package-skills are never overwritten); with 'uninstall', also removes installed files that were edited locally",
			null,
			null,
			null,
			false,
			false);

		public static CliCommand Instance { get; } = new SkillCommand();

		private SkillCommand()
			: base(
				"skill",
				true,
				false,
				"[install|uninstall] [options]",
				"print agent instructions in Markdown format, or install or remove the linq2db agent skills of a repository",
				[
					new("dotnet linq2db skill",                                              "prints the linq2db CLI skill as Markdown"),
					new("dotnet linq2db skill install",                                      "installs the linq2db and linq2db-cli skills into .agents/skills and .claude/skills of the current repository"),
					new("dotnet linq2db skill install --project src/App/App.csproj",        "takes the library skill from the linq2db version that project references"),
					new("dotnet linq2db skill install --check",                              "exits with a non-zero code when installed skills are missing, stale or edited"),
					new("dotnet linq2db skill uninstall",                                    "removes the files that 'skill install' wrote (edited and added files are kept); folders managed by dotnet-package-skills are never touched"),
				],
				true)
		{
			AddOption(_installOptions, _project);
			AddOption(_installOptions, _root);
			AddOption(_installOptions, _check);
			AddOption(_installOptions, _force);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			options.Remove(_project, out var projectValue);
			options.Remove(_root,    out var rootValue);
			options.Remove(_check,   out var checkValue);
			options.Remove(_force,   out var forceValue);

			if (options.Count > 0)
				throw new InvalidOperationException($"Not all options handled by {Name} command");

			if (unknownArgs.Count == 0 && projectValue == null && rootValue == null && checkValue == null && forceValue == null)
			{
				var markdown = SkillResource.ReadMarkdown();

				await environment.Out.WriteAsync(markdown.AsMemory(), cancellationToken);
				return StatusCodes.SUCCESS;
			}

			var isUninstall = unknownArgs.Count == 1 && string.Equals(unknownArgs.Single(), UninstallOperation, StringComparison.Ordinal);

			if (unknownArgs.Count > 1 || (unknownArgs.Count == 1 && !isUninstall && !string.Equals(unknownArgs.Single(), InstallOperation, StringComparison.Ordinal)))
			{
				await environment.Error.WriteLineAsync($"Command '{Name}' doesn't accept arguments. Use '{Name} {InstallOperation}' to install the skills or '{Name} {UninstallOperation}' to remove them.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			if (unknownArgs.Count == 0)
			{
				await environment.Error.WriteLineAsync($"Options of command '{Name}' are supported only with '{InstallOperation}' or '{UninstallOperation}'.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			if (isUninstall)
			{
				if (checkValue != null)
				{
					await environment.Error.WriteLineAsync($"Option '--check' is not supported with '{UninstallOperation}'.");
					return StatusCodes.INVALID_ARGUMENTS;
				}

				return await Uninstall(environment, (string?)projectValue, (string?)rootValue, (bool?)forceValue ?? false);
			}

			return await Install(
				environment,
				(string?)projectValue,
				(string?)rootValue,
				(bool?)checkValue ?? false,
				(bool?)forceValue ?? false,
				cancellationToken);
		}

		private static async Task<int> Install(ICliEnvironment environment, string? projectOption, string? rootOption, bool check, bool force, CancellationToken cancellationToken)
		{
			if (check && force)
			{
				await environment.Error.WriteLineAsync("Options '--check' and '--force' cannot be used together.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			var projectPath = Path.GetFullPath(projectOption ?? Directory.GetCurrentDirectory());
			var isDirectory = Directory.Exists(projectPath);

			if (!isDirectory && !File.Exists(projectPath))
			{
				await environment.Error.WriteLineAsync($"Project '{projectPath}' was not found.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			if (!isDirectory && !ProjectAssetsLocator.IsProjectFile(projectPath))
			{
				await environment.Error.WriteLineAsync($"'{projectPath}' is not a project file (.csproj, .fsproj, .vbproj). Pass a project file or a directory.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			var projectDirectory = isDirectory ? projectPath : Path.GetDirectoryName(projectPath)!;
			var root             = rootOption != null ? Path.GetFullPath(rootOption) : FindRepositoryRoot(projectDirectory);

			// which linq2db does the project use
			var projects = isDirectory ? ProjectAssetsLocator.FindProjects(projectPath) : [projectPath];
			var found    = new List<ProjectLinq2db>();
			var restored = 0;

			foreach (var project in projects)
			{
				var (assetsFile, assetsError) = await ProjectAssetsLocator.GetAssetsFile(environment, project, cancellationToken);

				if (assetsFile == null)
				{
					await environment.Error.WriteLineAsync(assetsError);
					return StatusCodes.EXPECTED_ERROR;
				}

				if (!File.Exists(assetsFile))
					continue;

				restored++;

				var (resolved, readError) = ProjectAssetsLocator.ReadLinq2db(project, assetsFile);

				if (readError != null)
				{
					await environment.Error.WriteLineAsync(readError);
					return StatusCodes.EXPECTED_ERROR;
				}

				found.AddRange(resolved);
			}

			if (projects.Count > 0 && restored == 0)
			{
				await environment.Error.WriteLineAsync(
					$"No project assets file was found for {string.Join(", ", projects.Select(p => $"'{p}'"))}. Run 'dotnet restore' first.");
				return StatusCodes.EXPECTED_ERROR;
			}

			var versions = found.Select(static p => p.Version).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

			if (versions.Count > 1)
			{
				await environment.Error.WriteLineAsync("Projects reference different linq2db versions; pass the one to use with '--project':");

				foreach (var project in found)
					await environment.Error.WriteLineAsync($"  {project.Project}: {project.Version}");

				return StatusCodes.EXPECTED_ERROR;
			}

			SkillBundle   library;
			SkillBundle[] bundles;
			var           plans   = new List<SkillInstallPlan>();
			var           skipped = 0;

			try
			{
				// where the library skill comes from
				var         embedded = SkillSource.GetEmbeddedLibrarySkill();

				if (found.Count == 0)
				{
					await environment.Error.WriteLineAsync("Warning: no restored project referencing linq2db was found; using the linq2db skill embedded in the tool.");
					library = embedded;
				}
				else
				{
					var linq2db = found[0];
					var package = SkillSource.TryGetPackageLibrarySkill(linq2db);

					if (package != null)
					{
						library = package;
					}
					else
					{
						library = embedded;
						await environment.Error.WriteLineAsync(
							$"Warning: linq2db {linq2db.Version} package has no skill (it is not in the NuGet cache, or the version predates it); using the linq2db skill embedded in the tool.");
					}

					if (!string.Equals(library.Version, linq2db.Version, StringComparison.OrdinalIgnoreCase))
						await environment.Error.WriteLineAsync($"Warning: the project uses linq2db {linq2db.Version}, but the installed skill describes linq2db {library.Version}.");
				}

				// the linq2db-cli skill always comes from the copy embedded in the tool, never from the package: it describes
				// the options of the tool that is running, and it is the only copy that cannot be older than that tool
				bundles = [library, SkillSource.GetCliSkill()];

				var (destinations, linkSkipped) = await ResolveDestinations(environment, root);

				skipped += linkSkipped;

				foreach (var destination in destinations)
				{
					var relative         = Path.GetRelativePath(root, destination);
					var destinationPlans = new List<SkillInstallPlan>();

					try
					{
						foreach (var bundle in bundles)
							destinationPlans.Add(SkillInstaller.Analyze(bundle, root, Path.Combine(destination, bundle.Name)));

						plans.AddRange(destinationPlans);
					}
					catch (InvalidOperationException ex)
					{
						// a link inside the destination: that destination is skipped, the others are still handled
						await environment.Error.WriteLineAsync($"  skipped: {relative}: {ex.Message}");
						skipped++;
					}
				}
			}
			catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
			{
				await environment.Error.WriteLineAsync(ex.Message);
				return StatusCodes.EXPECTED_ERROR;
			}

			await environment.Out.WriteLineAsync($"linq2db skill: {library.Origin}; linq2db-cli skill: {bundles[1].Origin}.");

			return check
				? await Check(environment, root, plans, found.Count > 0 ? found[0].Version : null, skipped)
				: await Apply(environment, root, plans, force, skipped);
		}

		private static async Task<int> Uninstall(ICliEnvironment environment, string? projectOption, string? rootOption, bool force)
		{
			var projectPath = Path.GetFullPath(projectOption ?? Directory.GetCurrentDirectory());

			if (!Directory.Exists(projectPath) && !File.Exists(projectPath))
			{
				await environment.Error.WriteLineAsync($"Project '{projectPath}' was not found.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			var projectDirectory = Directory.Exists(projectPath) ? projectPath : Path.GetDirectoryName(projectPath)!;
			var root             = rootOption != null ? Path.GetFullPath(rootOption) : FindRepositoryRoot(projectDirectory);

			var (destinations, skipped) = await ResolveDestinations(environment, root);
			var plans                   = new List<SkillUninstallPlan>();

			foreach (var destination in destinations)
			{
				var destinationPlans = new List<SkillUninstallPlan>();

				try
				{
					foreach (var name in new[] { SkillSource.LibrarySkillName, SkillSource.CliSkillName })
						destinationPlans.Add(SkillUninstaller.Analyze(name, root, Path.Combine(destination, name)));

					plans.AddRange(destinationPlans);
				}
				catch (InvalidOperationException ex)
				{
					// a link inside the destination: that destination is skipped, the others are still handled
					await environment.Error.WriteLineAsync($"  skipped: {Path.GetRelativePath(root, destination)}: {ex.Message}");
					skipped++;
				}
			}

			var kept      = 0;
			var installed = false;

			foreach (var plan in plans)
			{
				var relative = Path.GetRelativePath(root, plan.Directory);

				switch (plan.State)
				{
					case SkillUninstallState.NotInstalled:
						break;
					case SkillUninstallState.OwnershipUnknown:
						await environment.Error.WriteLineAsync($"Warning: {relative}: {plan.Reason}.");
						break;
					case SkillUninstallState.ManagedExternally:
					case SkillUninstallState.NotInstalledByTool:
						await environment.Out.WriteLineAsync($"  {relative}: {plan.Reason}");
						break;
					default:
					{
						installed = true;

						int left;

						try
						{
							left = SkillUninstaller.Apply(plan, root, force);
						}
						catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
						{
							await environment.Error.WriteLineAsync($"Cannot remove '{plan.Directory}': {ex.Message}");
							return StatusCodes.EXPECTED_ERROR;
						}

						if (left == 0)
						{
							await environment.Out.WriteLineAsync($"  removed: {relative}");
						}
						else
						{
							kept += left;
							await environment.Error.WriteLineAsync($"  kept {relative}: {left.ToString(System.Globalization.CultureInfo.InvariantCulture)} file(s) changed or added; remove by hand");
						}

						break;
					}
				}
			}

			if (!installed)
				await environment.Out.WriteLineAsync("Nothing to uninstall.");

			// edited or added files were left in place, or a destination was refused: not a clean result
			return kept > 0 || skipped > 0 ? StatusCodes.EXPECTED_ERROR : StatusCodes.SUCCESS;
		}

		/// <summary>
		/// The destinations that can be written: a destination root that is a link to the other destination is handled there, and
		/// one that links anywhere else (or nowhere) is reported and skipped.
		/// </summary>
		private static async Task<(List<string> Destinations, int Skipped)> ResolveDestinations(ICliEnvironment environment, string root)
		{
			var all     = _skillRoots.Select(r => Path.Combine(root, r.Replace('/', Path.DirectorySeparatorChar))).ToList();
			var result  = new List<string>();
			var skipped = 0;

			foreach (var destination in all)
			{
				var relative = Path.GetRelativePath(root, destination);
				var link     = SkillPath.FindLink(root, destination);

				if (link == null)
				{
					result.Add(destination);
					continue;
				}

				// compared as fully resolved paths, so a repository reached through a linked ancestor still matches
				var real = SkillPath.GetRealPath(destination);

				// one tool directory aliased to the other is common: the real directory is processed once, under its own name
				if (Directory.Exists(real) && all.Exists(d => !string.Equals(d, destination, StringComparison.Ordinal)
					&& SkillPath.FindLink(root, d) == null
					&& SkillPath.Comparer.Equals(real, SkillPath.GetRealPath(d))))
				{
					await environment.Out.WriteLineAsync($"  {relative}: link to {Path.GetRelativePath(SkillPath.GetRealPath(root), real)}; handled there");
				}
				else
				{
					var why = !Directory.Exists(real)
						? $"'{link}' is a dangling or unresolvable link"
						: $"'{link}' resolves to '{real}', which is not another skill directory of this tool";

					await environment.Error.WriteLineAsync($"  skipped: {relative}: {why}; refusing to write through it.");
					skipped++;
				}
			}

			return (result, skipped);
		}

		private static async Task<int> Check(ICliEnvironment environment, string root, List<SkillInstallPlan> plans, string? resolvedVersion, int skipped)
		{
			var failed = skipped > 0;

			foreach (var plan in plans)
			{
				var relative = Path.GetRelativePath(root, plan.Directory);

				if (plan.State == SkillInstallState.UpToDate)
				{
					await environment.Out.WriteLineAsync($"  up to date: {relative}");
				}
				else if (plan.State == SkillInstallState.ManagedExternally)
				{
					if (plan.ExternalMissing)
					{
						failed = true;
						await environment.Error.WriteLineAsync($"  missing: {relative} (managed by dotnet-package-skills); run 'dotnet-package-skills install'");
					}
					else if (plan.OwnershipUnknown)
					{
						failed = true;
						await environment.Error.WriteLineAsync(
							$"  ownership unknown: {relative}; {Path.Combine(Path.GetDirectoryName(relative)!, PackageSkillsManifest.FileName)} could not be read; fix or delete it, or run 'dotnet-package-skills install' to see its error");
					}
					// not ours to update, but a copy of another linq2db version is as stale as one of ours
					else if (plan.ExternalOwner is { IsLinq2db: true } owner && resolvedVersion != null && !SameVersion(owner.Version, resolvedVersion))
					{
						failed = true;
						await environment.Error.WriteLineAsync(
							$"  stale: {relative}: managed by dotnet-package-skills (linq2db {owner.Version}), the project uses linq2db {resolvedVersion}; run 'dotnet-package-skills install' to update it");
					}
					else
					{
						await environment.Out.WriteLineAsync($"  {relative}: {plan.Reason}");
					}
				}
				else
				{
					failed = true;
					await environment.Error.WriteLineAsync($"  {plan.State.ToString().ToLowerInvariant()}: {relative}: {plan.Reason}");
				}
			}

			if (failed)
			{
				await environment.Error.WriteLineAsync("Installed skills are not current. Run 'dotnet linq2db skill install' (or 'dotnet-package-skills install' for the ones it manages) to update them.");
				return StatusCodes.EXPECTED_ERROR;
			}

			return StatusCodes.SUCCESS;
		}

		private static async Task<int> Apply(ICliEnvironment environment, string root, List<SkillInstallPlan> plans, bool force, int skipped)
		{
			var conflicts = plans.SelectMany(static p => p.Conflicts).ToList();

			if (conflicts.Count > 0 && !force)
			{
				await environment.Error.WriteLineAsync("Nothing was installed; these paths were not installed by this tool or were edited locally:");

				foreach (var conflict in conflicts)
					await environment.Error.WriteLineAsync($"  {conflict}");

				await environment.Error.WriteLineAsync("Use '--force' to overwrite them (folders managed by dotnet-package-skills are never overwritten).");
				return StatusCodes.EXPECTED_ERROR;
			}

			var changed = new List<SkillInstallPlan>();

			foreach (var plan in plans)
			{
				var relative = Path.GetRelativePath(root, plan.Directory);

				if (plan.State == SkillInstallState.UpToDate)
				{
					await environment.Out.WriteLineAsync($"  up to date: {relative}");
					continue;
				}

				if (plan.State == SkillInstallState.ManagedExternally)
				{
					// neither a conflict nor something '--force' may override
					if (plan.ExternalMissing)
						await environment.Out.WriteLineAsync($"  missing: {relative} (managed by dotnet-package-skills); run 'dotnet-package-skills install'");
					else if (plan.OwnershipUnknown)
						await environment.Error.WriteLineAsync($"Warning: {relative}: {plan.Reason}.");
					else
						await environment.Out.WriteLineAsync($"  {relative}: {plan.Reason}");

					continue;
				}

				try
				{
					SkillInstaller.Apply(plan);
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException)
				{
					await environment.Error.WriteLineAsync($"Cannot write '{plan.Directory}': {ex.Message}");
					return StatusCodes.EXPECTED_ERROR;
				}

				var verb = plan.State == SkillInstallState.Missing ? "installed" : "updated";
				await environment.Out.WriteLineAsync($"  {verb}: {relative} ({plan.Bundle.Files.Count} file(s), version {plan.Bundle.Version})");
				changed.Add(plan);
			}

			await PrintHandOff(environment, root, changed);

			// nothing was installed or checked when every destination was refused
			if (plans.Count > 0)
				await PrintPointerHint(environment, root);

			// a destination that was refused is not a success, although the others were handled
			return skipped > 0 ? StatusCodes.EXPECTED_ERROR : StatusCodes.SUCCESS;
		}

		/// <summary>
		/// A destination that also holds a record of dotnet-package-skills: it skips a folder it did not install, so say once per
		/// destination, for the folders written by this run, how to let it take over.
		/// </summary>
		private static async Task PrintHandOff(ICliEnvironment environment, string root, List<SkillInstallPlan> changed)
		{
			foreach (var destination in changed.Where(static p => p.OtherToolPresent).GroupBy(static p => Path.GetDirectoryName(p.Directory)!, SkillPath.Comparer))
			{
				var folders = destination.Select(p => Path.GetRelativePath(root, p.Directory)).ToList();

				if (folders.Count == 1)
				{
					await environment.Out.WriteLineAsync(
						"    dotnet-package-skills will skip this folder. To let it manage the skill, run 'dotnet linq2db skill uninstall', then 'dotnet-package-skills install'.");
				}
				else
				{
					await environment.Out.WriteLineAsync(
						$"    dotnet-package-skills will skip these folders: {string.Join(", ", folders.Select(static f => $"'{f}'"))}. To let it manage them, run 'dotnet linq2db skill uninstall', then 'dotnet-package-skills install'.");
				}
			}
		}

		/// <summary>
		/// Package versions are compared the way NuGet does: the numeric part is padded to three components (a fourth is kept
		/// unless it is zero) with leading zeros ignored, build metadata (<c>+...</c>) is ignored, and the prerelease label is
		/// compared without regard to case.
		/// </summary>
		internal static bool SameVersion(string left, string right)
		{
			static string Normalize(string version)
			{
				var plus = version.IndexOf('+', StringComparison.Ordinal);

				if (plus >= 0)
					version = version.Substring(0, plus);

				var dash   = version.IndexOf('-', StringComparison.Ordinal);
				var number = dash >= 0 ? version.Substring(0, dash) : version;
				var label  = dash >= 0 ? version.Substring(dash)    : string.Empty;
				var parts  = number.Split('.');
				var values = new List<string>();

				foreach (var part in parts)
				{
					if (!System.Numerics.BigInteger.TryParse(part, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out var value))
						return version.ToLowerInvariant();

					values.Add(value.ToString(System.Globalization.CultureInfo.InvariantCulture));
				}

				while (values.Count < 3)
					values.Add("0");

				if (values.Count == 4 && string.Equals(values[3], "0", StringComparison.Ordinal))
					values.RemoveAt(3);

				return string.Join('.', values.ToArray()) + label.ToLowerInvariant();
			}

			return string.Equals(Normalize(left), Normalize(right), StringComparison.Ordinal);
		}

		/// <summary>
		/// The tool never edits AGENTS.md or CLAUDE.md; when neither points at the skill it prints a block to paste.
		/// </summary>
		private static async Task PrintPointerHint(ICliEnvironment environment, string root)
		{
			foreach (var name in new[] { "AGENTS.md", "CLAUDE.md" })
			{
				var path = Path.Combine(root, name);

				try
				{
					if (File.Exists(path) && (await File.ReadAllTextAsync(path)).Contains("skills/linq2db", StringComparison.OrdinalIgnoreCase))
						return;
				}
				catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
				{
					// an unreadable file is as good as one without the pointer
				}
			}

			await environment.Out.WriteLineAsync();
			await environment.Out.WriteLineAsync("Neither AGENTS.md nor CLAUDE.md mentions the skill. Consider adding this to AGENTS.md:");
			await environment.Out.WriteLineAsync();
			await environment.Out.WriteLineAsync("## linq2db");
			await environment.Out.WriteLineAsync();
			await environment.Out.WriteLineAsync("Before writing or reviewing code that uses linq2db, read `.agents/skills/linq2db/SKILL.md`.");
			await environment.Out.WriteLineAsync("To inspect a database with the `dotnet linq2db` tool, read `.agents/skills/linq2db-cli/SKILL.md`.");
		}

		private static string FindRepositoryRoot(string directory)
		{
			for (var current = new DirectoryInfo(directory); current != null; current = current.Parent)
			{
				// .git is a directory in a clone and a file in a worktree or submodule
				if (Directory.Exists(Path.Combine(current.FullName, ".git")) || File.Exists(Path.Combine(current.FullName, ".git")))
					return current.FullName;
			}

			return directory;
		}
	}
}
