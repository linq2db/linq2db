﻿namespace LinqToDB.CommandLine
{
	/// <summary>
	/// Defines single allowed value for <see cref="StringEnumCliOption"/> CLI option.
	/// </summary>
	/// <param name="Default">When <c>true</c>, indicates that value is set by default when user doesn't provide option values explicitly in default mode.</param>
	/// <param name="T4Default">When <c>true</c>, indicates that value is set by default when user doesn't provide option values explicitly in T4-compat mode.</param>
	/// <param name="Value">Option value.</param>
	/// <param name="Help">Option descripton/help text.</param>
	public sealed record StringEnumOption(bool Default, bool T4Default, string Value, string Help);
}
