namespace LinqToDB.CLI
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
		/// Boolean value (true/false).
		/// </summary>
		Boolean
	}
}
