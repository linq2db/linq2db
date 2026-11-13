using System;
using System.Collections.Generic;
using System.IO;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Command line splitting, program resolution and launch rules for both operating systems, over a fake file system
	/// (pure, every OS).
	/// </summary>
	[TestFixture]
	public sealed class CredentialsCliCommandResolverTests
	{
		[TestCase("helper",                               "helper",                    "",                         TestName = "CommandLineProgramOnly")]
		[TestCase("  helper   get  ",                     "helper",                    "get",                      TestName = "CommandLineTrimsSeparators")]
		[TestCase("dotnet run --file /a/b.cs --",         "dotnet",                    "run --file /a/b.cs --",    TestName = "CommandLinePlainFirstWord")]
		[TestCase("pwsh\t-NoProfile -File x.ps1",          "pwsh",                      "-NoProfile -File x.ps1",   TestName = "CommandLineTabSeparates")]
		[TestCase("\"C:\\Program Files\\h\\h.exe\" --vault \"a b\"", "C:\\Program Files\\h\\h.exe", "--vault \"a b\"", TestName = "CommandLineQuotedProgram")]
		[TestCase("\"/opt/my tools/h\"",                  "/opt/my tools/h",           "",                         TestName = "CommandLineQuotedProgramOnly")]
		[TestCase("h a \"b",                               "h",                         "a \"b",                     TestName = "CommandLineUnbalancedQuoteInArgumentsIsTheProgramsBusiness")]
		public void CommandLineSplitsTheFirstWord(string commandLine, string program, string arguments)
		{
			CredentialsCliCommandResolver.TryParseCommandLine(commandLine, out var parsedProgram, out var parsedArguments, out var error).ShouldBeTrue(error);

			parsedProgram.  ShouldBe(program);
			parsedArguments.ShouldBe(arguments);
		}

		[TestCase("",                    "is empty",                       TestName = "CommandLineEmpty")]
		[TestCase("   \t ",              "is empty",                       TestName = "CommandLineWhitespace")]
		[TestCase("\"/opt/h --x",         "unterminated quote at position 1", TestName = "CommandLineUnterminatedQuote")]
		[TestCase("  \"/opt/h",           "unterminated quote at position 3", TestName = "CommandLineUnterminatedQuoteAfterSpaces")]
		[TestCase("\"\" x",               "does not name a program",        TestName = "CommandLineEmptyQuotedProgram")]
		[TestCase("\"/opt/h\"x",          "space or tab after the quoted program", TestName = "CommandLineTextAfterQuote")]
		[TestCase("h\nget",              "control characters",             TestName = "CommandLineControlCharacter")]
		public void CommandLineErrors(string commandLine, string message)
		{
			CredentialsCliCommandResolver.TryParseCommandLine(commandLine, out _, out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain(message);
		}

		static bool Resolve(string command, bool isWindows, string? path, string? pathExt, IEnumerable<string> files, out string resolved, out string? error, string? baseDirectory = null)
		{
			var set = new HashSet<string>(files, isWindows ? StringComparer.OrdinalIgnoreCase : StringComparer.Ordinal);
			return CredentialsCliCommandResolver.TryResolve(command, baseDirectory, isWindows, path, pathExt, set.Contains, out resolved, out error);
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

			error.ShouldBe("Credentials CLI program 'helper' was not found (looked up on PATH; the current directory is never searched).");
		}

		[Test]
		public void PosixAbsolutePath()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX paths are normalized with the host's path rules.");

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
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX paths are normalized with the host's path rules.");

			Resolve("/opt/missing", false, null, null, [], out _, out var error).ShouldBeFalse();

			error.ShouldBe("Credentials CLI program '/opt/missing' was not found (looked for '/opt/missing').");
		}

		[TestCase("~/bin/helper")]
		[TestCase("$HOME/bin/helper")]
		[TestCase("%USERPROFILE%/helper")]
		public void NotFoundNotesThatNothingIsExpanded(string program)
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX paths are normalized with the host's path rules.");

			Resolve(program, false, "/bin", null, [], out _, out var error, "/cfg").ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("'~', '$HOME' and '%VAR%' are not expanded");
			error.ShouldNotBeNull().ShouldContain(program.Contains('/', StringComparison.Ordinal) ? "looked for '/cfg/" : "looked up on PATH");
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

			error.ShouldBe("Credentials CLI program 'helper' was not found (looked up on PATH; the current directory is never searched).");
		}

		[TestCase("helper.ps1",            "pwsh -NoProfile -File",                   TestName = "WindowsBarePowerShellScriptSuggestsPwsh")]
		[TestCase(@"C:\Tools\helper.ps1",  "pwsh -NoProfile -File",                   TestName = "WindowsPowerShellScriptSuggestsPwsh")]
		[TestCase(@"C:\Tools\helper.py",   @"python \""C:\\Tools\\helper.py\""", TestName = "WindowsPythonScriptSuggestsPython")]
		[TestCase(@"C:\Tools\helper.sh",   "through its interpreter",                 TestName = "WindowsShellScriptSuggestsAnInterpreter")]
		public void WindowsUnsupportedExtensionSuggestsTheInterpreter(string command, string hint)
		{
			Resolve(command, true, @"C:\Tools", null, [@"C:\Tools\helper.ps1", @"C:\Tools\helper.sh", @"C:\Tools\helper.py"], out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("is not a valid application on Windows");
			error.ShouldNotBeNull().ShouldContain(hint);
		}

		[Test]
		public void WindowsAbsolutePathWithoutExtensionTriesPathExt()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows paths are normalized with the host's path rules.");

			Resolve(@"C:\Tools\helper", true, null, null, [@"C:\Tools\helper.cmd"], out var resolved, out var error).ShouldBeTrue(error);

			resolved.ShouldBe(@"C:\Tools\helper.cmd");
		}

		static CredentialsCliCommandResolver.Launch GetLaunch(string path, string verb, bool isWindows, string arguments = "")
		{
			return CredentialsCliCommandResolver.GetLaunch(path, arguments, verb, isWindows, @"C:\Windows\System32");
		}

		[Test]
		public void PosixProgramRunsDirectlyWithTheVerbLast()
		{
			var launch = GetLaunch("/home/u/helper.sh", "get", false);

			launch.FileName. ShouldBe("/home/u/helper.sh");
			launch.Arguments.ShouldBe("get");
		}

		[Test]
		public void ArgumentStringIsPassedUnchangedBeforeTheVerb()
		{
			var launch = GetLaunch("/usr/bin/dotnet", "store", false, "run --file \"/home/u/secret helper.cs\" --");

			launch.FileName. ShouldBe("/usr/bin/dotnet");
			launch.Arguments.ShouldBe("run --file \"/home/u/secret helper.cs\" -- store");
		}

		[TestCase(@"C:\Program Files (x86)\h & co\helper.cmd", "",               "store", TestName = "WindowsBatchRunsThroughCmdWithOuterQuotes")]
		[TestCase(@"C:\Tools\helper.BAT",                     "",               "store", TestName = "WindowsBatchUpperCaseExtension")]
		[TestCase(@"C:\Tools\100%\helper.cmd",                "--vault \"a b\"", "get",   TestName = "WindowsBatchWithArgumentsAndPercent")]
		public void WindowsBatchRunsThroughCmd(string path, string arguments, string verb)
		{
			var launch = GetLaunch(path, verb, true, arguments);
			var tail   = arguments.Length == 0 ? verb : arguments + " " + verb;

			launch.FileName. ShouldBe(@"C:\Windows\System32\cmd.exe");
			launch.Arguments.ShouldBe($"/d /v:off /s /c \"\"{path}\" {tail}\"");
		}

		[Test]
		public void WindowsExecutableRunsDirectly()
		{
			var launch = GetLaunch(@"C:\Program Files\dotnet\dotnet.exe", "get", true, @"run --file C:\Users\u\secrethelper.cs --");

			launch.FileName. ShouldBe(@"C:\Program Files\dotnet\dotnet.exe");
			launch.Arguments.ShouldBe(@"run --file C:\Users\u\secrethelper.cs -- get");
		}

		[Test]
		public void PathOfThisPlatformResolvesRealFile()
		{
			var directory = CredentialsCliTestSupport.CreateTempDirectory();

			try
			{
				var script = CredentialsCliTestSupport.WriteScript(directory, "real-helper", "exit 0\n", "@exit /b 0\r\n");

				CredentialsCliCommandResolver.TryResolve(
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
				CredentialsCliTestSupport.DeleteDirectory(directory);
			}
		}
	}
}
