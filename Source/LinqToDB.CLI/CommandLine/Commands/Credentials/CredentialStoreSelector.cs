using System;
using System.IO;

using LinqToDB.CommandLine.Commands.QueryExecution;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Selects the credential store: the <c>--credentials-cli</c> value, else the profile's <c>credentialsCli</c>, else the
	/// OS default (Windows Credential Manager on Windows, the local store elsewhere). Reserved values name the built-in
	/// stores and the generated scripts; anything else is a command line.
	/// </summary>
	internal static class CredentialStoreSelector
	{
		public const string Local             = "@local";
		public const string CredentialManager = "@credential-manager";
		public const string Keyring           = "@keyring";
		public const string Gpg               = "@gpg";

		public const string OptionSource    = "option";
		public const string OsDefaultSource = "default for this OS";

		/// <summary>
		/// Selects the store for a command: <paramref name="optionValue"/> (<c>--credentials-cli</c>), else the configuration
		/// profile's <c>credentialsCli</c>, else the OS default.
		/// </summary>
		public static bool TrySelect(ICliEnvironment environment, string? optionValue, QueryExecutionConfiguration? configuration, string? configFile, out CredentialStoreChoice choice, out string? error)
		{
			if (optionValue != null)
				return TrySelect(optionValue, OptionSource, null, OperatingSystem.IsWindows(), environment.GetEnvironmentVariable, out choice, out error);

			if (configuration?.CredentialsCli != null && configFile != null)
			{
				var fullPath = Path.GetFullPath(configFile);

				return TrySelect(
					configuration.CredentialsCli,
					$"profile '{configuration.CredentialsCliProfile}' of {configFile}",
					Path.GetDirectoryName(fullPath),
					OperatingSystem.IsWindows(),
					environment.GetEnvironmentVariable,
					out choice,
					out error);
			}

			return TrySelect(null, OsDefaultSource, null, OperatingSystem.IsWindows(), environment.GetEnvironmentVariable, out choice, out error);
		}

		/// <summary>Creates the selected store.</summary>
		public static ICredentialStore Create(ICliEnvironment environment, CredentialStoreChoice choice)
		{
			return choice.Kind switch
			{
				CredentialStoreKind.CredentialManager => environment.CredentialStore,
				CredentialStoreKind.Local             => environment.CreateLocalCredentialStore(choice.Directory!),
				_                                     => environment.CreateCredentialsCliStore(choice.Cli!),
			};
		}

		/// <summary>The script a generated store uses, in the credentials directory.</summary>
		public static string GetScriptFileName(string store)
		{
			return $"credentials-{store}.sh";
		}

		/// <param name="value">The configured value, or <see langword="null"/> for the OS default.</param>
		/// <param name="source">Where <paramref name="value"/> came from.</param>
		/// <param name="baseDirectory">Directory a relative program path is resolved against (<see langword="null"/>: current directory).</param>
		/// <param name="isWindows">Whether the Windows rules apply.</param>
		/// <param name="getVariable">Reads an environment variable.</param>
		/// <param name="choice">The selected store.</param>
		/// <param name="error">Why no store can be selected.</param>
		public static bool TrySelect(string? value, string source, string? baseDirectory, bool isWindows, Func<string, string?> getVariable, out CredentialStoreChoice choice, out string? error)
		{
			choice = null!;

			if (value == null)
			{
				if (isWindows)
				{
					choice = new CredentialStoreChoice(CredentialStoreKind.CredentialManager, OsDefaultSource) { IsOsDefault = true };
					error  = null;
					return true;
				}

				return TrySelectLocal(OsDefaultSource, isWindows, getVariable, isOsDefault: true, out choice, out error);
			}

			switch (value.Trim())
			{
				case Local:
					return TrySelectLocal(source, isWindows, getVariable, isOsDefault: false, out choice, out error);

				case CredentialManager:
					if (!isWindows)
					{
						error = $"'{CredentialManager}' ({source}) is Windows Credential Manager, available only on Windows; use '{Local}' or a credentials CLI here.";
						return false;
					}

					choice = new CredentialStoreChoice(CredentialStoreKind.CredentialManager, source);
					error  = null;
					return true;

				case Keyring:
					return TrySelectScript("keyring", source, isWindows, getVariable, out choice, out error);

				case Gpg:
					return TrySelectScript("gpg", source, isWindows, getVariable, out choice, out error);
			}

			if (value.TrimStart(' ', '\t').StartsWith('@'))
			{
				error = $"Unknown credential store '{value}' ({source}). Expected {Local}, {CredentialManager}, {Keyring}, {Gpg}, or a command line; write a program whose name starts with '@' with a path, for example ./@name.";
				return false;
			}

			if (!CredentialsCliSettings.TryCreate(value, baseDirectory, out var settings, out error))
			{
				error = $"{error} ({source})";
				return false;
			}

			choice = new CredentialStoreChoice(CredentialStoreKind.CredentialsCli, source) { Cli = settings };
			return true;
		}

		static bool TrySelectLocal(string source, bool isWindows, Func<string, string?> getVariable, bool isOsDefault, out CredentialStoreChoice choice, out string? error)
		{
			choice = null!;

			if (!CredentialsDirectory.TryResolve(isWindows, getVariable, out var directory, out error))
				return false;

			choice = new CredentialStoreChoice(CredentialStoreKind.Local, source) { Directory = directory, IsOsDefault = isOsDefault };
			return true;
		}

		static bool TrySelectScript(string store, string source, bool isWindows, Func<string, string?> getVariable, out CredentialStoreChoice choice, out string? error)
		{
			choice = null!;

			if (isWindows)
			{
				error = $"'@{store}' ({source}) is a generated sh script for Linux and macOS; on Windows use Windows Credential Manager, '{Local}', or a credentials CLI.";
				return false;
			}

			if (!CredentialsDirectory.TryResolve(isWindows, getVariable, out var directory, out error))
				return false;

			var script = directory + "/" + GetScriptFileName(store);

			if (!CheckScript(script, store, out error))
				return false;

			choice = new CredentialStoreChoice(CredentialStoreKind.CredentialsCli, source)
			{
				Cli       = new CredentialsCliSettings("@" + store, script, string.Empty, null),
				Script    = script,
				Directory = directory,
			};
			return true;
		}

		/// <summary>
		/// A generated script must exist and must not be replaceable by other users: no symbolic link, no group or other
		/// write permission, and the same for its directory.
		/// </summary>
		static bool CheckScript(string script, string store, out string? error)
		{
			if (OperatingSystem.IsWindows())
			{
				error = null;
				return true;
			}

			try
			{
				var info = new FileInfo(script);

				if (!info.Exists && info.LinkTarget == null)
				{
					error = $"'{script}' does not exist. Create it with: dotnet linq2db credentials cli init --store {store}";
					return false;
				}

				if (!CredentialsDirectory.CheckUnix(Path.GetDirectoryName(script)!, out error))
					return false;

				if (info.LinkTarget != null)
				{
					error = $"'{script}' is a symbolic link; a generated script must be a file of its own. Recreate it with: dotnet linq2db credentials cli init --store {store} --force";
					return false;
				}

				if ((info.UnixFileMode & CredentialsDirectory.OthersWrite) != 0)
				{
					error = $"'{script}' is writable by other users, who could change what it runs. Run: chmod 700 '{script}'";
					return false;
				}

				error = null;
				return true;
			}
			catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
			{
				error = $"Cannot check '{script}': {ex.Message}{CredentialsDirectory.AccessHint(ex)}";
				return false;
			}
		}
	}
}
