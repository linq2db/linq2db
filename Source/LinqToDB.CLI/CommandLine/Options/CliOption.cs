using System.Text.Json;

namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Base type for CLI command option.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Type">Option data type.</param>
	/// <param name="Required">When <see langword="true"/>, used requred to specify this option.</param>
	/// <param name="AllowMultiple">When <see langword="true"/>, user can specify multiple values (separated by comma).</param>
	/// <param name="AllowInJson">When <see langword="true"/>, option could be provided in JSON file.</param>
	/// <param name="AllowInCli">When <see langword="true"/>, option could be provided through command-line interface.</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	internal abstract record CliOption(
		string     Name,
		char?      ShortName,
		OptionType Type,
		bool       Required,
		bool       AllowMultiple,
		bool       AllowInJson,
		bool       AllowInCli,
		string     Help,
		string?    DetailedHelp,
		string[]?  Examples,
		string[]?  JsonExamples)
	{
		/// <summary>
		/// Parse option value(s) using CLI arguments.
		/// </summary>
		/// <param name="command">Option's command descriptor.</param>
		/// <param name="rawValue">Option's value argument.</param>
		/// <param name="errorDetails">Optional error details on failue (when method returns <see langword="null"/>).</param>
		/// <returns>
		/// Returns parsed value or <see langword="null"/> on error.
		/// </returns>
		public abstract object? ParseCommandLine(CliCommand command, string rawValue, out string? errorDetails);

		/// <summary>
		/// Parse option value(s) using value from JSON.
		/// </summary>
		/// <param name="rawValue">Option's property value in JSON.</param>
		/// <param name="errorDetails">Optional error details on failue (when method returns <see langword="null"/>).</param>
		/// <returns>
		/// Returns parsed value or <see langword="null"/> on error.
		/// </returns>
		public abstract object? ParseJSON(JsonElement rawValue, out string? errorDetails);
	}
}
