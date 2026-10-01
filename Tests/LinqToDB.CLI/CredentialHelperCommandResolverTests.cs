using System;
using System.Collections.Generic;
using System.IO;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Command resolution and launch rules for both operating systems, over a fake file system (pure, every OS).
	/// </summary>
	[TestFixture]
	public sealed class CredentialHelperCommandResolverTests
	{
		static bool Resolve(string command, bool isWindows, string? path, string? pathExt, IEnumerable<string> files, out string resolved, out string? error, string? baseDirectory = null)
		{
			var set = new HashSet<string>(files, isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			return CredentialHelperCommandResolver.TryResolve(command, baseDirectory, isWindows, path, pathExt, set.Contains, out resolved, out error);
		}

		[Test]
		public void PosixBareNameSearchesPathInOrder()
		{
			Resolve("helper", false, "/usr/local/bin:/usr/bin", null, ["/usr/bin/helper", "/usr/local/bin/helper"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe("/usr/local/bin/helper");
		}

		[Test]
		public void PosixEmptyAndRelativePathEntriesAreSkipped()
		{
			Resolve("helper", false, ":.:bin:/opt/bin", null, ["helper", "./helper", "bin/helper"], out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'helper' was not found on PATH.");
		}

		[Test]
		public void PosixAbsolutePath()
		{
			Resolve("/opt/h/helper.sh", false, null, null, ["/opt/h/helper.sh"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe("/opt/h/helper.sh");
		}

		[Test]
		public void PosixRelativePathUsesBaseDirectory()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX paths are normalized with the host's path rules.");

			Resolve("helpers/../h.sh", false, null, null, ["/cfg/h.sh"], out var resolved, out var error, "/cfg").ShouldBeTrue(error);

			resolved.ShouldBe("/cfg/h.sh");
		}

		[Test]
		public void MissingPathIsReported()
		{
			Resolve("/opt/missing", false, null, null, [], out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper '/opt/missing' was not found.");
		}

		[TestCase("")]
		[TestCase(" ")]
		[TestCase("he\nlper")]
		public void InvalidCommandIsRefused(string command)
		{
			Resolve(command, false, "/bin", null, [], out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("non-empty path or file name without control characters");
		}

		[Test]
		public void WindowsBareNameUsesPathExtOrder()
		{
			Resolve("helper", true, @"C:\Tools;C:\Other", ".BAT;.EXE", [@"C:\Tools\helper.exe", @"C:\Tools\helper.bat"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\Tools\helper.bat");
		}

		[Test]
		public void WindowsPathExtIsFilteredToRunnableExtensions()
		{
			Resolve("helper", true, @"C:\Tools", ".VBS;.PS1;.CMD", [@"C:\Tools\helper.vbs", @"C:\Tools\helper.ps1", @"C:\Tools\helper.cmd"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\Tools\helper.cmd");
		}

		[Test]
		public void WindowsDefaultPathExt()
		{
			Resolve("docker-credential-wincred", true, @"C:\Program Files\Docker\bin", null, [@"C:\Program Files\Docker\bin\docker-credential-wincred.exe"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\Program Files\Docker\bin\docker-credential-wincred.exe");
		}

		[Test]
		public void WindowsExplicitExtension()
		{
			Resolve("helper.cmd", true, @"C:\Tools", null, [@"C:\Tools\helper.cmd", @"C:\Tools\helper.cmd.exe"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\Tools\helper.cmd");
		}

		[Test]
		public void WindowsQuotedPathEntry()
		{
			Resolve("helper", true, "\"C:\\My Tools\"", null, [@"C:\My Tools\helper.exe"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\My Tools\helper.exe");
		}

		[Test]
		public void WindowsRelativePathEntryIsSkipped()
		{
			Resolve("helper", true, @"tools;;C:\Bin", null, [@"tools\helper.exe"], out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credential helper 'helper' was not found on PATH.");
		}

		[TestCase("helper.ps1")]
		[TestCase(@"C:\Tools\helper.ps1")]
		[TestCase(@"C:\Tools\helper.sh")]
		public void WindowsUnsupportedExtensionIsRefused(string command)
		{
			Resolve(command, true, @"C:\Tools", null, [@"C:\Tools\helper.ps1", @"C:\Tools\helper.sh"], out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("a helper must be an .exe, .com, .cmd or .bat file");
			error.ShouldNotBeNull().ShouldContain("%~dp0helper.ps1");
		}

		[Test]
		public void WindowsAbsolutePathWithoutExtensionTriesPathExt()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows paths are normalized with the host's path rules.");

			Resolve(@"C:\Tools\helper", true, null, null, [@"C:\Tools\helper.cmd"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\Tools\helper.cmd");
		}

		static CredentialHelperCommandResolver.Launch GetLaunch(string path, string verb, bool isWindows, params string[] arguments)
		{
			CredentialHelperCommandResolver.TryGetLaunch(path, arguments, verb, isWindows, @"C:\Windows\System32", out var launch, out var error).ShouldBeTrue(error);
			return launch;
		}

		[Test]
		public void PosixShellScriptRunsThroughBinSh()
		{
			var launch = GetLaunch("/home/u/helper.sh", "get", false);

			launch.FileName.ShouldBe("/bin/sh");
			launch.Arguments.ShouldBe(["/home/u/helper.sh", "get"]);
			launch.RawArguments.ShouldBeNull();
		}

		[Test]
		public void PosixShellScriptWithArguments()
		{
			GetLaunch("/home/u/helper.sh", "get", false, "--vault", "a b").Arguments.ShouldBe(["/home/u/helper.sh", "--vault", "a b", "get"]);
		}

		[Test]
		public void PosixExecutableRunsDirectly()
		{
			var launch = GetLaunch("/usr/bin/docker-credential-pass", "list", false);

			launch.FileName.ShouldBe("/usr/bin/docker-credential-pass");
			launch.Arguments.ShouldBe(["list"]);
		}

		[Test]
		public void DotnetRunWithArgumentsBeforeTheVerb()
		{
			var launch = GetLaunch("/usr/bin/dotnet", "store", false, "run", "/home/u/secret helper.cs", "--");

			launch.FileName.ShouldBe("/usr/bin/dotnet");
			launch.Arguments.ShouldBe(["run", "/home/u/secret helper.cs", "--", "store"]);
		}

		[TestCase(@"C:\Program Files (x86)\h & co\helper.cmd")]
		[TestCase(@"C:\Tools\helper.BAT")]
		public void WindowsBatchRunsThroughCmdWithOuterQuotes(string path)
		{
			var launch = GetLaunch(path, "store", true);

			launch.FileName.ShouldBe(@"C:\Windows\System32\cmd.exe");
			launch.Arguments.ShouldBeEmpty();
			launch.RawArguments.ShouldBe($"/d /v:off /s /c \"\"{path}\" store\"");
		}

		[Test]
		public void WindowsBatchRefusesArguments()
		{
			CredentialHelperCommandResolver.TryGetLaunch(@"C:\Tools\helper.cmd", ["x"], "get", true, null, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("configured arguments are not supported for batch files");
		}

		[Test]
		public void WindowsBatchPathWithPercentIsRefused()
		{
			CredentialHelperCommandResolver.TryGetLaunch(@"C:\Tools\%USERNAME%\helper.cmd", [], "get", true, null, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("contains '%', which cmd.exe would expand");
		}

		[Test]
		public void WindowsExecutablePathWithPercentIsAllowed()
		{
			GetLaunch(@"C:\Tools\100%\helper.exe", "get", true).FileName.ShouldBe(@"C:\Tools\100%\helper.exe");
		}

		[Test]
		public void WindowsExecutableRunsDirectly()
		{
			var launch = GetLaunch(@"C:\Program Files\dotnet\dotnet.exe", "get", true, "run", @"C:\Users\u\secrethelper.cs", "--");

			launch.FileName.ShouldBe(@"C:\Program Files\dotnet\dotnet.exe");
			launch.Arguments.ShouldBe(["run", @"C:\Users\u\secrethelper.cs", "--", "get"]);
			launch.RawArguments.ShouldBeNull();
		}

		[Test]
		public void ArgumentsWithControlCharactersAreRefused()
		{
			CredentialHelperCommandResolver.TryGetLaunch("/usr/bin/h", ["a\nb"], "get", false, null, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("must not contain control characters");
		}

		[Test]
		public void PathOfThisPlatformResolvesRealFile()
		{
			var directory = CredentialHelperTestSupport.CreateTempDirectory();

			try
			{
				var script = CredentialHelperTestSupport.WriteScript(directory, "real-helper", "exit 0\n", "@exit /b 0\r\n");

				CredentialHelperCommandResolver.TryResolve(
					Path.GetFileNameWithoutExtension(script) + (OperatingSystem.IsWindows() ? string.Empty : ".sh"),
					null,
					OperatingSystem.IsWindows(),
					directory,
					null,
					File.Exists,
					out var resolved,
					out var error).ShouldBeTrue(error);

				resolved.ShouldBe(script, StringCompareShould.IgnoreCase);
			}
			finally
			{
				CredentialHelperTestSupport.DeleteDirectory(directory);
			}
		}
	}
}
