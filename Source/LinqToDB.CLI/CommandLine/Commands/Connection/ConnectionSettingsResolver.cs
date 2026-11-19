using System;
using System.Globalization;
using System.IO;
using System.Text;

using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.Connection
{
	/// <summary>
	/// Resolves trusted connection options from command/MCP inputs and optional configuration profiles.
	/// </summary>
	internal sealed class ConnectionSettingsResolver(ICliEnvironment environment)
	{
		internal const string MissingEnvironmentVariable = "\u0000";

		readonly ICliEnvironment _environment = environment;

		public int ErrorStatusCode { get; private set; } = StatusCodes.INVALID_ARGUMENTS;

		public ConnectionSettings? Resolve(ConnectionOptionValues values)
		{
			ErrorStatusCode = StatusCodes.INVALID_ARGUMENTS;

			var profileName = values.Profile ?? QueryExecutionDefaults.DefaultProfileName;

			QueryExecutionConfiguration? configuration = null;

			if (values is { Profile: not null, Config: null })
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Profile.Name}' requires option '--{QueryExecutionCliOptions.Config.Name}'.");
				return null;
			}

			var configFileName = ResolvePath(QueryExecutionCliOptions.Config, values.Config);

			if (string.Equals(configFileName, MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			if (configFileName != null && !QueryExecutionConfiguration.TryLoad(_environment, configFileName, profileName, out configuration, out var error))
			{
				_environment.Error.WriteLine(error);
				return null;
			}

			var configDirectory     = configFileName != null ? Path.GetDirectoryName(configFileName) : null;
			var providerName        = values.Provider ?? configuration?.Provider;
			var providerLocation    = values.ProviderLocation != null
				? ResolvePath(QueryExecutionCliOptions.ProviderLocation, values.ProviderLocation)
				: ResolvePath(QueryExecutionCliOptions.ProviderLocation, configuration?.ProviderLocation, configDirectory);
			var connectionString    = GetConfiguredValue(QueryExecutionCliOptions.ConnectionString, values.ConnectionString, values.ConnectionStringEnv, configuration?.ConnectionString, configuration?.ConnectionStringEnv);
			var credentials         = values.Credentials != null
				? ResolveEnvironmentVariables(QueryExecutionCliOptions.Credentials, values.Credentials)
				: ResolveEnvironmentVariables(QueryExecutionCliOptions.Credentials, configuration?.Credentials);
			string? user;
			string? password;

			if (values.Credentials != null)
			{
				if (HasCommandCredentials(values))
				{
					_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Credentials.Name}' cannot be combined with '--{QueryExecutionCliOptions.User.Name}', '--{QueryExecutionCliOptions.UserEnv.Name}', '--{QueryExecutionCliOptions.Password.Name}', or '--{QueryExecutionCliOptions.PasswordEnv.Name}'.");
					return null;
				}
			}
			else if (configuration?.Credentials != null)
			{
				if (HasCommandCredentials(values))
				{
					_environment.Error.WriteLine("Configuration property 'credentials' cannot be combined with command-line user or password options.");
					return null;
				}

				if (configuration.User != null || configuration.UserEnv != null || configuration.Password != null || configuration.PasswordEnv != null)
				{
					_environment.Error.WriteLine("Configuration property 'credentials' cannot be combined with 'user', 'userEnv', 'password', or 'passwordEnv'. Values inherited from the 'default' profile count as set.");
					return null;
				}
			}

			if (credentials != null && !string.Equals(credentials, MissingEnvironmentVariable, StringComparison.Ordinal))
			{
				if (!_environment.CredentialStore.TryRead(credentials, out user, out password, out var credentialError))
				{
					_environment.Error.WriteLine(credentialError);
					return null;
				}
			}
			else
			{
				user     = GetConfiguredExpandableValue(QueryExecutionCliOptions.User,     values.User,     values.UserEnv,     configuration?.User,     configuration?.UserEnv);
				password = GetConfiguredValue          (QueryExecutionCliOptions.Password, values.Password, values.PasswordEnv, configuration?.Password, configuration?.PasswordEnv);
			}

			var commandTimeout      = values.CommandTimeout != null ? ParseTimeout(QueryExecutionCliOptions.CommandTimeout, values.CommandTimeout) : configuration?.CommandTimeout;
			var lockTimeout         = values.LockTimeout    != null ? ParseTimeout(QueryExecutionCliOptions.LockTimeout,    values.LockTimeout)    : configuration?.LockTimeout;
			var impersonate         = values.Impersonate ?? configuration?.Impersonate ?? false;
			var impersonateMode     = ParseImpersonateMode(values.ImpersonateMode ?? configuration?.ImpersonateMode);

			if (commandTimeout < 0 || lockTimeout < 0)
				return null;

			if (impersonateMode == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.ImpersonateMode.Name}' has unknown value '{values.ImpersonateMode ?? configuration?.ImpersonateMode}'.");
				return null;
			}

			if (string.Equals(connectionString, MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(user,             MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(password,         MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(providerLocation, MissingEnvironmentVariable, StringComparison.Ordinal) ||
			    string.Equals(credentials,      MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			if (providerName == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Provider.Name}' must be specified.");
				return null;
			}

			if (connectionString == null)
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.ConnectionString.Name}' must be specified.");
				return null;
			}

			try
			{
				// The template must hold no placeholder but {0} and {1}.
				_ = string.Format(CultureInfo.InvariantCulture, connectionString, string.Empty, string.Empty);
			}
			catch (FormatException ex)
			{
				_environment.Error.WriteLine($"Invalid connection string format: {ex.Message} Use '{{0}}' for user, '{{1}}' for password, and escape literal braces as '{{{{' and '}}}}'.");
				return null;
			}

			var formatted = FormatCredentials(connectionString, user, password, providerName.Contains("Odbc", StringComparison.OrdinalIgnoreCase), out var formatError);

			if (formatted == null)
			{
				_environment.Error.WriteLine(formatError);
				return null;
			}

			connectionString = formatted;

			if (impersonate && (user == null || password == null))
			{
				_environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Impersonate.Name}' requires resolved '--{QueryExecutionCliOptions.User.Name}' and '--{QueryExecutionCliOptions.Password.Name}' values.");
				return null;
			}

			return new ConnectionSettings(
				profileName,
				providerName,
				providerLocation,
				user,
				password,
				connectionString,
				commandTimeout,
				lockTimeout,
				configDirectory,
				impersonate,
				impersonateMode.Value,
				configuration);

			string? GetConfiguredExpandableValue(CliOption option, string? commandValue, string? commandEnvironmentVariableName, string? configurationValue, string? configurationEnvironmentVariableName)
			{
				if (commandValue                         != null) return ResolveEnvironmentVariables(option, commandValue);
				if (commandEnvironmentVariableName       != null) return GetEnvironmentValue        (option, commandEnvironmentVariableName);
				if (configurationValue                   != null) return ResolveEnvironmentVariables(option, configurationValue);
				if (configurationEnvironmentVariableName != null) return GetEnvironmentValue        (option, configurationEnvironmentVariableName);

				return null;
			}

			string? GetConfiguredValue(CliOption option, string? commandValue, string? commandEnvironmentVariableName, string? configurationValue, string? configurationEnvironmentVariableName)
			{
				if (commandValue                         != null) return commandValue;
				if (commandEnvironmentVariableName       != null) return GetEnvironmentValue(option, commandEnvironmentVariableName);
				if (configurationValue                   != null) return configurationValue;
				if (configurationEnvironmentVariableName != null) return GetEnvironmentValue(option, configurationEnvironmentVariableName);

				return null;
			}

			static WindowsImpersonationMode? ParseImpersonateMode(string? value)
			{
				return value?.ToLower(CultureInfo.InvariantCulture) switch
				{
					null                => WindowsImpersonationMode.NetworkCleartext,
					"8"                 => WindowsImpersonationMode.NetworkCleartext,
					"network-cleartext" => WindowsImpersonationMode.NetworkCleartext,
					"2"                 => WindowsImpersonationMode.Interactive,
					"interactive"       => WindowsImpersonationMode.Interactive,
					"3"                 => WindowsImpersonationMode.Network,
					"network"           => WindowsImpersonationMode.Network,
					"9"                 => WindowsImpersonationMode.NewCredentials,
					"new-credentials"   => WindowsImpersonationMode.NewCredentials,
					_                   => null,
				};
			}

			static bool HasCommandCredentials(ConnectionOptionValues optionValues)
			{
				return optionValues.User        != null
					|| optionValues.UserEnv     != null
					|| optionValues.Password    != null
					|| optionValues.PasswordEnv != null;
			}

			int? ParseTimeout(CliOption option, string value)
			{
				if (int.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out var timeout) && timeout >= 0)
					return timeout;

				_environment.Error.WriteLine($"Option '--{option.Name}' must be a non-negative integer number of seconds.");
				return -1;
			}
		}

		public string? ResolvePath(CliOption option, string? path, string? baseDirectory = null)
		{
			if (path == null)
				return null;

			var resolvedPath = ResolveEnvironmentVariables(option, path);

			if (resolvedPath == null || string.Equals(resolvedPath, MissingEnvironmentVariable, StringComparison.Ordinal))
				return resolvedPath;

			if (!string.IsNullOrEmpty(baseDirectory) && !Path.IsPathRooted(resolvedPath))
				resolvedPath = Path.Combine(baseDirectory, resolvedPath);

			return resolvedPath;
		}

		string GetEnvironmentValue(CliOption option, string environmentVariableName)
		{
			var value = _environment.GetEnvironmentVariable(environmentVariableName);

			if (value != null)
				return value;

			_environment.Error.WriteLine($"Environment variable '{environmentVariableName}' specified for option '--{option.Name}' is not set.");

			return MissingEnvironmentVariable;
		}

		string? ResolveEnvironmentVariables(CliOption option, string? value)
		{
			if (value == null)
				return null;

			var result = new StringBuilder(value.Length);

			for (var i = 0; i < value.Length; i++)
			{
				if (value[i] == '%')
				{
					var end = value.IndexOf('%', i + 1);

					if (end > i + 1)
					{
						if (!TryAppendEnvironmentValue(option, _environment, value.Substring(i + 1, end - i - 1), result))
							return MissingEnvironmentVariable;

						i = end;
						continue;
					}
				}

				if (value[i] == '$' && i + 1 < value.Length && value[i + 1] == '{')
				{
					var end = value.IndexOf('}', i + 2);

					if (end > i + 2)
					{
						if (!TryAppendEnvironmentValue(option, _environment, value.Substring(i + 2, end - i - 2), result))
							return MissingEnvironmentVariable;

						i = end;
						continue;
					}
				}

				result.Append(value[i]);
			}

			return result.ToString();

			static bool TryAppendEnvironmentValue(CliOption option, ICliEnvironment environment, string name, StringBuilder result)
			{
				var value = environment.GetEnvironmentVariable(name);

				if (value == null)
				{
					environment.Error.WriteLine($"Environment variable '{name}' referenced by option '--{option.Name}' is not set.");
					return false;
				}

				result.Append(value);
				return true;
			}
		}

		// The user name and password go into the connection string as one value each, so that a value cannot end its item
		// and add keys of its own (Charset, Pooling, Data Source, Dbq). That holds only where the placeholder is the whole
		// value of its key: Key={1}, or quoted by the template tightly (Key="{1}", Key='{1}'; ODBC: Key={{{1}}}). A
		// placeholder inside a longer value or a key, or with space inside the template's quotes, is refused. A bare value
		// is quoted when it holds a separator, a quote, a brace, an equals sign or outer space: "…" (or '…' when it holds a
		// double quote; both kinds are refused, since AdoNetCore.AseClient knows no doubled quote); ODBC drivers quote with
		// {…} and double a closing brace. In the template's own quotes a value may not hold that quote. An empty value stays
		// empty.
		internal static string? FormatCredentials(string template, string? user, string? password, bool odbc, out string? error)
		{
			error = null;

			// The template with {{ and }} read and each placeholder replaced by a mark that holds its index.
			const char UserMark     = '\u0001';
			const char PasswordMark = '\u0002';

			if (template.AsSpan().IndexOfAny(UserMark, PasswordMark) >= 0)
			{
				error = "The connection string holds a control character the CLI uses for the {0} and {1} placeholders.";
				return null;
			}

			var text = new StringBuilder(template.Length);

			for (var i = 0; i < template.Length; i++)
			{
				var c = template[i];

				if (c is '{' or '}' && i + 1 < template.Length && template[i + 1] == c)
				{
					text.Append(c);
					i++;
				}
				else if (c == '{')
				{
					var close = template.IndexOf('}', i);
					var name  = close < 0 ? null : template.Substring(i + 1, close - i - 1);

					if (name is not ("0" or "1"))
					{
						error = $"The connection string placeholder '{template.Substring(i, close < 0 ? template.Length - i : close - i + 1)}' is not supported: use {{0}} for user and {{1}} for password, with no format.";
						return null;
					}

					text.Append(string.Equals(name, "0", StringComparison.Ordinal) ? UserMark : PasswordMark);
					i = close;
				}
				else
				{
					text.Append(c);
				}
			}

			var unescaped = text.ToString();
			var contexts  = new char[unescaped.Length];

			// A mark no item reads as its value (after the last key, say) stays unread, and is refused below.
			const char Unread = '\uFFFF';

			Array.Fill(contexts, Unread);

			// Read the items as a connection-string parser does, and note for each mark whether it is a whole value: bare
			// ('\0'), or the whole content of a quoted one (the quote, or '{' for ODBC braces).
			var position = 0;

			while (position < unescaped.Length)
			{
				var end = unescaped.IndexOf('=', position);

				if (end < 0)
					break;

				for (var k = position; k < end; k++)
				{
					if (unescaped[k] is UserMark or PasswordMark)
						return NotWholeValue(out error);
				}

				var start = end + 1;

				while (start < unescaped.Length && unescaped[start] is ' ' or '\t')
					start++;

				var quote = start < unescaped.Length ? unescaped[start] : '\0';
				var close = quote switch
				{
					'"' or '\'' when !odbc => QuotedEnd(unescaped, start, quote, quote),
					'{' when odbc          => QuotedEnd(unescaped, start, '{', '}'),
					_                      => -1,
				};

				int next;

				if (close >= 0)
				{
					// Key="…": a mark is the whole content, or none of it.
					var inside = close - start - 1;

					for (var k = start + 1; k < close; k++)
					{
						if (unescaped[k] is UserMark or PasswordMark)
						{
							if (inside != 1)
								return NotWholeValue(out error);

							contexts[k] = quote;
						}
					}

					next = unescaped.IndexOf(';', close);
				}
				else
				{
					next = unescaped.IndexOf(';', start);

					var valueEnd = next < 0 ? unescaped.Length : next;
					var value    = unescaped.AsSpan(start, valueEnd - start).TrimEnd();

					for (var k = start; k < valueEnd; k++)
					{
						if (unescaped[k] is UserMark or PasswordMark)
						{
							if (value.Length != 1)
								return NotWholeValue(out error);

							contexts[k] = '\0';
						}
					}
				}

				if (next < 0)
					break;

				position = next + 1;
			}

			var result = new StringBuilder(unescaped.Length);

			for (var i = 0; i < unescaped.Length; i++)
			{
				var c = unescaped[i];

				if (c is not (UserMark or PasswordMark))
				{
					result.Append(c);
					continue;
				}

				if (contexts[i] == Unread)
					return NotWholeValue(out error);

				var (value, name) = c == UserMark ? (user, "user") : (password, "password");
				var written       = Write(value ?? string.Empty, name, contexts[i], odbc, out error);

				if (written == null)
					return null;

				result.Append(written);
			}

			return result.ToString();

			static string? NotWholeValue(out string? error)
			{
				error = "The connection string must hold {0} and {1} as the whole value of a key (Key={1}, Key=\"{1}\" or Key='{1}', with no space inside the quotes; ODBC: Key={1} or Key={{{1}}}): a placeholder inside a longer value or a key would let the user name or password add keys of its own.";
				return null;
			}
		}

		// The position of the quote that closes a quoted value starting at the position (a doubled closing quote stands for
		// itself), or -1 when it does not close.
		// A value that holds one of these is quoted.
		static readonly System.Buffers.SearchValues<char> _quotedCharacters = System.Buffers.SearchValues.Create(";'\"={}");

		static int QuotedEnd(string text, int start, char open, char close)
		{
			for (var i = start + 1; i < text.Length; i++)
			{
				if (text[i] != close)
					continue;

				if (i + 1 < text.Length && text[i + 1] == close)
				{
					i++;
					continue;
				}

				return i;
			}

			return -1;
		}

		static string? Write(string value, string name, char context, bool odbc, out string? error)
		{
			error = null;

			if (value.Length == 0)
				return value;

			switch (context)
			{
				case '{':
					return value.Replace("}", "}}", StringComparison.Ordinal);

				case '"' or '\'':
					if (value.Contains(context, StringComparison.Ordinal))
					{
						error = $"The {name} holds the quote ({context}) that encloses it in the connection string, so it would end the value there. Use a template without those quotes: the CLI quotes values itself.";
						return null;
					}

					return value;
			}

			var needsQuotes = value.AsSpan().IndexOfAny(_quotedCharacters) >= 0 || value.Trim().Length != value.Length;

			if (!needsQuotes)
				return value;

			if (odbc)
				return "{" + value.Replace("}", "}}", StringComparison.Ordinal) + "}";

			if (!value.Contains('"', StringComparison.Ordinal))
				return "\"" + value + "\"";

			if (!value.Contains('\'', StringComparison.Ordinal))
				return "'" + value + "'";

			error = $"The {name} holds both kinds of quote (' and \"), so it cannot be put into the connection string as one value that every driver reads alike.";
			return null;
		}
	}
}
