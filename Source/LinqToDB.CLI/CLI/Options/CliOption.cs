namespace LinqToDB.CLI
{
	/// <summary>
	/// Base type for CLI command option.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Type">Option data type.</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="AllowMultiple">When <c>true</c>, user can specify multiple values (separated by comma).</param>
	/// <param name="AllowInJson">When <c>true</c>, option could be provided in JSON file.</param>
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
		string     Help,
		string?    DetailedHelp,
		string[]?  Examples,
		string[]?  JsonExamples);
}
