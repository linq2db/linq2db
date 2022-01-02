namespace LinqToDB.CLI
{
	/// <summary>
	/// Boolean CLI option descriptor.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="Default">Default option value when used didn't specified it explicitly.</param>
	internal sealed record BooleanCliOption(
		string    Name,
		char?     ShortName,
		bool      Required,
		string    Help,
		string?   DetailedHelp,
		string[]? Examples,
		string[]? JsonExamples,
		bool      Default)
		: CliOption(
			Name,
			ShortName,
			OptionType.Boolean,
			Required,
			false,
			true,
			Help,
			DetailedHelp,
			Examples,
			JsonExamples);
}
