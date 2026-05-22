using System;

#pragma warning disable MA0062 // Non-flags enums should not be marked with "FlagsAttribute"

namespace LinqToDB.CodeModel
{
	// following attributes not supported currently: extern, unsafe, volatile
	/// <summary>
	/// Explicit type and type member attributes and modifiers.
	/// We don't check if set flag match defaults for applied member or type and always generate modifier if it set by this enum.
	/// Also we don't check wether modifier is valid on member/type.
	/// </summary>
	[Flags]
	public enum Modifiers
	{
		/// <summary>
		/// No explicit attributes on type or type member.
		/// </summary>
		None      = 0,
		/// <summary>
		/// Public type or type member.
		/// </summary>
		Public    = 0x0001,
		/// <summary>
		/// Protected type member.
		/// Could be combined with <see cref="Private"/> to form <c>private protected</c> modifier.
		/// Could be combined with <see cref="Internal"/> to form <c>protected internal</c> modifier.
		/// </summary>
		Protected = 0x0002,
		/// <summary>
		/// Internal type or type member.
		/// Could be combined with <see cref="Protected"/> to form <c>protected internal</c> modifier.
		/// </summary>
		Internal  = 0x0004,
		/// <summary>
		/// Private type or type member.
		/// Could be combined with <see cref="Protected"/> to form <c>private protected</c> modifier.
		/// </summary>
		Private   = 0x0008,
		/// <summary>
		/// <see langword="new"/> overload attribute on type members.
		/// </summary>
		New       = 0x0010,
		/// <summary>
		/// Member override.
		/// </summary>
		Override  = 0x0020,
		/// <summary>
		/// Abstract class or member.
		/// </summary>
		Abstract  = 0x0040,
		/// <summary>
		/// Sealed class/member.
		/// </summary>
		Sealed    = 0x0080,
		/// <summary>
		/// Partial type or type member (e.g. method).
		/// </summary>
		Partial   = 0x0100,
		/// <summary>
		/// Extension method.
		/// </summary>
		Extension = 0x0200 | Static,
		/// <summary>
		/// Read-only field.
		/// </summary>
		ReadOnly  = 0x0400,
		/// <summary>
		/// Async method.
		/// </summary>
		Async     = 0x0800,
		/// <summary>
		/// <see langword="static"/> attribute on type/type members.
		/// </summary>
		Static    = 0x1000,
		/// <summary>
		/// <c><see langword="virtual"/></c> attribute on type members.
		/// </summary>
		Virtual   = 0x2000,
	}
}
