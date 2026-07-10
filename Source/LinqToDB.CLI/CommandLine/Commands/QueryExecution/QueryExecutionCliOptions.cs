using System;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.QueryExecution
{
	/// <summary>
	/// Shared query execution CLI option descriptors used by command adapters.
	/// </summary>
	internal static class QueryExecutionCliOptions
	{
		public static readonly OptionCategory ConfigurationOptions = new(1, "Configuration", "Configuration options", "configuration");
		public static readonly OptionCategory ConnectionOptions    = new(2, "Connection",    "Connection options",    "connection");
		public static readonly OptionCategory OutputOptions        = new(3, "Output",        "Output options",        "output");

		public static readonly CliOption Config              = new StringCliOption("config",                null, false, false, "path to query configuration file; supports %NAME% and ${NAME} environment variable expansion");
		public static readonly CliOption Profile             = new StringCliOption("profile",               null, false, false, "configuration profile name");
		public static readonly CliOption Provider            = new StringCliOption("provider",              null, false, false, "linq2db provider name");
		public static readonly CliOption ProviderLocation    = new StringCliOption("provider-location",     'l',  false, false, "path to external database provider assembly; dependencies must be available next to it or through normal application probing; supports %NAME% and ${NAME} environment variable expansion");
		public static readonly CliOption ConnectionString    = new StringCliOption("connection-string",     null, false, false, "database connection string; use {0} for user and {1} for password placeholders; configure provider-specific connection timeout here");
		public static readonly CliOption ConnectionStringEnv = new StringCliOption("connection-string-env", null, false, false, "environment variable with database connection string");
		public static readonly CliOption User                = new StringCliOption("user",                  null, false, false, "database user name for connection string formatting; supports %NAME% and ${NAME} environment variable expansion");
		public static readonly CliOption UserEnv             = new StringCliOption("user-env",              null, false, false, "environment variable with database user name");
		public static readonly CliOption Password            = new StringCliOption("password",              null, false, false, "database password for connection string formatting");
		public static readonly CliOption PasswordEnv         = new StringCliOption("password-env",          null, false, false, "environment variable with database password");
		public static readonly CliOption CommandTimeout      = new StringCliOption("command-timeout",       null, false, false, "SQL command timeout in seconds; 0 disables the option");
		public static readonly CliOption LockTimeout         = new StringCliOption("lock-timeout",          null, false, false, "provider-specific lock wait timeout in seconds; 0 disables the option");
		public static readonly CliOption MaxRows             = new StringCliOption("max-rows",              null, false, false, "maximum number of result rows to read; 0 disables the limit");
		public static readonly CliOption OutputFile          = new StringCliOption("output-file",           null, false, false, "path to file for command output; supports %NAME% and ${NAME} environment variable expansion");
		public static readonly CliOption Sql                 = new StringCliOption("sql",                   null, false, false, "single user-provided SQL query text to execute");
		public static readonly CliOption SqlFile             = new StringCliOption("sql-file",              null, false, false, "path to file with single user-provided SQL query text to execute; supports %NAME% and ${NAME} environment variable expansion");

		public static readonly CliOption Overwrite      = new BooleanCliOption("overwrite", null, false, "replace existing output file", null, null, null, false, false);
		public static readonly CliOption Impersonate    = new BooleanCliOption("impersonate", null, false, "on Windows, run database access under resolved user/password credentials; file and configuration access uses the original process account", null, null, null, false, false);

		public static readonly CliOption Output = new StringEnumCliOption(
			"output",
			null,
			false,
			false,
			"output format",
			null,
			null,
			null,
			false,
			new StringEnumOption(true,  true,  "json",       "JSON output"),
			new StringEnumOption(false, false, "json-table", "JSON table output"),
			new StringEnumOption(false, false, "csv",        "CSV output"));

		public static readonly CliOption McpOutput = new StringEnumCliOption(
			"output",
			null,
			false,
			false,
			"output format",
			null,
			null,
			null,
			false,
			new StringEnumOption(false, false, "json",       "JSON output"),
			new StringEnumOption(true,  true,  "json-table", "JSON table output"));

		public static readonly CliOption ImpersonateMode = new StringEnumCliOption(
			"impersonate-mode",
			null,
			false,
			false,
			"Windows impersonation logon mode",
			null,
			null,
			null,
			false,
			new StringEnumOption(true,  true,  "network-cleartext", "network cleartext logon; default"),
			new StringEnumOption(false, false, "interactive",       "interactive logon"),
			new StringEnumOption(false, false, "network",           "network logon"),
			new StringEnumOption(false, false, "new-credentials",   "new credentials logon"),
			new StringEnumOption(false, false, "2",                 "system code for interactive logon"),
			new StringEnumOption(false, false, "3",                 "system code for network logon"),
			new StringEnumOption(false, false, "8",                 "system code for network cleartext logon"),
			new StringEnumOption(false, false, "9",                 "system code for new credentials logon"));
	}
}
