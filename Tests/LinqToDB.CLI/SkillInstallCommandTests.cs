using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

using LinqToDB.CommandLine;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Tests of <c>skill install</c>: temporary repositories with a project, a hand-made NuGet assets file and a
	/// fake package folder; MSBuild is asked for the assets file location exactly as in real use.
	/// </summary>
	[TestFixture]
	public sealed class SkillInstallCommandTests
	{
		private const string LibraryManifestFile = ".linq2db-skill.json";

		private string _directory = null!;

		[SetUp]
		public void SetUp()
		{
			_directory = Path.Combine(Path.GetTempPath(), "l2db-skill-" + Guid.NewGuid().ToString("N"));
			Directory.CreateDirectory(_directory);
		}

		[TearDown]
		public void TearDown()
		{
			try
			{
				Directory.Delete(_directory, true);
			}
			catch (IOException)
			{
			}
		}

		[Test]
		public async Task InstallCopiesBothSkillsIntoBothRoots()
		{
			var repo   = CreateRepository("5.0.0");
			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0, result.Error);
				result.Output.  ShouldContain("NuGet package linq2db 5.0.0");

				foreach (var root in new[] { ".agents/skills", ".claude/skills" })
				{
					// bytes are copied verbatim, so the packaged CRLF stays
					File.ReadAllBytes(Path.Combine(repo.Root, root, "linq2db", "SKILL.md"))
						.ShouldBe(File.ReadAllBytes(Path.Combine(repo.Package, "skills", "linq2db", "SKILL.md")));
					File.ReadAllText(Path.Combine(repo.Root, root, "linq2db", "docs", "crud", "crud-update.md")).ShouldContain("update guide");
					File.Exists(Path.Combine(repo.Root, root, "linq2db-cli", "SKILL.md")).ShouldBeTrue();
				}

				var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(repo.Root, ".agents", "skills", "linq2db", LibraryManifestFile))).RootElement;
				manifest.GetProperty("skill").  GetString().ShouldBe("linq2db");
				manifest.GetProperty("version").GetString().ShouldBe("5.0.0");
				manifest.GetProperty("source"). GetString().ShouldBe("package");
				manifest.GetProperty("files").  EnumerateObject().Select(p => p.Name).ShouldBe(["SKILL.md", "docs/crud/crud-update.md"], ignoreOrder: true);

				// the instruction files are never edited; a pointer block is printed instead
				File.Exists(Path.Combine(repo.Root, "AGENTS.md")).ShouldBeFalse();
				result.Output.ShouldContain("## linq2db");
				result.Output.ShouldContain(".agents/skills/linq2db/SKILL.md");
			}
		}

		[Test]
		public async Task InstalledCliSkillHasValidFrontmatter()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			var text        = File.ReadAllText(Path.Combine(repo.Root, ".claude", "skills", "linq2db-cli", "SKILL.md"));
			var frontmatter = ReadFrontmatter(text);

			using (Assert.EnterMultipleScope())
			{
				frontmatter["name"].       ShouldBe("linq2db-cli");
				frontmatter["description"].Length.ShouldBeInRange(50, 1024);
				frontmatter["description"].ShouldContain("linq2db");
				text.ShouldContain("# linq2db CLI Agent Skill");
			}
		}

		[Test]
		public async Task PointerBlockIsNotPrintedWhenAgentsFileMentionsSkill()
		{
			var repo = CreateRepository("5.0.0");
			File.WriteAllText(Path.Combine(repo.Root, "AGENTS.md"), "Use .agents/skills/linq2db/SKILL.md\n");

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0, result.Error);
				result.Output.  ShouldNotContain("## linq2db");
				File.ReadAllText(Path.Combine(repo.Root, "AGENTS.md")).ShouldBe("Use .agents/skills/linq2db/SKILL.md\n");
			}
		}

		[Test]
		public async Task ReinstallIsIdempotent()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			var skill  = Path.Combine(repo.Root, ".agents", "skills", "linq2db", "SKILL.md");
			var before = Snapshot(repo.Root);
			File.SetLastWriteTimeUtc(skill, new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0, result.Error);
				result.Output.  ShouldContain("up to date");
				result.Output.  ShouldNotContain("installed:");
				Snapshot(repo.Root).ShouldBe(before);
				File.GetLastWriteTimeUtc(skill).ShouldBe(new DateTime(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc));
			}
		}

		[Test]
		public async Task NewPackageVersionReplacesUnchangedFilesAndRemovesDroppedOnes()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			var next = CreateRepository("5.1.0", repo);
			File.Delete(Path.Combine(next.Package, "skills", "linq2db", "docs", "crud", "crud-update.md"));
			File.WriteAllText(Path.Combine(next.Package, "skills", "linq2db", "docs", "new.md"), "new guide\r\n");

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0, result.Error);
				result.Output.  ShouldContain("updated");

				foreach (var root in new[] { ".agents/skills", ".claude/skills" })
				{
					var skill = Path.Combine(repo.Root, root, "linq2db");
					File.Exists(Path.Combine(skill, "docs", "new.md")).ShouldBeTrue();
					File.Exists(Path.Combine(skill, "docs", "crud", "crud-update.md")).ShouldBeFalse();
					Directory.Exists(Path.Combine(skill, "docs", "crud")).ShouldBeFalse();
					File.ReadAllText(Path.Combine(skill, LibraryManifestFile)).ShouldContain("5.1.0");
				}
			}
		}

		[Test]
		public async Task LocallyEditedFileIsRefusedWithoutForce()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			var edited = Path.Combine(repo.Root, ".claude", "skills", "linq2db", "SKILL.md");
			File.AppendAllText(edited, "my own note\r\n");

			// a newer package, so that other files would change if the refusal did not stop everything
			var next = CreateRepository("5.1.0", repo);
			File.WriteAllText(Path.Combine(next.Package, "skills", "linq2db", "docs", "crud", "crud-update.md"), "changed\r\n");
			var before = Snapshot(repo.Root);

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain(edited);
				result.Error.   ShouldContain("--force");
				Snapshot(repo.Root).ShouldBe(before);
			}

			var forced = await RunCli("skill", "install", "--project", repo.Project, "--force");

			using (Assert.EnterMultipleScope())
			{
				forced.ExitCode.ShouldBe(0, forced.Error);
				File.ReadAllText(edited).ShouldNotContain("my own note");
				File.ReadAllText(Path.Combine(repo.Root, ".agents", "skills", "linq2db", "docs", "crud", "crud-update.md")).ShouldBe("changed\r\n");
			}
		}

		[Test]
		public async Task ExistingDirectoryWithoutManifestIsRefusedWithoutForce()
		{
			var repo   = CreateRepository("5.0.0");
			var mine   = Path.Combine(repo.Root, ".agents", "skills", "linq2db");
			var extra  = Path.Combine(mine, "notes.md");

			Directory.CreateDirectory(mine);
			File.WriteAllText(extra, "my notes\n");

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain(mine);
				Directory.Exists(Path.Combine(repo.Root, ".claude")).ShouldBeFalse();
			}

			var forced = await RunCli("skill", "install", "--project", repo.Project, "--force");

			using (Assert.EnterMultipleScope())
			{
				forced.ExitCode.ShouldBe(0, forced.Error);
				File.Exists(Path.Combine(mine, "SKILL.md")).ShouldBeTrue();
				// files the tool did not install are never touched
				File.ReadAllText(extra).ShouldBe("my notes\n");
			}
		}

		[Test]
		public async Task FileThatIsNotInManifestIsNotOverwrittenWithoutForce()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			// the next package adds a guide where the user already has a file of the same name
			var existing = Path.Combine(repo.Root, ".agents", "skills", "linq2db", "docs", "new.md");
			File.WriteAllText(existing, "the user's guide\n");

			var next = CreateRepository("5.1.0", repo);
			File.WriteAllText(Path.Combine(next.Package, "skills", "linq2db", "docs", "new.md"), "package guide\r\n");

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain(existing);
				File.ReadAllText(existing).ShouldBe("the user's guide\n");
			}
		}

		[Test]
		public async Task LineEndingNormalizationIsNotAnEdit()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			// a checkout with `* text=auto` on Linux turns the packaged CRLF files into LF
			foreach (var file in Directory.EnumerateFiles(Path.Combine(repo.Root, ".agents", "skills", "linq2db"), "*.md", SearchOption.AllDirectories))
				File.WriteAllText(file, File.ReadAllText(file).Replace("\r\n", "\n"));

			var check     = await RunCli("skill", "install", "--project", repo.Project, "--check");
			var reinstall = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				check    .ExitCode.ShouldBe(0, check.Error);
				reinstall.ExitCode.ShouldBe(0, reinstall.Error);
				File.ReadAllText(Path.Combine(repo.Root, ".agents", "skills", "linq2db", "SKILL.md")).ShouldNotContain("\r");
			}
		}

		[Test]
		public async Task CheckFailsWhenNothingIsInstalledAndWritesNothing()
		{
			var repo   = CreateRepository("5.0.0");
			var result = await RunCli("skill", "install", "--project", repo.Project, "--check");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("not installed");
				Directory.Exists(Path.Combine(repo.Root, ".agents")).ShouldBeFalse();
				Directory.Exists(Path.Combine(repo.Root, ".claude")).ShouldBeFalse();
			}
		}

		[Test]
		public async Task CheckSucceedsAfterInstallAndFailsWhenStaleOrEdited()
		{
			var repo = CreateRepository("5.0.0");
			await RunCli("skill", "install", "--project", repo.Project);

			var current = await RunCli("skill", "install", "--project", repo.Project, "--check");

			var edited = Path.Combine(repo.Root, ".agents", "skills", "linq2db-cli", "SKILL.md");
			File.AppendAllText(edited, "edit\n");
			var modified = await RunCli("skill", "install", "--project", repo.Project, "--check");
			await RunCli("skill", "install", "--project", repo.Project, "--force");

			CreateRepository("5.1.0", repo);
			var before = Snapshot(repo.Root);
			var stale  = await RunCli("skill", "install", "--project", repo.Project, "--check");

			using (Assert.EnterMultipleScope())
			{
				current .ExitCode.ShouldBe(0, current.Error);
				modified.ExitCode.ShouldBe(-3);
				modified.Error.   ShouldContain("edited locally");
				stale   .ExitCode.ShouldBe(-3);
				stale   .Error.   ShouldContain("installed version 5.0.0, current 5.1.0");
				Snapshot(repo.Root).ShouldBe(before);
			}
		}

		[Test]
		public async Task CheckAndForceCannotBeCombined()
		{
			var repo   = CreateRepository("5.0.0");
			var result = await RunCli("skill", "install", "--project", repo.Project, "--check", "--force");

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-1);
				result.Error.   ShouldContain("cannot be used together");
			}
		}

		[Test]
		public async Task PackageWithoutSkillFallsBackToEmbeddedCopyAndWarns()
		{
			var repo = CreateRepository("5.0.0", withSkill: false);
			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0, result.Error);
				result.Error.   ShouldContain("linq2db 5.0.0 package has no skill");
				result.Error.   ShouldContain("project uses linq2db 5.0.0");
				result.Output.  ShouldContain("embedded in the tool");

				var skill = Path.Combine(repo.Root, ".agents", "skills", "linq2db");
				File.ReadAllText(Path.Combine(skill, LibraryManifestFile)).ShouldContain("\"embedded\"");
				Directory.Exists(Path.Combine(skill, "docs")).ShouldBeTrue();
				ReadFrontmatter(File.ReadAllText(Path.Combine(skill, "SKILL.md")))["name"].ShouldBe("linq2db");
			}

			// the embedded copy is a stable source: a second run and --check agree with it
			var check = await RunCli("skill", "install", "--project", repo.Project, "--check");
			check.ExitCode.ShouldBe(0, check.Error);
		}

		[Test]
		public async Task ProjectWithoutAssetsFileAsksForRestore()
		{
			var repo = CreateRepository("5.0.0");
			File.Delete(repo.Assets);

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("dotnet restore");
				Directory.Exists(Path.Combine(repo.Root, ".agents")).ShouldBeFalse();
			}
		}

		[Test]
		public async Task AssetsFileIsLocatedThroughMsBuild()
		{
			var repo = CreateRepository("5.0.0", moveIntermediateOutput: true);

			File.Exists(Path.Combine(repo.ProjectDirectory, "obj", "project.assets.json")).ShouldBeFalse();

			var result = await RunCli("skill", "install", "--project", repo.Project);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(0, result.Error);
				result.Output.  ShouldContain("NuGet package linq2db 5.0.0");
			}
		}

		[Test]
		public async Task DirectoryWithProjectsOfDifferentVersionsIsAnError()
		{
			var repo = CreateRepository("5.0.0");
			var other = CreateRepository("5.1.0", repo, projectName: "Other");

			var result = await RunCli("skill", "install", "--project", repo.Root);

			using (Assert.EnterMultipleScope())
			{
				result.ExitCode.ShouldBe(-3);
				result.Error.   ShouldContain("different linq2db versions");
				result.Error.   ShouldContain(repo.Project + ": 5.0.0");
				result.Error.   ShouldContain(other.Project + ": 5.1.0");
			}
		}

		[Test]
		public async Task RootIsTheGitRootAboveTheProjectUnlessGiven()
		{
			var repo = CreateRepository("5.0.0");

			var byDefault = await RunCli("skill", "install", "--project", repo.Project);
			var custom    = Path.Combine(_directory, "elsewhere");
			var explicitRoot = await RunCli("skill", "install", "--project", repo.Project, "--root", custom);

			using (Assert.EnterMultipleScope())
			{
				byDefault   .ExitCode.ShouldBe(0, byDefault.Error);
				explicitRoot.ExitCode.ShouldBe(0, explicitRoot.Error);
				Directory.Exists(Path.Combine(repo.Root, ".agents", "skills", "linq2db")).ShouldBeTrue();
				Directory.Exists(Path.Combine(repo.ProjectDirectory, ".agents")).ShouldBeFalse();
				Directory.Exists(Path.Combine(custom, ".claude", "skills", "linq2db-cli")).ShouldBeTrue();
			}
		}

		private Repository CreateRepository(string version, Repository? existing = null, bool withSkill = true, bool moveIntermediateOutput = false, string projectName = "App")
		{
			var root     = existing?.Root ?? Path.Combine(_directory, "repo");
			var packages = Path.Combine(_directory, "packages");
			var package  = Path.Combine(packages, "linq2db", version);
			var project  = Path.Combine(root, "src", projectName);

			Directory.CreateDirectory(Path.Combine(root, ".git"));
			Directory.CreateDirectory(project);

			var extensions = moveIntermediateOutput ? Path.Combine(project, ".build", "obj") + Path.DirectorySeparatorChar : Path.Combine(project, "obj") + Path.DirectorySeparatorChar;
			var projectFile = Path.Combine(project, projectName + ".csproj");

			File.WriteAllText(
				projectFile,
				$"""
				<Project Sdk="Microsoft.NET.Sdk">
					<PropertyGroup>
						<TargetFramework>net10.0</TargetFramework>
						{(moveIntermediateOutput ? $"<MSBuildProjectExtensionsPath>{extensions}</MSBuildProjectExtensionsPath>" : "")}
					</PropertyGroup>
				</Project>
				""");

			Directory.CreateDirectory(extensions);

			var assets = Path.Combine(extensions, "project.assets.json");

			File.WriteAllText(
				assets,
				JsonSerializer.Serialize(new
				{
					version        = 3,
					libraries      = new Dictionary<string, object> { [$"linq2db/{version}"] = new { type = "package", path = $"linq2db/{version}" } },
					packageFolders = new Dictionary<string, object> { [packages + Path.DirectorySeparatorChar] = new { } },
				}));

			if (withSkill)
			{
				var skill = Path.Combine(package, "skills", "linq2db");

				Directory.CreateDirectory(Path.Combine(skill, "docs", "crud"));
				File.WriteAllText(Path.Combine(skill, "SKILL.md"), $"---\r\nname: linq2db\r\ndescription: test skill {version}\r\n---\r\n\r\n# linq2db skill\r\n");
				File.WriteAllText(Path.Combine(skill, "docs", "crud", "crud-update.md"), "update guide\r\n");
			}
			else
			{
				Directory.CreateDirectory(package);
			}

			return new Repository(root, projectFile, project, package, assets);
		}

		private static Dictionary<string, string> ReadFrontmatter(string text)
		{
			var lines = text.Replace("\r\n", "\n").Split('\n');

			lines[0].ShouldBe("---");

			var end = Array.IndexOf(lines, "---", 1);

			end.ShouldBeGreaterThan(0);

			return lines.Skip(1).Take(end - 1)
				.Select(l => l.Split(": ", 2))
				.ToDictionary(p => p[0], p => p[1], StringComparer.Ordinal);
		}

		/// <summary>
		/// Content and last write time of every file under a directory.
		/// </summary>
		private static Dictionary<string, string> Snapshot(string directory)
		{
			return Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories)
				.Where(f => !f.Contains($"{Path.DirectorySeparatorChar}.git{Path.DirectorySeparatorChar}", StringComparison.Ordinal))
				.ToDictionary(f => Path.GetRelativePath(directory, f), f => Convert.ToBase64String(File.ReadAllBytes(f)), StringComparer.Ordinal);
		}

		private static async Task<CliResult> RunCli(params string[] arguments)
		{
			var environment = new TestCliEnvironment();
			var exitCode    = await new LinqToDBCliController().Execute(arguments, environment).ConfigureAwait(false);

			return new CliResult(exitCode, environment.Output, environment.ErrorOutput);
		}

		private sealed record Repository(string Root, string Project, string ProjectDirectory, string Package, string Assets);

		private sealed record CliResult(int ExitCode, string Output, string Error);
	}
}
