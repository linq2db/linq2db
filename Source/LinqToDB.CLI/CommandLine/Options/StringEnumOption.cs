namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Defines single allowed value for <see cref="StringEnumCliOption"/> CLI option.
	/// </summary>
	/// <param name="Default">When <see langword="true"/>, indicates that value is set by default when user doesn't provide option values explicitly in default mode.</param>
	/// <param name="T4Default">When <see langword="true"/>, indicates that value is set by default when user doesn't provide option values explicitly in T4-compat mode.</param>
	/// <param name="Value">Option value.</param>
	/// <param name="Help">Option descripton/help text.</param>
	internal sealed record StringEnumOption(bool Default, bool T4Default, string Value, string Help);
}
