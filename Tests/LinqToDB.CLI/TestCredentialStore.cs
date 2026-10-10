using System;
using System.Collections.Generic;
using System.Linq;

using LinqToDB.CommandLine.Commands.Credentials;

namespace Tests.LinqToDB.CLI
{
	internal sealed class TestCredentialStore : ICredentialStore
	{
		public Dictionary<string, (string User, string Password)> Credentials { get; } = new(StringComparer.Ordinal);
		public HashSet<string> UnreadableTargets { get; } = new(StringComparer.Ordinal);

		public bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			if (Credentials.TryGetValue(target, out var credentials))
			{
				user     = credentials.User;
				password = credentials.Password;
				error    = null;
				return true;
			}

			user     = null;
			password = null;
			error    = $"Credential target '{target}' was not found for the current Windows account.";
			return false;
		}

		public bool TryStore(string profile, string user, string password, out string? error)
		{
			Credentials[$"linq2db/{profile}"] = (user, password);
			error = null;
			return true;
		}

		public bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error)
		{
			profiles = Credentials
				.Where(static credential => credential.Key.StartsWith("linq2db/", StringComparison.Ordinal))
				.Where(credential => !UnreadableTargets.Contains(credential.Key))
				.Select(static credential => new CredentialProfile(credential.Key.Substring("linq2db/".Length), credential.Value.User))
				.OrderBy(static credential => credential.Name, StringComparer.Ordinal)
				.ToArray();
			diagnostics = UnreadableTargets
				.Where(static target => target.StartsWith("linq2db/", StringComparison.Ordinal))
				.Select(static target => $"Credential target '{target}' cannot be decoded.")
				.ToArray();
			error = null;
			return true;
		}

		public bool TryGetCount(out int count, out string? error)
		{
			count = Credentials.Keys.Count(static target => target.StartsWith("linq2db/", StringComparison.Ordinal));
			error = null;
			return true;
		}

		public bool TryRemove(string profile, out bool removed, out string? error)
		{
			removed = Credentials.Remove($"linq2db/{profile}");
			error   = null;
			return true;
		}

		public bool TryClear(out int removedCount, out string? error)
		{
			var targets = Credentials.Keys.Where(static target => target.StartsWith("linq2db/", StringComparison.Ordinal)).ToArray();

			foreach (var target in targets)
			{
				Credentials.Remove(target);
				UnreadableTargets.Remove(target);
			}

			removedCount = targets.Length;
			error        = null;
			return true;
		}
	}
}
