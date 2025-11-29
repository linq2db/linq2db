#if !NET8_0_OR_GREATER

#pragma warning disable IDE0130
#pragma warning disable IDE0160
namespace System;

internal static class StringExtensions
{
	extension(string str)
	{
		/// <summary>
		///	    False proxy for `string.AsSpan()` available in net6+. Returns <see cref="string.Substring(int)"/> in
		///     net462 and netstandard2.0. This matches existing behavior and so does not reduce performance further.
		/// </summary>
		/// <param name="startIndex">
		///	    The zero-based starting character position of a substring in this instance.
		///	</param>
		/// <returns>
		///	    A string that is equivalent to the substring that begins at <paramref name="startIndex"/> in this
		///     instance, or <see cref="string.Empty"/> if <paramref name="startIndex"/> is equal to the length of this instance.
		///	</returns>
		public string AsSpan(int startIndex) =>
			str.Substring(startIndex);

		/// <summary>
		///	    False proxy for `string.AsSpan()` available in net6+. Returns <see cref="string.Substring(int)"/> in
		///     net462 and netstandard2.0. This matches existing behavior and so does not reduce performance further.
		/// </summary>
		/// <param name="startIndex">
		///	    The zero-based starting character position of a substring in this instance.
		///	</param>
		///	<param name="length">
		///	    The number of characters in the substring.
		///	</param>
		/// <returns>
		///	    A string that is equivalent to the substring of length <paramref name="length"/> that begins at
		///     <paramref name="startIndex"/> in this instance, or <see cref="string.Empty"/> if <paramref
		///     name="startIndex"/> is equal to the length of this instance and <paramref name="length"/> is zero.
		///	</returns>
		public string AsSpan(int startIndex, int length) =>
			str.Substring(startIndex, length);
	}
}

#endif
