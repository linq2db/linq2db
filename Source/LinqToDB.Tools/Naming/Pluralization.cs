namespace LinqToDB.Naming
{
	/// <summary>
	/// Defines pluralization-related word conversions.
	/// </summary>
	public enum Pluralization
	{
		/// <summary>
		/// No conversion applied.
		/// </summary>
		None,
		/// <summary>
		/// Convert word to singular form.
		/// </summary>
		Singular,
		/// <summary>
		/// Convert word to plural form.
		/// </summary>
		Plural,
		/// <summary>
		/// Convert word to plural form if it is longer than 1 character.
		/// </summary>
		PluralIfLongerThanOne,
	}
}
