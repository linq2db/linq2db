using System;
using System.Text.RegularExpressions;

using LinqToDB.CommandLine;
using LinqToDB.CommandLine.Commands.Connection;

namespace LinqToDB.CommandLine.Commands.SchemaInspection
{
	/// <summary>
	/// Resolves schema inspection settings from trusted connection settings and schema-specific inputs.
	/// </summary>
	internal sealed class SchemaInspectionSettingsResolver(ICliEnvironment environment)
	{
		public const string FullDetailLevel  = "full";
		public const string NamesDetailLevel = "names";

		const string DefaultOutput = "json";

		readonly ICliEnvironment _environment = environment;

		public int ErrorStatusCode { get; private set; } = StatusCodes.INVALID_ARGUMENTS;

		public SchemaInspectionSettings? Resolve(ConnectionSettings connection, SchemaInspectionOptionValues values)
		{
			ErrorStatusCode = StatusCodes.INVALID_ARGUMENTS;

			var output = values.Output ?? DefaultOutput;

			if (!string.Equals(output, DefaultOutput, StringComparison.OrdinalIgnoreCase))
			{
				_environment.Error.WriteLine($"Option '--{SchemaInspectionCliOptions.Output.Name}' has unknown value '{output}'. Schema output supports only 'json'.");
				return null;
			}

			if (!ValidateRegexFilters(values.FilterTables))
				return null;

			var detailLevel = values.DetailLevel ?? FullDetailLevel;

			if (!string.Equals(detailLevel, FullDetailLevel, StringComparison.OrdinalIgnoreCase)
				&& !string.Equals(detailLevel, NamesDetailLevel, StringComparison.OrdinalIgnoreCase))
			{
				_environment.Error.WriteLine($"Option '--{SchemaInspectionCliOptions.DetailLevel.Name}' has unknown value '{detailLevel}'. Allowed values: '{FullDetailLevel}', '{NamesDetailLevel}'.");
				return null;
			}

			detailLevel = detailLevel.ToLowerInvariant();

			var options = new SchemaInspectionEffectiveOptions(
				detailLevel,
				values.PreferProviderSpecificTypes ?? false,
				values.GetTables                   ?? true,
				string.Equals(detailLevel, NamesDetailLevel, StringComparison.Ordinal) ? false : values.GetForeignKeys ?? true,
				false,
				values.GenerateChar1AsString       ?? false,
				values.IgnoreSystemHistoryTables   ?? false,
				values.DefaultSchema,
				values.FilterSchemas               ?? [],
				values.FilterCatalogs              ?? [],
				values.FilterTables                ?? []);

			var outputFile = values.OutputFile != null
				? new ConnectionSettingsResolver(_environment).ResolvePath(SchemaInspectionCliOptions.OutputFile, values.OutputFile)
				: null;

			if (string.Equals(outputFile, ConnectionSettingsResolver.MissingEnvironmentVariable, StringComparison.Ordinal))
				return null;

			return new SchemaInspectionSettings(connection, options, DefaultOutput, outputFile, values.Overwrite);
		}

		bool ValidateRegexFilters(string[]? filters)
		{
			if (filters == null)
				return true;

			foreach (var filter in filters)
			{
				var pattern = GetRegexPattern(filter);

				if (pattern == null)
					continue;

				try
				{
					_ = new Regex(pattern, RegexOptions.CultureInvariant, SchemaFilterConstants.RegexTimeout);
				}
				catch (ArgumentException ex)
				{
					_environment.Error.WriteLine($"Option '--{SchemaInspectionCliOptions.FilterTable.Name}' contains invalid regex '{pattern}': {ex.Message}");
					return false;
				}
			}

			return true;
		}

		static string? GetRegexPattern(string filter)
		{
			if (filter.StartsWith("regex:", StringComparison.OrdinalIgnoreCase))
				return filter.Substring("regex:".Length);

			if (filter.StartsWith("rx:", StringComparison.OrdinalIgnoreCase))
				return filter.Substring("rx:".Length);

			return null;
		}
	}
}
