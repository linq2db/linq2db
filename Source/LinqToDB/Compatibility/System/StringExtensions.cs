using System.Collections.Generic;

#pragma warning disable IDE0130
#pragma warning disable IDE0160

namespace System;

internal static class StringExtensions
{
	extension(string str)
	{
#if !NET8_0_OR_GREATER
		/// <summary>
		///	    False proxy for `string.AsSpan()` available in net6+. Returns the instance itself in net462 and
		///     netstandard2.0. This matches existing behavior and so does not reduce performance further.
		/// </summary>
		/// <returns>
		///	    The string passed in.
		///	</returns>
		public string AsSpan() => str;

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

		/// <summary>
		///		Creates a new string by using the specified provider to control the formatting of the specified interpolated string.
		/// </summary>
		/// <param name="provider">
		///		An object that supplies culture-specific formatting information.
		/// </param>
		/// <param name="formattableString">
		///		The interpolated string.
		/// </param>
		/// <returns>
		///		The string that results for formatting the interpolated string using the specified format provider.
		/// </returns>
		public static string Create(IFormatProvider? provider, FormattableString formattableString) =>
			formattableString.ToString(provider);
#endif

		public static string JoinStrings(char separator, IEnumerable<string> values)
		{
#pragma warning disable RS0030 // Do not use banned APIs
#if NET8_0_OR_GREATER
			return string.Join(separator, values);
#else
			return string.Join(separator.ToString(), values);
#endif
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}
}

