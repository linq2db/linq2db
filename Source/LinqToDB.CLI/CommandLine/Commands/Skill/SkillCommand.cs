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
		private const string InstallOperation = "install";

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
			"overwrite files that were edited locally or that this tool did not install",
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
				"[install] [options]",
				"print agent instructions in Markdown format, or install the linq2db agent skills into a repository",
				[
					new("dotnet linq2db skill",                                              "prints the linq2db CLI skill as Markdown"),
					new("dotnet linq2db skill install",                                      "installs the linq2db and linq2db-cli skills into .agents/skills and .claude/skills of the current repository"),
					new("dotnet linq2db skill install --project src/App/App.csproj",        "takes the library skill from the linq2db version that project references"),
					new("dotnet linq2db skill install --check",                              "exits with a non-zero code when installed skills are missing, stale or edited"),
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

			if (unknownArgs.Count > 1 || (unknownArgs.Count == 1 && !string.Equals(unknownArgs.Single(), InstallOperation, StringComparison.Ordinal)))
			{
				await environment.Error.WriteLineAsync($"Command '{Name}' doesn't accept arguments. Use '{Name} {InstallOperation}' to install the skills.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			if (unknownArgs.Count == 0)
			{
				await environment.Error.WriteLineAsync($"Options of command '{Name}' are supported only with '{InstallOperation}'.");
				return StatusCodes.INVALID_ARGUMENTS;
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
			var           plans = new List<SkillInstallPlan>();

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

				bundles = [library, SkillSource.GetCliSkill()];

				foreach (var skillRoot in _skillRoots)
				{
					foreach (var bundle in bundles)
						plans.Add(SkillInstaller.Analyze(bundle, root, Path.Combine(root, skillRoot.Replace('/', Path.DirectorySeparatorChar), bundle.Name)));
				}
			}
			catch (Exception ex) when (ex is InvalidDataException or InvalidOperationException)
			{
				await environment.Error.WriteLineAsync(ex.Message);
				return StatusCodes.EXPECTED_ERROR;
			}

			await environment.Out.WriteLineAsync($"linq2db skill: {library.Origin}; linq2db-cli skill: {bundles[1].Origin}.");

			return check
				? await Check(environment, root, plans)
				: await Apply(environment, root, plans, force);
		}

		private static async Task<int> Check(ICliEnvironment environment, string root, List<SkillInstallPlan> plans)
		{
			var failed = false;

			foreach (var plan in plans)
			{
				var relative = Path.GetRelativePath(root, plan.Directory);

				if (plan.State == SkillInstallState.UpToDate)
				{
					await environment.Out.WriteLineAsync($"  up to date: {relative}");
				}
				else
				{
					failed = true;
					await environment.Error.WriteLineAsync($"  {plan.State.ToString().ToLowerInvariant()}: {relative}: {plan.Reason}");
				}
			}

			if (failed)
			{
				await environment.Error.WriteLineAsync("Installed skills are not current. Run 'dotnet linq2db skill install' to update them.");
				return StatusCodes.EXPECTED_ERROR;
			}

			return StatusCodes.SUCCESS;
		}

		private static async Task<int> Apply(ICliEnvironment environment, string root, List<SkillInstallPlan> plans, bool force)
		{
			var conflicts = plans.SelectMany(static p => p.Conflicts).ToList();

			if (conflicts.Count > 0 && !force)
			{
				await environment.Error.WriteLineAsync("Nothing was installed; these paths were not installed by this tool or were edited locally:");

				foreach (var conflict in conflicts)
					await environment.Error.WriteLineAsync($"  {conflict}");

				await environment.Error.WriteLineAsync("Use '--force' to overwrite them.");
				return StatusCodes.EXPECTED_ERROR;
			}

			foreach (var plan in plans)
			{
				var relative = Path.GetRelativePath(root, plan.Directory);

				if (plan.State == SkillInstallState.UpToDate)
				{
					await environment.Out.WriteLineAsync($"  up to date: {relative}");
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
			}

			await PrintPointerHint(environment, root);

			return StatusCodes.SUCCESS;
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
