using System;
using System.Collections.Generic;
using System.IO;

using LinqToDB.CommandLine.Commands.Credentials;

using NUnit.Framework;

using Shouldly;

namespace Tests.LinqToDB.CLI
{
	/// <summary>
	/// Which store a command uses (option, profile, OS default), the reserved values on each OS, and the credentials
	/// directory rules. Selection is pure except for the checks of generated scripts, which use real files (POSIX).
	/// </summary>
	[TestFixture]
	public sealed class CredentialStoreSelectorTests
	{
		static Func<string, string?> Variables(params (string Name, string Value)[] variables)
		{
			var map = new Dictionary<string, string>(StringComparer.Ordinal);

			foreach (var (name, value) in variables)
				map[name] = value;

			return name => map.GetValueOrDefault(name);
		}

		static CredentialStoreChoice Select(string? value, bool isWindows, Func<string, string?> variables, string source = "option", string? baseDirectory = null)
		{
			CredentialStoreSelector.TrySelect(value, source, baseDirectory, isWindows, variables, out var choice, out var error).ShouldBeTrue(error);
			return choice;
		}

		static string SelectError(string? value, bool isWindows, Func<string, string?> variables)
		{
			CredentialStoreSelector.TrySelect(value, "option", null, isWindows, variables, out _, out var error).ShouldBeFalse();
			return error.ShouldNotBeNull();
		}

		[Test]
		public void LinuxDefaultIsTheLocalStore()
		{
			var choice = Select(null, false, Variables(("HOME", "/home/u")), CredentialStoreSelector.OsDefaultSource);

			choice.Kind.       ShouldBe(CredentialStoreKind.Local);
			choice.Directory.  ShouldBe("/home/u/.config/linq2db");
			choice.IsOsDefault.ShouldBeTrue();
			choice.Describe(). ShouldBe("the local store (/home/u/.config/linq2db, default for this OS)");
		}

		[Test]
		public void WindowsDefaultIsCredentialManager()
		{
			var choice = Select(null, true, Variables(("LOCALAPPDATA", @"C:\Users\u\AppData\Local")), CredentialStoreSelector.OsDefaultSource);

			choice.Kind.       ShouldBe(CredentialStoreKind.CredentialManager);
			choice.IsOsDefault.ShouldBeTrue();
			choice.Describe(). ShouldBe("Windows Credential Manager (default for this OS)");
		}

		[TestCase(false, "/home/u/.config/linq2db")]
		[TestCase(true,  @"C:\Users\u\AppData\Local\linq2db")]
		public void LocalIsNamedOnEveryOs(bool isWindows, string directory)
		{
			var choice = Select("@local", isWindows, Variables(("HOME", "/home/u"), ("LOCALAPPDATA", @"C:\Users\u\AppData\Local")), "profile 'prod' of q.json");

			choice.Kind.       ShouldBe(CredentialStoreKind.Local);
			choice.Directory.  ShouldBe(directory);
			choice.IsOsDefault.ShouldBeFalse();
			choice.Describe(). ShouldBe($"the local store ({directory}, profile 'prod' of q.json)");
		}

		[Test]
		public void CredentialManagerOnWindows()
		{
			Select("@credential-manager", true, Variables()).Kind.ShouldBe(CredentialStoreKind.CredentialManager);
		}

		[Test]
		public void CredentialManagerElsewhereIsAnError()
		{
			SelectError("@credential-manager", false, Variables(("HOME", "/home/u"))).ShouldContain("available only on Windows");
		}

		[TestCase("@keyring")]
		[TestCase("@gpg")]
		public void GeneratedScriptsOnWindowsAreAnError(string value)
		{
			SelectError(value, true, Variables(("LOCALAPPDATA", @"C:\Users\u\AppData\Local"))).ShouldContain("generated sh script for Linux and macOS");
		}

		[TestCase("@vault")]
		[TestCase(" @local2")]
		public void UnknownReservedValueIsAnError(string value)
		{
			var error = SelectError(value, false, Variables(("HOME", "/home/u")));

			error.ShouldContain("Unknown credential store");
			error.ShouldContain("./@name");
		}

		[Test]
		public void AnythingElseIsACommandLine()
		{
			var choice = Select("\"/opt/my tools/vault-cli\" --vault prod", false, Variables(), "profile 'default' of /cfg/q.json", "/cfg");

			choice.Kind.ShouldBe(CredentialStoreKind.CredentialsCli);
			choice.Cli.ShouldBe(new CredentialsCliSettings("\"/opt/my tools/vault-cli\" --vault prod", "/opt/my tools/vault-cli", "--vault prod", "/cfg"));
			choice.Describe().ShouldBe("credentials CLI '\"/opt/my tools/vault-cli\" --vault prod' (profile 'default' of /cfg/q.json)");
		}

		[Test]
		public void CommandLineErrorNamesTheSource()
		{
			SelectError("\"/opt/h", false, Variables()).ShouldEndWith("(option)");
		}

		[Test]
		public void PathProgramStartingWithAtIsACommandLine()
		{
			Select("./@x", false, Variables()).Cli!.Program.ShouldBe("./@x");
		}

		[TestCase(false, "/srv/creds",   "/srv/creds",   TestName = "DirectoryFromVariableUnix")]
		[TestCase(true,  @"D:\creds\",   @"D:\creds",    TestName = "DirectoryFromVariableWindows")]
		public void DirectoryVariableWins(bool isWindows, string value, string expected)
		{
			CredentialsDirectory.TryResolve(isWindows, Variables((CredentialsDirectory.Variable, value), ("HOME", "/home/u"), ("XDG_CONFIG_HOME", "/xdg"), ("LOCALAPPDATA", @"C:\L")), out var directory, out var error).ShouldBeTrue(error);

			directory.ShouldBe(expected);
		}

		[Test]
		public void DirectoryFromXdgConfigHome()
		{
			CredentialsDirectory.TryResolve(false, Variables(("HOME", "/home/u"), ("XDG_CONFIG_HOME", "/xdg/")), out var directory, out var error).ShouldBeTrue(error);

			directory.ShouldBe("/xdg/linq2db");
		}

		[Test]
		public void RelativeVariablesAreIgnored()
		{
			CredentialsDirectory.TryResolve(false, Variables((CredentialsDirectory.Variable, "creds"), ("XDG_CONFIG_HOME", "xdg"), ("HOME", "/home/u")), out var directory, out var error).ShouldBeTrue(error);

			directory.ShouldBe("/home/u/.config/linq2db");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void UnresolvableDirectoryAsksForTheVariable(bool isWindows)
		{
			CredentialsDirectory.TryResolve(isWindows, Variables(("HOME", "home"), (CredentialsDirectory.Variable, "relative")), out _, out var error).ShouldBeFalse();

			error.ShouldNotBeNull().ShouldContain("Set LINQ2DB_CREDENTIALS_DIR");
		}

		[Test]
		public void UnresolvableDirectoryIsAnErrorForTheDefaultStore()
		{
			SelectError(null, false, Variables()).ShouldContain("Set LINQ2DB_CREDENTIALS_DIR");
		}

		[TestCase(@"C:\Users\u\AppData\Local\linq2db", @"C:\Users\u",  true,  TestName = "WindowsDirectoryInsideProfile")]
		[TestCase(@"c:\users\U\x",                     @"C:\Users\u\", true,  TestName = "WindowsDirectoryInsideProfileIgnoresCase")]
		[TestCase(@"D:\shared\linq2db",                @"C:\Users\u",  false, TestName = "WindowsDirectoryOnAnotherDrive")]
		[TestCase(@"C:\Users\u2\linq2db",              @"C:\Users\u",  false, TestName = "WindowsDirectoryInAProfileWithTheSamePrefix")]
		[TestCase(@"C:\Users\u",                       @"C:\Users\u",  false, TestName = "WindowsDirectoryIsTheProfileItself")]
		[TestCase(@"C:\Users\u\linq2db",               null,           false, TestName = "WindowsDirectoryWithoutProfile")]
		public void WindowsDirectoryMustBeInsideTheProfile(string directory, string? profile, bool inside)
		{
			CredentialsDirectory.IsInsideProfile(directory, profile).ShouldBe(inside);
		}

		[Test]
		public void WindowsShortNameOfTheProfileIsInsideIt()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows 8.3 short names.");

			var profile = Directory.CreateDirectory(Path.Combine(_directory, "Long Profile Name")).FullName;
			var shortProfile = GetShortPath(profile);

			if (shortProfile == null || string.Equals(shortProfile, profile, StringComparison.OrdinalIgnoreCase))
				Assert.Ignore("8.3 short names are disabled on this volume.");

			CredentialsDirectory.IsInsideProfile(Path.Combine(shortProfile, "AppData", "linq2db"), profile).ShouldBeTrue();
			CredentialsDirectory.IsInsideProfile(Path.Combine(profile, "AppData", "linq2db"), shortProfile).ShouldBeTrue();
		}

		[Test]
		public void WindowsLinkFromTheProfileToElsewhereIsOutside()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows links.");

			var profile = Directory.CreateDirectory(Path.Combine(_directory, "profile")).FullName;
			var outside = Directory.CreateDirectory(Path.Combine(_directory, "outside")).FullName;
			var link    = Path.Combine(profile, "shared");

			try
			{
				Directory.CreateSymbolicLink(link, outside);
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				Assert.Ignore($"Cannot create a directory link here: {ex.Message}");
			}

			CredentialsDirectory.IsInsideProfile(Path.Combine(link, "linq2db"), profile).ShouldBeFalse();
			CredentialsDirectory.IsInsideProfile(Path.Combine(profile, "linq2db"), profile).ShouldBeTrue();
		}

		/// <summary>The 8.3 short form of an existing path, as cmd.exe reports it (%~s).</summary>
		static string? GetShortPath(string path)
		{
			var startInfo = new System.Diagnostics.ProcessStartInfo("cmd.exe")
			{
				Arguments              = $"/d /c for %I in (\"{path}\") do @echo %~sI",
				UseShellExecute        = false,
				RedirectStandardOutput = true,
			};

			using var process = System.Diagnostics.Process.Start(startInfo)!;

			var output = process.StandardOutput.ReadToEnd().Trim();
			process.WaitForExit();

			return process.ExitCode == 0 && output.Length > 0 ? output : null;
		}

		[Test]
		public void WindowsLocalStoreOutsideProfileIsRefused()
		{
			if (!OperatingSystem.IsWindows())
				Assert.Ignore("Windows behaviour.");

			var store = new LocalCredentialStore(@"D:\shared\linq2db", Environment.GetEnvironmentVariable("USERPROFILE"));

			store.TryList(out _, out _, out var error).ShouldBeFalse();
			error.ShouldNotBeNull().ShouldContain("Set LINQ2DB_CREDENTIALS_DIR inside your profile");
		}

		// Generated scripts (POSIX, real files).

		string _directory = null!;

		[SetUp]
		public void SetUp()
		{
			_directory = CredentialsCliTestSupport.CreateTempDirectory();
		}

		[TearDown]
		public void TearDown()
		{
			CredentialsCliTestSupport.DeleteDirectory(_directory);
		}

		Func<string, string?> DirectoryVariables()
		{
			return Variables((CredentialsDirectory.Variable, _directory));
		}

		string WriteGeneratedScript(string store, UnixFileMode mode)
		{
			var script = Path.Combine(_directory, $"credentials-{store}.sh");
			File.WriteAllText(script, "#!/bin/sh\n");
			File.SetUnixFileMode(script, mode);
			return script;
		}

		[TestCase("keyring")]
		[TestCase("gpg")]
		public void GeneratedScriptIsUsedWhenNamed(string store)
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX scripts.");

			var script = WriteGeneratedScript(store, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			var choice = Select("@" + store, false, DirectoryVariables());

			choice.Kind.  ShouldBe(CredentialStoreKind.CredentialsCli);
			choice.Script.ShouldBe(script);
			choice.Cli.   ShouldBe(new CredentialsCliSettings("@" + store, script, string.Empty, null));
			choice.Describe().ShouldBe($"credentials CLI '@{store}' ({script}, option)");
		}

		[Test]
		public void MissingGeneratedScriptNamesThePathAndInit()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX scripts.");

			var error = SelectError("@gpg", false, DirectoryVariables());

			error.ShouldContain(Path.Combine(_directory, "credentials-gpg.sh"));
			error.ShouldContain("dotnet linq2db credentials cli init --store gpg");
		}

		[Test]
		public void SymlinkedGeneratedScriptIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX scripts.");

			var target = Path.Combine(_directory, "elsewhere.sh");
			File.WriteAllText(target, "#!/bin/sh\n");
			File.CreateSymbolicLink(Path.Combine(_directory, "credentials-gpg.sh"), target);

			SelectError("@gpg", false, DirectoryVariables()).ShouldContain("is a symbolic link");
		}

		[Test]
		public void GroupWritableGeneratedScriptIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX scripts.");

			WriteGeneratedScript("keyring", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupWrite);

			SelectError("@keyring", false, DirectoryVariables()).ShouldContain("chmod 700");
		}

		[Test]
		public void GeneratedScriptUnderAGroupWritableAncestorIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX scripts.");

			var shared    = Path.Combine(_directory, "shared");
			var directory = Path.Combine(shared, "deeper", "credentials");

			Directory.CreateDirectory(directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			File.SetUnixFileMode(Path.Combine(shared, "deeper"), UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			File.SetUnixFileMode(shared, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute | UnixFileMode.GroupRead | UnixFileMode.GroupWrite | UnixFileMode.GroupExecute);

			var script = Path.Combine(directory, "credentials-gpg.sh");
			File.WriteAllText(script, "#!/bin/sh\n");
			File.SetUnixFileMode(script, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);

			var error = SelectError("@gpg", false, Variables((CredentialsDirectory.Variable, directory)));

			error.ShouldContain($"is inside '{shared}', which other users can write to without the sticky bit");
			error.ShouldContain("chmod go-w");
		}

		[Test]
		public void GeneratedScriptInWorldWritableDirectoryIsRefused()
		{
			if (OperatingSystem.IsWindows())
				Assert.Ignore("POSIX scripts.");

			WriteGeneratedScript("gpg", UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			File.SetUnixFileMode(_directory, (UnixFileMode)0b111_111_111);

			try
			{
				SelectError("@gpg", false, DirectoryVariables()).ShouldContain("writable by other users");
			}
			finally
			{
				File.SetUnixFileMode(_directory, UnixFileMode.UserRead | UnixFileMode.UserWrite | UnixFileMode.UserExecute);
			}
		}
	}
}
