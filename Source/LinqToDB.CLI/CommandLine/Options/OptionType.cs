namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Base data type for CLI option.
	/// </summary>
	internal enum OptionType
	{
		/// <summary>
		/// Arbitrary string.
		/// </summary>
		String,
		/// <summary>
		/// Predefined string.
		/// </summary>
		StringEnum,
		/// <summary>
		/// String-typed key-value pairs (keys are unique).
		/// </summary>
		StringDictionary,
		/// <summary>
		/// Boolean value (true/false).
		/// </summary>
		Boolean,
		/// <summary>
		/// Database object name filter.
		/// Value is a list of objects of two types (both types could be used in same list):
		/// <list type="bullet">
		/// <item>string: maps to database object name with exact match including match by case</item>
		/// <item><c>{ name: string, schema?: string }</c>: maps to database object with specified name (exact match) and optionally by schema (also exact match required)</item>
		/// <item><c>{ regex: string /*.net regular expression*/, schema?: string }</c>: maps to database objects with name matched by specified regular expression and optionally by schema (also exact match required)</item>
		/// </list>
		/// </summary>
		DatabaseObjectFilter,
		/// <summary>
		/// Language identifier naming options.
		/// Value is an object with following properties:
		/// <list type="bullet">
		/// <item>"transformation": "split_by_underscore" (default) | "t4" - custom transformation, applied to original name.</item>
		/// <item>"case": "none" (default) | "pascal_case" | "camel_case" | "snake_case" | "lower_case" | "upper_case" | "t4_pluralized" | "t4"</item>
		/// <item>"pluralization": "none" (default) | "singular" | "plural" | "plural_multiple_characters"</item>
		/// <item>"prefix": string?</item>
		/// <item>"suffix": string?</item>
		/// <item>"pluralize_if_ends_with_word_only": bool (default: false)</item>
		/// <item>"ignore_all_caps": bool (default: false)</item>
		/// </list>
		/// </summary>
		Naming,
		/// <summary>
		/// Option with path to JSON file with additional CLI options.
		/// If option specified in both CLI interface and JSON file, CLI version will be used.
		/// </summary>
		JSONImport,
	}
}
