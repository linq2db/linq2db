using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Manages credential profiles in the current user's system credential store.
	/// </summary>
	sealed class CredentialsCommand : CliCommand
	{
		static readonly OptionCategory _credentialOptions   = new(1, "Credential",   "Credential profile options",           "credential");
		static readonly OptionCategory _confirmationOptions = new(2, "Confirmation", "Destructive operation confirmation", "confirmation");

		static readonly CliOption _profile = new StringCliOption(
			"profile",
			null,
			false,
			false,
			"credential profile name without the automatic linq2db/ target prefix");

		static readonly CliOption _user = new StringCliOption(
			"user",
			null,
			false,
			false,
			"database user name stored in the credential profile");

		static readonly CliOption _force = new BooleanCliOption(
			"force",
			null,
			false,
			"remove all linq2db credential profiles without an interactive confirmation",
			null,
			null,
			null,
			false,
			false);

		public static CliCommand Instance { get; } = new CredentialsCommand();

		CredentialsCommand()
			: base(
				"credentials",
				true,
				false,
				"<set|list|remove|clear> [options]",
				"manage encrypted linq2db credential profiles in the current user's system credential store",
				[
					new("dotnet linq2db credentials set --profile project-a/production-read --user ProjectReader", "securely prompts for and stores a password"),
					new("dotnet linq2db credentials list", "lists linq2db credential profile names and users"),
					new("dotnet linq2db credentials remove --profile project-a/production-read", "removes one credential profile"),
					new("dotnet linq2db credentials clear", "removes all linq2db credential profiles after confirmation"),
				],
				true)
		{
			AddOption(_credentialOptions,   _profile);
			AddOption(_credentialOptions,   _user);
			AddOption(_confirmationOptions, _force);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			options.Remove(_profile, out var profileValue);
			options.Remove(_user,    out var userValue);
			options.Remove(_force,   out var forceValue);

			if (options.Count > 0)
				throw new InvalidOperationException($"Not all options handled by {Name} command");

			if (unknownArgs.Count != 1)
			{
				await environment.Error.WriteLineAsync("Credentials operation must be one of: set, list, remove, clear.");
				return StatusCodes.INVALID_ARGUMENTS;
			}

			var operation = unknownArgs.Single();
			var profile   = (string?)profileValue;
			var user      = (string?)userValue;
			var force     = (bool?)forceValue ?? false;

			switch (operation)
			{
				case "set":
					if (!ValidateProfile(environment, profile) || string.IsNullOrWhiteSpace(user))
					{
						if (string.IsNullOrWhiteSpace(user))
							await environment.Error.WriteLineAsync("Option '--user' must be specified for credentials set.");

						return StatusCodes.INVALID_ARGUMENTS;
					}

					if (force)
					{
						await environment.Error.WriteLineAsync("Option '--force' is supported only by credentials clear.");
						return StatusCodes.INVALID_ARGUMENTS;
					}

					if (!environment.TryReadSecret("Password: ", out var password, out var passwordError))
					{
						await environment.Error.WriteLineAsync(passwordError);
						return StatusCodes.EXPECTED_ERROR;
					}

					if (!environment.TryReadSecret("Confirm password: ", out var confirmation, out var confirmationError))
					{
						await environment.Error.WriteLineAsync(confirmationError);
						return StatusCodes.EXPECTED_ERROR;
					}

					if (!string.Equals(password, confirmation, StringComparison.Ordinal))
					{
						await environment.Error.WriteLineAsync("Passwords do not match.");
						return StatusCodes.INVALID_ARGUMENTS;
					}

					if (!environment.CredentialStore.TryStore(profile!, user, password!, out var storeError))
					{
						await environment.Error.WriteLineAsync(storeError);
						return StatusCodes.EXPECTED_ERROR;
					}

					await environment.Out.WriteLineAsync($"Stored credential profile '{profile}' as target 'linq2db/{profile}'.");
					return StatusCodes.SUCCESS;

				case "list":
					if (profile != null || user != null || force)
					{
						await environment.Error.WriteLineAsync("Credentials list does not accept '--profile', '--user', or '--force'.");
						return StatusCodes.INVALID_ARGUMENTS;
					}

					if (!environment.CredentialStore.TryList(out var profiles, out var listError))
					{
						await environment.Error.WriteLineAsync(listError);
						return StatusCodes.EXPECTED_ERROR;
					}

					if (profiles.Count == 0)
					{
						await environment.Out.WriteLineAsync("No linq2db credential profiles found.");
						return StatusCodes.SUCCESS;
					}

					var profileWidth = Math.Max("PROFILE".Length, profiles.Max(static credential => credential.Name.Length));

					await environment.Out.WriteLineAsync($"{"PROFILE".PadRight(profileWidth)}  USER");

					foreach (var credential in profiles)
						await environment.Out.WriteLineAsync($"{credential.Name.PadRight(profileWidth)}  {credential.User}");

					return StatusCodes.SUCCESS;

				case "remove":
					if (!ValidateProfile(environment, profile))
						return StatusCodes.INVALID_ARGUMENTS;

					if (user != null || force)
					{
						await environment.Error.WriteLineAsync("Credentials remove accepts only '--profile'.");
						return StatusCodes.INVALID_ARGUMENTS;
					}

					if (!environment.CredentialStore.TryRemove(profile!, out var removed, out var removeError))
					{
						await environment.Error.WriteLineAsync(removeError);
						return StatusCodes.EXPECTED_ERROR;
					}

					if (!removed)
					{
						await environment.Error.WriteLineAsync($"Credential profile '{profile}' was not found.");
						return StatusCodes.EXPECTED_ERROR;
					}

					await environment.Out.WriteLineAsync($"Removed credential profile '{profile}'.");
					return StatusCodes.SUCCESS;

				case "clear":
					if (profile != null || user != null)
					{
						await environment.Error.WriteLineAsync("Credentials clear does not accept '--profile' or '--user'.");
						return StatusCodes.INVALID_ARGUMENTS;
					}

					if (!environment.CredentialStore.TryList(out profiles, out listError))
					{
						await environment.Error.WriteLineAsync(listError);
						return StatusCodes.EXPECTED_ERROR;
					}

					if (profiles.Count == 0)
					{
						await environment.Out.WriteLineAsync("No linq2db credential profiles found.");
						return StatusCodes.SUCCESS;
					}

					if (!force)
					{
						await environment.Error.WriteAsync($"Remove all {profiles.Count} linq2db credential profiles? [y/N] ");

						var answer = environment.ReadLine();

						if (!string.Equals(answer, "y", StringComparison.OrdinalIgnoreCase)
							&& !string.Equals(answer, "yes", StringComparison.OrdinalIgnoreCase))
						{
							await environment.Out.WriteLineAsync("Credential profiles were not removed.");
							return StatusCodes.SUCCESS;
						}
					}

					if (!environment.CredentialStore.TryClear(out var removedCount, out var clearError))
					{
						await environment.Error.WriteLineAsync(clearError);
						return StatusCodes.EXPECTED_ERROR;
					}

					await environment.Out.WriteLineAsync($"Removed {removedCount.ToString(CultureInfo.InvariantCulture)} linq2db credential profile(s).");
					return StatusCodes.SUCCESS;

				default:
					await environment.Error.WriteLineAsync($"Unknown credentials operation '{operation}'. Expected set, list, remove, or clear.");
					return StatusCodes.INVALID_ARGUMENTS;
			}
		}

		static bool ValidateProfile(ICliEnvironment environment, string? profile)
		{
			if (string.IsNullOrWhiteSpace(profile))
			{
				environment.Error.WriteLine("Option '--profile' must be specified.");
				return false;
			}

			if (profile.StartsWith("linq2db/", StringComparison.OrdinalIgnoreCase))
			{
				environment.Error.WriteLine("Option '--profile' must not include the automatic 'linq2db/' target prefix.");
				return false;
			}

			if (profile[0] == '/'
				|| profile[^1] == '/'
				|| profile.Any(char.IsControl))
			{
				environment.Error.WriteLine("Option '--profile' must be a non-empty target suffix without leading/trailing '/' or control characters.");
				return false;
			}

			return true;
		}
	}
}
