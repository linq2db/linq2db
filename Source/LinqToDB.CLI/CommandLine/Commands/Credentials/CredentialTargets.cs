using System;
using System.Linq;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Rules for credential targets and values shared by the stores that do not keep them in Windows Credential Manager.
	/// </summary>
	internal static class CredentialTargets
	{
		public const string Prefix = "linq2db/";

		/// <summary>
		/// Validates a target and returns the form a store keeps: targets under <c>linq2db/</c> are case-insensitive and
		/// folded to lower case with invariant rules; any other target is kept as written.
		/// </summary>
		public static bool TryNormalize(string target, out string normalized, out string? error)
		{
			normalized = target;

			if (string.IsNullOrEmpty(target))
			{
				error = "Credential target must not be empty.";
				return false;
			}

			if (target.Any(char.IsControl))
			{
				error = "Credential target must not contain control characters.";
				return false;
			}

			if (target[0] == '-')
			{
				error = $"Credential target '{target}' must not start with '-'.";
				return false;
			}

			if (IsLinq2Db(target))
			{
				normalized = target.ToLowerInvariant();

				var name = normalized.Substring(Prefix.Length);

				if (name.Length == 0 || name.Split('/').Any(static segment => segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal)))
				{
					error = $"Credential target '{target}' must name a record after '{Prefix}' without leading or trailing '/', empty, '.' or '..' segments.";
					return false;
				}
			}

			error = null;
			return true;
		}

		/// <summary>
		/// Validates a <c>linq2db/</c> target for Windows Credential Manager, which keeps targets as written. Only the rules of
		/// earlier versions apply (a name after the prefix, no leading or trailing '/', no control characters), so records
		/// they stored, such as <c>linq2db/a//b</c> or <c>linq2db/../prod</c>, stay manageable one by one. Empty, '.' and
		/// '..' segments matter only to the stores that map a name to a path.
		/// </summary>
		public static bool TryValidateForCredentialManager(string target, out string? error)
		{
			var name = IsLinq2Db(target) ? target.Substring(Prefix.Length) : string.Empty;

			if (string.IsNullOrWhiteSpace(name) || name[0] == '/' || name[^1] == '/' || name.Any(char.IsControl))
			{
				error = $"Credential target '{target}' must name a record after '{Prefix}' without leading or trailing '/' or control characters.";
				return false;
			}

			error = null;
			return true;
		}

		/// <summary>Whether a target is in linq2db's own namespace (<c>linq2db/</c>, any case).</summary>
		public static bool IsLinq2Db(string target)
		{
			return target.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase);
		}

		/// <summary>Refuses a user name or password with a line break or another control character.</summary>
		public static bool ValidateValue(string name, string value, out string? error)
		{
			if (value.Any(char.IsControl))
			{
				error = $"Credential {name} must not contain line breaks or other control characters.";
				return false;
			}

			error = null;
			return true;
		}
	}
}
