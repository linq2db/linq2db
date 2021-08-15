using System;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Type and type member attributes and modifiers.
	/// </summary>
	[Flags]
	public enum Modifiers
	{
		/// <summary>
		/// No explicit attributes on type or type member.
		/// </summary>
		None = 0,
		/// <summary>
		/// Partial type or type member (e.g. method).
		/// </summary>
		Partial = 1,
		/// <summary>
		/// Static class or type member.
		/// </summary>
		Static = 2,
		/// <summary>
		/// Public type or type member.
		/// </summary>
		Public = 4,
		/// <summary>
		/// Private type member (including nested types).
		/// </summary>
		Private = 8,
		/// <summary>
		/// Extension method.
		/// </summary>
		Extension = 16,
		/// <summary>
		/// Read-only field.
		/// </summary>
		ReadOnly = 32,
	}
}
