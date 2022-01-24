namespace LinqToDB.Naming
{
	/// <summary>
	/// Define name casing formats.
	/// </summary>
	public enum NameCasing
	{
		/// <summary>
		/// No specific casing.
		/// </summary>
		None,
		/// <summary>
		/// PascalCase.
		/// </summary>
		Pascal,
		/// <summary>
		/// camelCase.
		/// </summary>
		CamelCase,
		/// <summary>
		/// snake_case.
		/// </summary>
		SnakeCase,
		/// <summary>
		/// lowercase.
		/// </summary>
		LowerCase,
		/// <summary>
		/// UPPERCASE.
		/// </summary>
		UpperCase,

		/// <summary>
		/// Pluralized casing T4 compatibility format.
		/// <list type="bullet">
		/// <item>first letter upper-cased</item>
		/// <item>non-first letters lower-cased</item>
		/// <item>one- and two-letter uppercase sequences treated as words</item>
		/// </list>
		/// </summary>
		T4CompatPluralized,
		/// <summary>
		/// Non-pluralized casing T4 compatibility format.
		/// <list type="bullet">
		/// <item>first letter upper-cased</item>
		/// <item>non-first letters lower-cased</item>
		/// <item>uppercase sequences treated as words in names with mixed casing</item>
		/// </list>
		/// </summary>
		T4CompatNonPluralized,
	}
}
