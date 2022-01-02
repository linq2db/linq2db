namespace LinqToDB.CLI
{
	/// <summary>
	/// Defines single allowed value for <see cref="StringEnumCliOption"/> CLI option.
	/// </summary>
	/// <param name="Default">When <c>true</c>, indicates that value is set by default when used doesn't provide option values explicitly.</param>
	/// <param name="Value">Option value.</param>
	/// <param name="Help">Option descripton/help text.</param>
	public sealed record StringEnumOption(bool Default, string Value, string Help);
}
