using System;

namespace LinqToDB.Mapping
{
	/// <summary>
	/// Flags for specifying skip modifications used for Attributes based on <see cref="SkipBaseAttribute"/>.
	/// </summary>
	[Flags]
	public enum SkipModification
	{
		/// <summary>
		/// No value should be skipped.
		/// </summary>
		None = 0x0,
		/// <summary>
		/// A value should be skipped on insert.
		/// </summary>
		Insert = 0x1,
		/// <summary>
		/// A value should be skipped on update.
		/// </summary>
		Update = 0x2
	}
}
