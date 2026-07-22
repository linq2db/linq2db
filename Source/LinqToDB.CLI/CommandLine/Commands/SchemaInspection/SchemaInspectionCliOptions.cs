using System;

using LinqToDB.CommandLine.Options;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Schema inspection CLI option descriptors.
	/// </summary>
	internal static class SchemaInspectionCliOptions
	{
		public static readonly OptionCategory SchemaOptions = new(5, "Schema", "Schema metadata options", "schema");
		public static readonly OptionCategory OutputOptions = new(6, "Output", "Output options", "output");

		public static readonly CliOption PreferProviderSpecificTypes = new BooleanCliOption("prefer-provider-specific-types", null, false, "prefer provider-specific .NET types in schema metadata", null, null, null, false, false);
		public static readonly CliOption GetTables                   = new BooleanCliOption("get-tables",                     null, false, "read table and view metadata", null, null, null, true, true);
		public static readonly CliOption GetForeignKeys              = new BooleanCliOption("get-foreign-keys",               null, false, "read foreign key metadata", null, null, null, true, true);
		public static readonly CliOption GenerateChar1AsString       = new BooleanCliOption("generate-char1-as-string",       null, false, "map char(1) metadata to string instead of char", null, null, null, false, false);
		public static readonly CliOption IgnoreSystemHistoryTables   = new BooleanCliOption("ignore-system-history-tables",   null, false, "ignore SQL Server temporal history tables when provider supports it", null, null, null, false, false);
		public static readonly CliOption DefaultSchema               = new StringCliOption ("default-schema",                 null, false, false, "default schema name");
		public static readonly CliOption FilterSchema                = new StringCliOption ("filter-schema",                  null, false, true,  "schema name filter; can be repeated or comma-separated");
		public static readonly CliOption FilterCatalog               = new StringCliOption ("filter-catalog",                 null, false, true,  "catalog name filter; can be repeated or comma-separated");
		public static readonly CliOption FilterTable                 = new StringCliOption ("filter-table",                   null, false, true,  "table or view name filter; can be repeated or comma-separated; matches name, schema.name, or catalog.schema.name; use regex: or rx: prefix for regular expressions");
		public static readonly CliOption OutputFile                  = new StringCliOption ("output-file",                    null, false, false, "path to file for schema JSON output; supports %NAME% and ${NAME} environment variable expansion");
		public static readonly CliOption Overwrite                   = new BooleanCliOption("overwrite",                      null, false, "replace existing output file", null, null, null, false, false);

		public static readonly CliOption Output = new StringEnumCliOption(
			"output",
			null,
			false,
			false,
			"output format; schema supports only JSON",
			null,
			null,
			null,
			false,
			new StringEnumOption(true, true, "json", "JSON output"));
	}
}
