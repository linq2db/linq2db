using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.Connection;
using LinqToDB.CommandLine.Commands.QueryExecution;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.ConfigInit
{
	/// <summary>
	/// Query/MCP configuration initialization command.
	/// </summary>
	sealed class ConfigInitCommand : CliCommand
	{
		const string DefaultProfileName = "default";
		const string DefaultConfigPath   = ".agents/linq2db-query.json";
		const int    DefaultMaxRows      = 1000;
		const string DefaultOutput       = "json-table";

		static readonly OptionCategory _profileOptions = new(2, "Profile", "Profile settings", "profile");
		static readonly OptionCategory _existsOptions  = new(5, "Existing profile", "Existing profile behavior", "existingProfile");

		static readonly CliOption _description = new StringCliOption(
			"description",
			null,
			false,
			false,
			"non-secret profile description returned by MCP linq2db_info");

		static readonly CliOption _output = new StringEnumCliOption(
			"output",
			null,
			false,
			false,
			"output format to write to the initialized profile",
			"Configuration profiles are shared by query and MCP. The query command supports json, json-table, and csv. The MCP linq2db_query tool supports only json and json-table; when a profile uses csv, MCP calls must override output to json-table or json.",
			null,
			null,
			false,
			new StringEnumOption(false, false, "json",       "JSON output"),
			new StringEnumOption(true,  true,  "json-table", "JSON table output"),
			new StringEnumOption(false, false, "csv",        "CSV output"));

		static readonly CliOption _ifExists = new StringEnumCliOption(
			"if-exists",
			null,
			false,
			false,
			"behavior when selected profile already exists",
			null,
			null,
			null,
			false,
			new StringEnumOption(true,  true,  "error",   "fail when selected profile exists"),
			new StringEnumOption(false, false, "replace", "replace selected profile"),
			new StringEnumOption(false, false, "skip",    "leave selected profile unchanged and succeed"));

		static readonly JsonSerializerOptions _jsonOptions = new()
		{
			WriteIndented = true,
		};

		public static CliCommand Instance { get; } = new ConfigInitCommand();

		ConfigInitCommand()
			: base(
				"config-init",
				true,
				false,
				"<options>",
				"create or update a query/MCP configuration profile; generated profiles intentionally include maxRows, output, and enableExecute for manual editing",
				[
					new("dotnet linq2db config-init --provider SQLite --connection-string \"Data Source=data.db\"",
						"creates .agents/linq2db-query.json with the default profile"),
					new("dotnet linq2db config-init --config query.json --profile dev --description \"Development database\" --provider SqlServer --connection-string-env LINQ2DB_DEV_CONNECTION",
						"adds a dev profile that reads its connection string from an environment variable"),
					new("dotnet linq2db config-init --config query.json --profile dev --provider PostgreSQL --connection-string-env LINQ2DB_DEV_CONNECTION --if-exists replace",
						"replaces an existing dev profile"),
					new("dotnet linq2db config-init --config query.json --profile export --provider SQLite --connection-string \"Data Source=data.db\" --output csv",
						"creates a shared query/MCP profile with CSV as the query command default; MCP calls must override output to json-table or json"),
				])
		{
			AddOption(QueryExecutionCliOptions.ConfigurationOptions, QueryExecutionCliOptions.Config);
			AddOption(QueryExecutionCliOptions.ConfigurationOptions, QueryExecutionCliOptions.Profile);
			AddOption(_profileOptions,                           _description);
			AddOption(QueryExecutionCliOptions.ConnectionOptions, QueryExecutionCliOptions.Provider);
			AddOption(QueryExecutionCliOptions.ConnectionOptions, QueryExecutionCliOptions.ProviderLocation);
			AddOption(QueryExecutionCliOptions.ConnectionOptions, QueryExecutionCliOptions.ConnectionString);
			AddOption(QueryExecutionCliOptions.ConnectionOptions, QueryExecutionCliOptions.ConnectionStringEnv);
			AddOption(QueryExecutionCliOptions.OutputOptions,     _output);
			AddOption(QueryExecutionCliOptions.OutputOptions,     QueryExecutionCliOptions.MaxRows);
			AddOption(_existsOptions,                            _ifExists);
		}

		public override async ValueTask<int> Execute(
			CliController                  controller,
			ICliEnvironment                environment,
			string[]                       rawArgs,
			Dictionary<CliOption, object?> options,
			IReadOnlyCollection<string>    unknownArgs,
			CancellationToken              cancellationToken)
		{
			options.Remove(QueryExecutionCliOptions.Config,              out var config);
			options.Remove(QueryExecutionCliOptions.Profile,             out var profile);
			options.Remove(_description,                                out var description);
			options.Remove(QueryExecutionCliOptions.Provider,            out var provider);
			options.Remove(QueryExecutionCliOptions.ProviderLocation,    out var providerLocation);
			options.Remove(QueryExecutionCliOptions.ConnectionString,    out var connectionString);
			options.Remove(QueryExecutionCliOptions.ConnectionStringEnv, out var connectionStringEnv);
			options.Remove(_output,                                      out var output);
			options.Remove(QueryExecutionCliOptions.MaxRows,             out var maxRows);
			options.Remove(_ifExists,                                   out var ifExists);

			if (options.Count > 0)
			{
				foreach (var kvp in options)
					await environment.Error.WriteLineAsync($"{Name} command missing '{kvp.Key.Name}' option handler").ConfigureAwait(false);

				throw new InvalidOperationException($"Not all options handled by {Name} command");
			}

			var configPath = new ConnectionSettingsResolver(environment).ResolvePath(QueryExecutionCliOptions.Config, (string?)config) ?? DefaultConfigPath;

			// A missing %NAME%/${NAME} expansion already reported its own diagnostic and
			// returned a sentinel value that can't be used as a real path.
			if (configPath.Contains('\0', StringComparison.Ordinal))
				return StatusCodes.INVALID_ARGUMENTS;

			var values = new ConfigInitValues(
				configPath,
				(string?)profile ?? DefaultProfileName,
				(string?)description,
				(string?)provider,
				(string?)providerLocation,
				(string?)connectionString,
				(string?)connectionStringEnv,
				(string?)output ?? DefaultOutput,
				(string?)maxRows,
				(string?)ifExists ?? "error");

			if (!Validate(environment, values, out var maxRowsValue))
				return StatusCodes.INVALID_ARGUMENTS;

			JsonObject root;
			var exists = environment.FileExists(values.ConfigPath);

			if (exists)
			{
				if (!TryLoadRoot(environment, values.ConfigPath, out root))
					return StatusCodes.EXPECTED_ERROR;
			}
			else
			{
				root = new JsonObject();
			}

			var profileExists = root.ContainsKey(values.Profile);

			if (profileExists)
			{
				if (string.Equals(values.IfExists, "skip", StringComparison.Ordinal))
				{
					await environment.Out.WriteLineAsync($"Configuration profile '{values.Profile}' already exists in '{values.ConfigPath}'. Skipped.").ConfigureAwait(false);
					return StatusCodes.SUCCESS;
				}

				if (string.Equals(values.IfExists, "error", StringComparison.Ordinal))
				{
					await environment.Error.WriteLineAsync($"Configuration profile '{values.Profile}' already exists in '{values.ConfigPath}'. Use '--if-exists replace' to overwrite it or '--if-exists skip' to leave it unchanged.").ConfigureAwait(false);
					return StatusCodes.EXPECTED_ERROR;
				}
			}

			if (!root.ContainsKey(DefaultProfileName))
				root[DefaultProfileName] = CreateDefaultProfile();
			else if (!string.Equals(values.Profile, DefaultProfileName, StringComparison.Ordinal) && root[DefaultProfileName] is not JsonObject)
			{
				await environment.Error.WriteLineAsync($"Configuration file '{values.ConfigPath}' profile '{DefaultProfileName}' must be object.").ConfigureAwait(false);
				return StatusCodes.EXPECTED_ERROR;
			}

			root[values.Profile] = CreateProfile(values, maxRowsValue);

			var directory = Path.GetDirectoryName(values.ConfigPath);
			if (!string.IsNullOrEmpty(directory))
				environment.CreateDirectory(directory);

			environment.WriteAllText(values.ConfigPath, root.ToJsonString(_jsonOptions) + Environment.NewLine);

			var action = profileExists && string.Equals(values.IfExists, "replace", StringComparison.Ordinal)
				? "Updated"
				: "Created";

			await environment.Out.WriteLineAsync($"{action} configuration profile '{values.Profile}' in '{values.ConfigPath}'.").ConfigureAwait(false);
			return StatusCodes.SUCCESS;
		}

		static bool Validate(ICliEnvironment environment, ConfigInitValues values, out int maxRows)
		{
			maxRows = DefaultMaxRows;

			if (string.IsNullOrWhiteSpace(values.Profile))
			{
				environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Profile.Name}' cannot be empty.");
				return false;
			}

			if (string.Equals(values.Profile, "mcp", StringComparison.Ordinal))
			{
				environment.Error.WriteLine("Configuration profile name 'mcp' is reserved for MCP server configuration.");
				return false;
			}

			if (string.IsNullOrWhiteSpace(values.Provider))
			{
				environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.Provider.Name}' must be specified.");
				return false;
			}

			var hasConnectionString    = !string.IsNullOrEmpty(values.ConnectionString);
			var hasConnectionStringEnv = !string.IsNullOrEmpty(values.ConnectionStringEnv);

			if (hasConnectionString == hasConnectionStringEnv)
			{
				environment.Error.WriteLine($"Exactly one of '--{QueryExecutionCliOptions.ConnectionString.Name}' or '--{QueryExecutionCliOptions.ConnectionStringEnv.Name}' must be specified.");
				return false;
			}

			if (values.MaxRows != null
				&& (!int.TryParse(values.MaxRows, NumberStyles.None, CultureInfo.InvariantCulture, out maxRows) || maxRows < 0))
			{
				environment.Error.WriteLine($"Option '--{QueryExecutionCliOptions.MaxRows.Name}' must be a non-negative integer row count.");
				return false;
			}

			return true;
		}

		static bool TryLoadRoot(ICliEnvironment environment, string configPath, out JsonObject root)
		{
			root = null!;

			try
			{
				var documentOptions = new JsonDocumentOptions
				{
					CommentHandling     = JsonCommentHandling.Skip,
					AllowTrailingCommas = true,
				};
				var node = JsonNode.Parse(environment.ReadAllText(configPath), null, documentOptions);

				if (node is JsonObject jsonObject)
				{
					root = jsonObject;
					return true;
				}

				environment.Error.WriteLine($"Configuration file '{configPath}' must contain JSON object as root element.");
				return false;
			}
			catch (JsonException ex)
			{
				environment.Error.WriteLine($"Configuration file '{configPath}' is not valid JSON: {ex.Message}");
				return false;
			}
		}

		static JsonObject CreateDefaultProfile()
		{
			return new JsonObject
			{
				["maxRows"]       = DefaultMaxRows,
				["output"]        = DefaultOutput,
				["enableExecute"] = false,
			};
		}

		static JsonObject CreateProfile(ConfigInitValues values, int maxRows)
		{
			var profile = new JsonObject();

			if (values.Description != null)
				profile["description"] = values.Description;

			profile["provider"] = values.Provider;

			if (values.ProviderLocation != null)
				profile["providerLocation"] = values.ProviderLocation;

			if (values.ConnectionString != null)
				profile["connectionString"] = values.ConnectionString;
			else
				profile["connectionStringEnv"] = values.ConnectionStringEnv;

			profile["maxRows"]       = maxRows;
			profile["output"]        = values.Output;
			profile["enableExecute"] = false;

			return profile;
		}

		sealed record ConfigInitValues(
			string  ConfigPath,
			string  Profile,
			string? Description,
			string? Provider,
			string? ProviderLocation,
			string? ConnectionString,
			string? ConnectionStringEnv,
			string  Output,
			string? MaxRows,
			string  IfExists);
	}
}
