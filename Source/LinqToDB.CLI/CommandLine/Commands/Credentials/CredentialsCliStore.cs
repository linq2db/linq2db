using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Credential store backed by a credentials CLI: any program speaking the linq2db credentials CLI protocol, version 1
	/// (<c>key=value</c> lines; the answer starts with <c>protocol=1</c> and <c>status=&lt;status&gt;</c>).
	/// </summary>
	internal sealed class CredentialsCliStore : ICredentialStore
	{
		public const int ProtocolVersion = 1;

		const string StatusOk          = "ok";
		const string StatusNotFound    = "not-found";
		const string StatusUnsupported = "unsupported";
		const string StatusError       = "error";

		static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

		/// <summary>A compiler diagnostic, as <c>dotnet run</c> prints it when it builds a file-based app.</summary>
		static readonly Regex _buildOutput = new(@"^(?<diagnostic>.+\(\d+,\d+\): (?:warning|error) [A-Z]+\d+):", RegexOptions.CultureInvariant | RegexOptions.ExplicitCapture, TimeSpan.FromSeconds(1));

		readonly ICredentialsCliRunner _runner;

		public CredentialsCliStore(ICredentialsCliRunner runner)
		{
			_runner = runner;
		}

		/// <summary>The resolved program and arguments (or the configured value when the program cannot be resolved), for diagnostics.</summary>
		public string Describe()
		{
			return _runner.Describe();
		}

		string Name => _runner.DisplayName;

		public bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			if (!CredentialTargets.TryNormalize(target, out var normalized, out error))
				return false;

			if (!TryRun("get", normalized, null, null, out var status, out var records, out error))
				return false;

			if (string.Equals(status, StatusNotFound, StringComparison.Ordinal))
			{
				error = $"Credential target '{normalized}' was not found by credentials CLI '{Name}'.";
				return false;
			}

			var lines     = records.SelectMany(static record => record).ToList();
			var users     = lines.Where(static line => string.Equals(line.Key, "username", StringComparison.Ordinal)).ToList();
			var passwords = lines.Where(static line => string.Equals(line.Key, "password", StringComparison.Ordinal)).ToList();

			if (users.Count != 1 || passwords.Count != 1)
			{
				error = InvalidAnswer("get", "status=ok needs exactly one 'username' and one 'password' line.");
				return false;
			}

			user     = users[0].Value;
			password = passwords[0].Value;
			return true;
		}

		public bool TryStore(string name, string user, string password, out string? error)
		{
			if (!CredentialTargets.TryNormalize(CredentialTargets.Prefix + name, out var target, out error)
				|| !CredentialTargets.ValidateValue("user name", user, out error)
				|| !CredentialTargets.ValidateValue("password", password, out error))
			{
				return false;
			}

			return TryRun("store", target, user, password, out _, out _, out error);
		}

		public bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error)
		{
			profiles    = [];
			diagnostics = [];

			if (!TryListTargets(out var entries, out error))
				return false;

			profiles = entries
				.Select(static entry => new CredentialProfile(entry.Target.Substring(CredentialTargets.Prefix.Length), entry.User))
				.OrderBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			return true;
		}

		public bool TryGetCount(out int count, out string? error)
		{
			count = 0;

			if (!TryListTargets(out var entries, out error))
				return false;

			count = entries.Count;
			return true;
		}

		public bool TryRemove(string name, out bool removed, out string? error)
		{
			removed = false;

			if (!CredentialTargets.TryNormalize(CredentialTargets.Prefix + name, out var target, out error))
				return false;

			return TryErase(target, out removed, out error);
		}

		public bool TryClear(out int removedCount, out string? error)
		{
			removedCount = 0;

			if (!TryListTargets(out var entries, out error))
				return false;

			// Only linq2db's own targets are listed here; nothing else in a shared store is ever erased.
			foreach (var (target, _) in entries)
			{
				if (!TryErase(target, out var removed, out error))
					return false;

				if (removed)
					removedCount++;
			}

			return true;
		}

		bool TryErase(string target, out bool removed, out string? error)
		{
			removed = false;

			if (!TryRun("erase", target, null, null, out var status, out _, out error))
				return false;

			removed = string.Equals(status, StatusOk, StringComparison.Ordinal);
			return true;
		}

		bool TryListTargets(out List<(string Target, string User)> entries, out string? error)
		{
			entries = [];

			if (!TryRun("list", null, null, null, out _, out var records, out error))
				return false;

			foreach (var record in records)
			{
				var duplicate = record.GroupBy(static line => line.Key, StringComparer.Ordinal).FirstOrDefault(static group => group.Skip(1).Any());

				if (duplicate != null)
				{
					error = InvalidAnswer("list", $"a record repeats '{duplicate.Key}'.");
					return false;
				}

				var target = record.Where(static line => string.Equals(line.Key, "target", StringComparison.Ordinal)).Select(static line => line.Value).FirstOrDefault();

				if (target == null)
				{
					error = InvalidAnswer("list", "a record has no 'target'.");
					return false;
				}

				if (!target.StartsWith(CredentialTargets.Prefix, StringComparison.Ordinal) || target.Length == CredentialTargets.Prefix.Length)
					continue;

				var user = record.Where(static line => string.Equals(line.Key, "username", StringComparison.Ordinal)).Select(static line => line.Value).FirstOrDefault() ?? string.Empty;

				entries.Add((target, user));
			}

			return true;
		}

		/// <summary>
		/// Runs one verb and validates the answer: exit code 0, <c>protocol=1</c>, a known <c>status</c> valid for the verb,
		/// and nothing after a status other than <c>ok</c>. Returns the status and, for <c>ok</c>, the records after the header.
		/// </summary>
		bool TryRun(string verb, string? target, string? user, string? password, out string status, out List<List<KeyValuePair<string, string>>> records, out string? error)
		{
			status  = string.Empty;
			records = [];

			var request = new StringBuilder();

			request.Append("protocol=").Append(ProtocolVersion.ToString(CultureInfo.InvariantCulture)).Append('\n');
			request.Append("verb=").Append(verb).Append('\n');

			if (target   != null) request.Append("target=")  .Append(target)  .Append('\n');
			if (user     != null) request.Append("username=").Append(user)    .Append('\n');
			if (password != null) request.Append("password=").Append(password).Append('\n');

			var requestBytes = Encoding.UTF8.GetBytes(request.ToString());
			request.Clear();

			CredentialsCliRunResult result;

			try
			{
				result = _runner.Run(verb, requestBytes);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(requestBytes);
			}

			try
			{
				if (result.Failure != null)
				{
					error = result.Failure;
					return false;
				}

				if (result.ExitCode != 0)
				{
					error = FailedMessage(result.ExitCode, ErrorLine(result, password));
					return false;
				}

				if (!TryParseAnswer(result.Output, out var protocol, out status, out records, out error))
				{
					error = InvalidAnswer(verb, error!);
					return false;
				}

				if (protocol != ProtocolVersion)
				{
					// Only a version mismatch reported as unsupported is a valid answer from another protocol version.
					error = string.Equals(status, StatusUnsupported, StringComparison.Ordinal)
						? $"Credentials CLI '{Name}' speaks protocol {protocol.ToString(CultureInfo.InvariantCulture)}, linq2db-cli speaks {ProtocolVersion.ToString(CultureInfo.InvariantCulture)}."
						: InvalidAnswer(verb, $"the first line is protocol={protocol.ToString(CultureInfo.InvariantCulture)}, expected protocol={ProtocolVersion.ToString(CultureInfo.InvariantCulture)}.");
					return false;
				}

				if (!string.Equals(status, StatusOk, StringComparison.Ordinal) && records.Count > 0)
				{
					error = InvalidAnswer(verb, $"status={status} must not be followed by other lines.");
					return false;
				}

				switch (status)
				{
					case StatusOk:
						break;

					case StatusNotFound:
						if (verb is not ("get" or "erase"))
						{
							error = InvalidAnswer(verb, $"status={StatusNotFound} is not an answer to '{verb}'.");
							return false;
						}

						break;

					case StatusUnsupported:
						error = $"Credentials CLI '{Name}' does not support '{verb}' ({DescribeVerb(verb)}).";
						return false;

					default:
						var line = ErrorLine(result, password);

						error = line != null
							? $"Credentials CLI '{Name}' reported an error: {line}"
							: $"Credentials CLI '{Name}' reported an error without a message on standard error.";
						return false;
				}

				error = null;
				return true;
			}
			finally
			{
				result.ClearOutput();
			}
		}

		static string DescribeVerb(string verb)
		{
			return verb switch
			{
				"get"   => "reading credentials",
				"store" => "storing credentials",
				"erase" => "removing credentials",
				"list"  => "listing credentials",
				_       => verb,
			};
		}

		string InvalidAnswer(string verb, string detail)
		{
			return $"Credentials CLI '{Name}' returned an invalid answer to '{verb}': {detail}";
		}

		/// <summary>
		/// Parses an answer: UTF-8 lines with an optional CR before LF and an optional byte order mark; line 1
		/// <c>protocol=&lt;N&gt;</c>, line 2 <c>status=&lt;status&gt;</c>, then <c>key=value</c> lines split at the first
		/// <c>=</c>, in records separated by empty lines. The output itself is never put into a message: it can hold a secret.
		/// </summary>
		internal static bool TryParseAnswer(byte[] output, out int protocol, out string status, out List<List<KeyValuePair<string, string>>> records, out string? error)
		{
			protocol = 0;
			status   = string.Empty;
			records  = [];

			string text;

			try
			{
				text = _strictUtf8.GetString(output);
			}
			catch (DecoderFallbackException)
			{
				error = "the output is not valid UTF-8.";
				return false;
			}

			if (text.Length > 0 && text[0] == '﻿')
				text = text.Substring(1);

			if (text.Length == 0)
			{
				error = "stdout is empty.";
				return false;
			}

			var lines = text.Split('\n').Select(static line => line.EndsWith('\r') ? line.Substring(0, line.Length - 1) : line).ToArray();

			if (!lines[0].StartsWith("protocol=", StringComparison.Ordinal)
				|| !int.TryParse(lines[0].AsSpan("protocol=".Length), NumberStyles.None, CultureInfo.InvariantCulture, out protocol))
			{
				var match = _buildOutput.Match(lines[0]);

				error = match.Success
					? $"the first line looks like build output: `{Shorten(match.Groups["diagnostic"].Value)}`."
					: $"the first line is not protocol={ProtocolVersion.ToString(CultureInfo.InvariantCulture)} ({lines[0].Length.ToString(CultureInfo.InvariantCulture)} characters).";
				return false;
			}

			if (lines.Length < 2 || !lines[1].StartsWith("status=", StringComparison.Ordinal))
			{
				error = "the second line is not status=<ok|not-found|unsupported|error>.";
				return false;
			}

			status = lines[1].Substring("status=".Length);

			if (status is not (StatusOk or StatusNotFound or StatusUnsupported or StatusError))
			{
				error = $"the second line has an unknown status ({status.Length.ToString(CultureInfo.InvariantCulture)} characters); expected ok, not-found, unsupported or error.";
				status = string.Empty;
				return false;
			}

			var record = new List<KeyValuePair<string, string>>();

			foreach (var line in lines.Skip(2))
			{
				if (line.Length == 0)
				{
					if (record.Count > 0)
					{
						records.Add(record);
						record = [];
					}

					continue;
				}

				if (line.Contains('\0', StringComparison.Ordinal))
				{
					error = "the output contains a NUL character.";
					return false;
				}

				var separator = line.IndexOf('=', StringComparison.Ordinal);

				if (separator < 0)
				{
					error = "a non-empty line has no '='.";
					return false;
				}

				var key   = line.Substring(0, separator);
				var value = line.Substring(separator + 1);

				if (key is "protocol" or "status")
				{
					error = $"'{key}' appears more than once.";
					return false;
				}

				// Values are printed (user names in "credentials list") and used in connection strings: no escape sequences.
				if (value.Any(char.IsControl))
				{
					error = "a value contains a control character.";
					return false;
				}

				record.Add(new KeyValuePair<string, string>(key, value));
			}

			if (record.Count > 0)
				records.Add(record);

			error = null;
			return true;
		}

		static string Shorten(string text)
		{
			return text.Length > CredentialsCliProcessRunner.MaxErrorLineLength
				? string.Concat(text.AsSpan(0, CredentialsCliProcessRunner.MaxErrorLineLength), "...")
				: text;
		}

		/// <summary>
		/// The first line of the program's standard error for an error message, with a secret the client sent removed before
		/// the line is trimmed or shortened.
		/// </summary>
		static string? ErrorLine(CredentialsCliRunResult result, string? secret)
		{
			return CredentialsCliProcessRunner.GetFirstLine(Redact(result.ErrorOutput, secret, result.ErrorOutputTruncated));
		}

		/// <summary>
		/// Removes a secret the client sent from the program's error output: a program that echoes its input on failure (a
		/// shell trace, a debug print) must not turn the error message into a password leak.
		/// </summary>
		/// <param name="text">Program output.</param>
		/// <param name="secret">Secret to remove.</param>
		/// <param name="truncated">
		/// <see langword="true"/> when <paramref name="text"/> was cut at a length limit: a secret crossing the cut is only
		/// partly present, so a tail that is a prefix of the secret is removed as well.
		/// </param>
		internal static string Redact(string text, string? secret, bool truncated = false)
		{
			if (string.IsNullOrEmpty(secret))
				return text;

			text = text.Replace(secret, "***", StringComparison.Ordinal);

			if (truncated)
			{
				// The cut can also split a multi-byte character, which decodes as U+FFFD.
				var end = text.TrimEnd('�');

				for (var length = Math.Min(secret.Length - 1, end.Length); length > 0; length--)
				{
					if (end.AsSpan().EndsWith(secret.AsSpan(0, length), StringComparison.Ordinal))
					{
						text = string.Concat(end.AsSpan(0, end.Length - length), "***");
						break;
					}
				}
			}

			return text;
		}

		string FailedMessage(int exitCode, string? errorLine)
		{
			return errorLine != null
				? $"Credentials CLI '{Name}' failed with exit code {exitCode.ToString(CultureInfo.InvariantCulture)}: {errorLine}"
				: $"Credentials CLI '{Name}' failed with exit code {exitCode.ToString(CultureInfo.InvariantCulture)}.";
		}
	}
}
