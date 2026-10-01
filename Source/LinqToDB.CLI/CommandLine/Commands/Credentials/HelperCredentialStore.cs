using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace LinqToDB.CommandLine.Commands.Credentials
{
	/// <summary>
	/// Credential store backed by an external credential helper: the linq2db-cli credential helper protocol (version 1,
	/// key=value lines) or the compatibility adapter for docker-credential-* programs.
	/// </summary>
	internal sealed class HelperCredentialStore : ICredentialStore
	{
		public const string TargetPrefix = "linq2db/";

		const string DockerNotFound    = "credentials not found in native keychain";
		const string DockerLabelPrefix = "Registry credentials for ";

		static readonly UTF8Encoding _strictUtf8 = new(encoderShouldEmitUTF8Identifier: false, throwOnInvalidBytes: true);

		readonly ICredentialHelperRunner  _runner;
		readonly CredentialHelperProtocol _protocol;

		public HelperCredentialStore(ICredentialHelperRunner runner, CredentialHelperProtocol protocol)
		{
			_runner   = runner;
			_protocol = protocol;
		}

		/// <summary>The resolved helper path (or the configured command when it cannot be resolved), for diagnostics.</summary>
		public string Describe()
		{
			return _runner.Describe();
		}

		string Name => _runner.DisplayName;

		/// <summary>
		/// Validates a target before it is sent to a helper and returns the form that is sent: targets under
		/// <c>linq2db/</c> are case-insensitive and folded to lower case with invariant rules; any other target belongs to
		/// the helper's store and is sent as written.
		/// </summary>
		public static bool TryNormalizeTarget(string target, out string normalized, out string? error)
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

			if (target.StartsWith(TargetPrefix, StringComparison.OrdinalIgnoreCase))
			{
				normalized = target.ToLowerInvariant();

				var name = normalized.Substring(TargetPrefix.Length);

				if (name.Length == 0 || name.Split('/').Any(static segment => segment.Length == 0 || string.Equals(segment, ".", StringComparison.Ordinal) || string.Equals(segment, "..", StringComparison.Ordinal)))
				{
					error = $"Credential target '{target}' must name a profile after '{TargetPrefix}' without leading or trailing '/', empty, '.' or '..' segments.";
					return false;
				}
			}

			error = null;
			return true;
		}

		static bool ValidateValue(string name, string value, out string? error)
		{
			if (value.Any(char.IsControl))
			{
				error = $"Credential {name} must not contain line breaks or other control characters when a credential helper is used.";
				return false;
			}

			error = null;
			return true;
		}

		public bool TryRead(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			if (!TryNormalizeTarget(target, out var normalized, out error))
				return false;

			return _protocol == CredentialHelperProtocol.Docker
				? DockerGet(normalized, out user, out password, out error)
				: Linq2DbGet(normalized, out user, out password, out error);
		}

		public bool TryStore(string profile, string user, string password, out string? error)
		{
			if (!TryNormalizeTarget(TargetPrefix + profile, out var target, out error)
				|| !ValidateValue("user name", user, out error)
				|| !ValidateValue("password", password, out error))
			{
				return false;
			}

			return _protocol == CredentialHelperProtocol.Docker
				? DockerStore(target, user, password, out error)
				: Linq2DbStore(target, user, password, out error);
		}

		public bool TryList(out IReadOnlyList<CredentialProfile> profiles, out IReadOnlyList<string> diagnostics, out string? error)
		{
			profiles    = [];
			diagnostics = [];

			if (!TryListTargets(out var entries, out var listDiagnostics, out error))
				return false;

			profiles = entries
				.Select(static entry => new CredentialProfile(entry.Target.Substring(TargetPrefix.Length), entry.User))
				.OrderBy(static profile => profile.Name, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			diagnostics = listDiagnostics;
			return true;
		}

		public bool TryGetCount(out int count, out string? error)
		{
			count = 0;

			if (!TryListTargets(out var entries, out _, out error))
				return false;

			count = entries.Count;
			return true;
		}

		public bool TryRemove(string profile, out bool removed, out string? error)
		{
			removed = false;

			if (!TryNormalizeTarget(TargetPrefix + profile, out var target, out error))
				return false;

			if (_protocol == CredentialHelperProtocol.Docker)
			{
				// Erasing a missing item fails with some docker helpers (pass) and succeeds with others (secretservice).
				if (!TryListTargets(out var entries, out _, out error))
					return false;

				if (!entries.Exists(entry => string.Equals(entry.Target, target, StringComparison.Ordinal)))
					return true;
			}

			return TryErase(target, out removed, out error);
		}

		public bool TryClear(out int removedCount, out string? error)
		{
			removedCount = 0;

			if (!TryListTargets(out var entries, out _, out error))
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
			return _protocol == CredentialHelperProtocol.Docker
				? DockerErase(target, out removed, out error)
				: Linq2DbErase(target, out removed, out error);
		}

		bool TryListTargets(out List<(string Target, string User)> entries, out List<string> diagnostics, out string? error)
		{
			return _protocol == CredentialHelperProtocol.Docker
				? DockerList(out entries, out diagnostics, out error)
				: Linq2DbList(out entries, out diagnostics, out error);
		}

		// linq2db protocol, version 1.

		bool Linq2DbGet(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			if (!TryRunLinq2Db("get", target, null, null, out var records, out error))
				return false;

			var lines = records.SelectMany(static record => record).ToList();

			if (lines.Count == 0)
			{
				error = $"Credential target '{target}' was not found by credential helper '{Name}'.";
				return false;
			}

			var users     = lines.Where(static line => string.Equals(line.Key, "username", StringComparison.Ordinal)).ToList();
			var passwords = lines.Where(static line => string.Equals(line.Key, "password", StringComparison.Ordinal)).ToList();

			if (users.Count != 1 || passwords.Count != 1)
			{
				error = $"Credential helper '{Name}' returned an invalid answer to 'get' for '{target}': expected exactly one 'username' and one 'password' line.";
				return false;
			}

			user     = users[0].Value;
			password = passwords[0].Value;
			return true;
		}

		bool Linq2DbStore(string target, string user, string password, out string? error)
		{
			return TryRunLinq2Db("store", target, user, password, out _, out error);
		}

		bool Linq2DbErase(string target, out bool removed, out string? error)
		{
			removed = false;

			if (!TryRunLinq2Db("erase", target, null, null, out var records, out error))
				return false;

			var values = records.SelectMany(static record => record).Where(static line => string.Equals(line.Key, "removed", StringComparison.Ordinal)).Select(static line => line.Value).ToList();

			// No 'removed' line: the helper does not know whether the credential existed.
			if (values.Count == 0 || (values.Count == 1 && string.Equals(values[0], "true", StringComparison.Ordinal)))
			{
				removed = true;
				return true;
			}

			if (values.Count == 1 && string.Equals(values[0], "false", StringComparison.Ordinal))
				return true;

			error = $"Credential helper '{Name}' returned an invalid answer to 'erase': 'removed' must be 'true' or 'false' and appear at most once.";
			return false;
		}

		bool Linq2DbList(out List<(string Target, string User)> entries, out List<string> diagnostics, out string? error)
		{
			entries     = [];
			diagnostics = [];

			if (!TryRunLinq2Db("list", null, null, null, out var records, out error))
				return false;

			foreach (var record in records)
			{
				var targets = record.Where(static line => string.Equals(line.Key, "target", StringComparison.Ordinal)).Select(static line => line.Value).ToList();

				if (targets.Count != 1)
				{
					diagnostics.Add($"Credential helper '{Name}' returned a 'list' record without exactly one 'target' line; the record was skipped.");
					continue;
				}

				if (!targets[0].StartsWith(TargetPrefix, StringComparison.Ordinal) || targets[0].Length == TargetPrefix.Length)
					continue;

				var user = record.Where(static line => string.Equals(line.Key, "username", StringComparison.Ordinal)).Select(static line => line.Value).FirstOrDefault() ?? string.Empty;

				entries.Add((targets[0], user));
			}

			return true;
		}

		bool TryRunLinq2Db(string verb, string? target, string? user, string? password, out List<List<KeyValuePair<string, string>>> records, out string? error)
		{
			records = [];

			var request = new StringBuilder("protocol=1\n");

			if (target   != null) request.Append("target=")  .Append(target)  .Append('\n');
			if (user     != null) request.Append("username=").Append(user)    .Append('\n');
			if (password != null) request.Append("password=").Append(password).Append('\n');

			var requestBytes = Encoding.UTF8.GetBytes(request.ToString());
			request.Clear();

			CredentialHelperRunResult result;

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
					error = FailedMessage(result.ExitCode, CredentialHelperProcessRunner.GetFirstLine(Redact(result.ErrorOutput, password)));
					return false;
				}

				if (!TryParseAnswer(result.Output, out records, out var unsupported, out error))
				{
					error = $"Credential helper '{Name}' returned an invalid answer to '{verb}': {error}";
					return false;
				}

				if (unsupported != null)
				{
					error = unsupported switch
					{
						"verb"     => $"Credential helper '{Name}' does not support '{verb}' ({DescribeVerb(verb)}).",
						"protocol" => $"Credential helper '{Name}' does not support credential helper protocol version 1.",
						_          => $"Credential helper '{Name}' reported 'unsupported={unsupported}' for '{verb}'.",
					};
					return false;
				}

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

		/// <summary>
		/// Parses a protocol answer: UTF-8 lines of <c>key=value</c> split at the first <c>=</c>, an optional CR before LF,
		/// records separated by empty lines. A non-empty line without <c>=</c>, a NUL or invalid UTF-8 is a failure.
		/// </summary>
		internal static bool TryParseAnswer(byte[] output, out List<List<KeyValuePair<string, string>>> records, out string? unsupported, out string? error)
		{
			records     = [];
			unsupported = null;

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

			if (text.Length > 0 && text[0] == '\uFEFF')
				text = text.Substring(1);

			var record = new List<KeyValuePair<string, string>>();

			foreach (var rawLine in text.Split('\n'))
			{
				var line = rawLine.EndsWith('\r') ? rawLine.Substring(0, rawLine.Length - 1) : rawLine;

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

				// Values are printed (user names in "credentials list") and used in connection strings: no escape sequences.
				if (value.Any(char.IsControl))
				{
					error = "a value contains a control character.";
					return false;
				}

				if (string.Equals(key, "unsupported", StringComparison.Ordinal))
					unsupported = value;

				record.Add(new KeyValuePair<string, string>(key, value));
			}

			if (record.Count > 0)
				records.Add(record);

			error = null;
			return true;
		}

		/// <summary>
		/// Removes a secret the client sent from a helper's error output: a helper that echoes its input on failure (a shell
		/// trace, a debug print) must not turn the error message into a password leak. The JSON-escaped form is removed too,
		/// since the docker adapter sends the secret inside JSON.
		/// </summary>
		internal static string Redact(string text, string? secret)
		{
			if (string.IsNullOrEmpty(secret))
				return text;

			text = text.Replace(secret, "***", StringComparison.Ordinal);

			var escaped = JsonEncodedText.Encode(secret).ToString();

			return string.Equals(escaped, secret, StringComparison.Ordinal) ? text : text.Replace(escaped, "***", StringComparison.Ordinal);
		}

		string FailedMessage(int exitCode, string? errorLine)
		{
			return errorLine != null
				? $"Credential helper '{Name}' failed with exit code {exitCode.ToString(CultureInfo.InvariantCulture)}: {errorLine}"
				: $"Credential helper '{Name}' failed with exit code {exitCode.ToString(CultureInfo.InvariantCulture)}.";
		}

		// Docker credential helper adapter: Docker's own contract (ServerURL on stdin, JSON answers, errors on stdout).

		bool DockerGet(string target, out string? user, out string? password, out string? error)
		{
			user     = null;
			password = null;

			var result = RunDocker("get", Encoding.UTF8.GetBytes(target));

			try
			{
				if (result.Failure != null)
				{
					error = result.Failure;
					return false;
				}

				if (result.ExitCode != 0)
				{
					error = IsDockerNotFound(result.Output)
						? $"Credential target '{target}' was not found by credential helper '{Name}' (or the keyring is locked)."
						: DockerFailedMessage(result);
					return false;
				}

				try
				{
					using var json = JsonDocument.Parse(result.Output);

					if (json.RootElement.ValueKind == JsonValueKind.Object
						&& json.RootElement.TryGetProperty("Username", out var userElement)
						&& json.RootElement.TryGetProperty("Secret",   out var secretElement)
						&& userElement  .ValueKind == JsonValueKind.String
						&& secretElement.ValueKind == JsonValueKind.String)
					{
						user     = userElement  .GetString();
						password = secretElement.GetString();

						if (user!.Any(char.IsControl) || password!.Any(char.IsControl))
						{
							user     = null;
							password = null;
							error    = $"Credential helper '{Name}' returned an invalid answer to 'get': a value contains a control character.";
							return false;
						}

						error = null;
						return true;
					}
				}
				catch (JsonException)
				{
				}

				error = $"Credential helper '{Name}' returned an invalid answer to 'get': expected a JSON object with 'Username' and 'Secret'.";
				return false;
			}
			finally
			{
				result.ClearOutput();
			}
		}

		bool DockerStore(string target, string user, string password, out string? error)
		{
			var buffer = new ArrayBufferWriter<byte>();

			using (var writer = new Utf8JsonWriter(buffer))
			{
				writer.WriteStartObject();
				writer.WriteString("ServerURL", target);
				writer.WriteString("Username",  user);
				writer.WriteString("Secret",    password);
				writer.WriteEndObject();
			}

			var request = buffer.WrittenSpan.ToArray();
			// Clear() also zeroes the written bytes.
			buffer.Clear();

			var result = RunDocker("store", request);

			try
			{
				if (result.Failure != null)
				{
					error = result.Failure;
					return false;
				}

				if (result.ExitCode != 0)
				{
					error = DockerFailedMessage(result, password);
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

		bool DockerErase(string target, out bool removed, out string? error)
		{
			removed = false;

			var result = RunDocker("erase", Encoding.UTF8.GetBytes(target));

			try
			{
				if (result.Failure != null)
				{
					error = result.Failure;
					return false;
				}

				if (result.ExitCode != 0)
				{
					error = DockerFailedMessage(result);
					return false;
				}

				removed = true;
				error   = null;
				return true;
			}
			finally
			{
				result.ClearOutput();
			}
		}

		bool DockerList(out List<(string Target, string User)> entries, out List<string> diagnostics, out string? error)
		{
			entries     = [];
			diagnostics = [];

			var result = RunDocker("list", []);

			try
			{
				if (result.Failure != null)
				{
					error = result.Failure;
					return false;
				}

				if (result.ExitCode != 0)
				{
					error = DockerFailedMessage(result);
					return false;
				}

				JsonDocument json;

				try
				{
					json = JsonDocument.Parse(result.Output);
				}
				catch (JsonException)
				{
					error = $"Credential helper '{Name}' returned an invalid answer to 'list': expected a JSON object.";
					return false;
				}

				using (json)
				{
					if (json.RootElement.ValueKind != JsonValueKind.Object)
					{
						error = $"Credential helper '{Name}' returned an invalid answer to 'list': expected a JSON object.";
						return false;
					}

					foreach (var property in json.RootElement.EnumerateObject())
					{
						// docker-credential-secretservice lists item labels ("Registry credentials for <url>"); other helpers
						// list the ServerURL itself.
						var key = property.Name.StartsWith(DockerLabelPrefix, StringComparison.Ordinal)
							? property.Name.Substring(DockerLabelPrefix.Length)
							: property.Name;

						if (key.StartsWith(TargetPrefix, StringComparison.Ordinal) && key.Length > TargetPrefix.Length)
						{
							var listedUser = property.Value.ValueKind == JsonValueKind.String ? property.Value.GetString() ?? string.Empty : string.Empty;

							if (key.Any(char.IsControl) || listedUser.Any(char.IsControl))
							{
								error = $"Credential helper '{Name}' returned an invalid answer to 'list': a value contains a control character.";
								return false;
							}

							entries.Add((key, listedUser));
							continue;
						}

						if (key.Contains(TargetPrefix, StringComparison.Ordinal))
						{
							// An unknown label format: failing is safer than silently dropping one of our credentials.
							error = $"Credential helper '{Name}' listed an entry '{property.Name}' in a format the docker adapter does not recognize.";
							return false;
						}
					}
				}

				error = null;
				return true;
			}
			finally
			{
				result.ClearOutput();
			}
		}

		CredentialHelperRunResult RunDocker(string verb, byte[] request)
		{
			try
			{
				return _runner.Run(verb, request);
			}
			finally
			{
				CryptographicOperations.ZeroMemory(request);
			}
		}

		static bool IsDockerNotFound(byte[] output)
		{
			return string.Equals(Encoding.UTF8.GetString(output).Trim(), DockerNotFound, StringComparison.Ordinal);
		}

		string DockerFailedMessage(CredentialHelperRunResult result, string? sentSecret = null)
		{
			// Docker helpers print their error message on standard output. A failed run carries no credential, so its first
			// line is shown when standard error is empty.
			return FailedMessage(
				result.ExitCode,
				CredentialHelperProcessRunner.GetFirstLine(Redact(result.ErrorOutput, sentSecret))
				?? CredentialHelperProcessRunner.GetFirstLine(Redact(Encoding.UTF8.GetString(result.Output), sentSecret)));
		}
	}
}
