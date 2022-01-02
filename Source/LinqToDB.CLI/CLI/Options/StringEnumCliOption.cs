namespace LinqToDB.CLI
{
	/// <summary>
	/// String-typed CLI option with fixed list of allowed values.
	/// </summary>
	/// <param name="Name">Option name (used with -- prefix).</param>
	/// <param name="ShortName">Optional short name (used with - prefix).</param>
	/// <param name="Required">When <c>true</c>, used requred to specify this option.</param>
	/// <param name="AllowMultiple">When <c>true</c>, user can specify multiple values (separated by comma).</param>
	/// <param name="Help">Short help/description test for option.</param>
	/// <param name="DetailedHelp">Optional detailed help for option.</param>
	/// <param name="Examples">Optional list of option use examples.</param>
	/// <param name="JsonExamples">Optional list of option use examples in JSON.</param>
	/// <param name="Values">List of allowed values (with defaults).</param>
	internal sealed record StringEnumCliOption(
		string             Name,
		char?              ShortName,
		bool               Required,
		bool               AllowMultiple,
		string             Help,
		string?            DetailedHelp,
		string[]?          Examples,
		string[]?          JsonExamples,
		StringEnumOption[] Values)
		: CliOption(
			Name,
			ShortName,
			OptionType.StringEnum,
			Required,
			AllowMultiple,
			true,
			Help,
			DetailedHelp,
			Examples,
			JsonExamples);
}
