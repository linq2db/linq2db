#pragma warning disable MA0047 // Declare types in namespaces
#pragma warning disable MA0048 // File name must match type name

using System.Collections.Generic;
using System.Text;

#if NETFRAMEWORK || NETSTANDARD2_0

using System;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

internal static class StringExtensions
{
	extension(string str)
	{
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

		public static string JoinStrings(char separator, IEnumerable<string> values)
		{
#pragma warning disable RS0030 // Do not use banned APIs
			return string.Join(separator.ToString(), values);
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}
}

internal static class StringBuilderExtensions
{
	public static StringBuilder Append(
		this StringBuilder sb,
		IFormatProvider? provider,
		FormattableString formattableString)
	{
		sb.Append(formattableString.ToString(provider));
		return sb;
	}

	public static StringBuilder AppendBuilder(this StringBuilder sb, StringBuilder? stringBuilder)
	{
#pragma warning disable MA0028 // Optimize StringBuilder usage
		if (stringBuilder?.Length > 0)
			sb.Append(stringBuilder.ToString());
#pragma warning restore MA0028 // Optimize StringBuilder usage
		return sb;
	}

	public static StringBuilder InsertBuilder(this StringBuilder sb, int index, StringBuilder? stringBuilder)
	{
#pragma warning disable MA0028 // Optimize StringBuilder usage
		if (stringBuilder?.Length > 0)
			sb.Insert(index, stringBuilder.ToString());
#pragma warning restore MA0028 // Optimize StringBuilder usage
		return sb;
	}

	public static StringBuilder AppendLine(
		this StringBuilder sb,
		IFormatProvider? provider,
		FormattableString formattableString)
	{
		sb.AppendLine(formattableString.ToString(provider));
		return sb;
	}

	public static StringBuilder AppendJoinStrings(this StringBuilder sb, string? separator, IEnumerable<string> values)
	{
		return sb.Append(string.Join(separator, values));
	}
}

internal static class CharExtensions
{
	public static bool IsAsciiDigit(this char chr) => chr is (>= '0' and <= '9');

	public static bool IsAsciiLetter(this char chr) => chr is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

	public static bool IsAsciiLetterOrDigit(this char chr) => IsAsciiLetter(chr) || IsAsciiDigit(chr);
}

internal static class DictionaryExtensions
{
	public static bool TryAdd<TKey, TValue>(this Dictionary<TKey, TValue> dict, TKey key, TValue value)
	{
		if (!dict.ContainsKey(key))
		{
			dict.Add(key, value);
			return true;
		}

		return false;
	}
}

internal static class AdoAsyncDispose
{
	public static ValueTask DisposeAsync(this DbCommand command) => TryDisposeAsync(command);
	public static ValueTask DisposeAsync(this DbDataReader dataReader) => TryDisposeAsync(dataReader);
	public static ValueTask DisposeAsync(this DbConnection connection) => TryDisposeAsync(connection);
	public static ValueTask DisposeAsync(this DbTransaction transaction) => TryDisposeAsync(transaction);

	static ValueTask TryDisposeAsync(IDisposable disposable)
	{
		if (disposable is IAsyncDisposable asyncDisposable)
			return asyncDisposable.DisposeAsync();

		disposable.Dispose();
		return default;
	}
}

#else

internal static class CharExtensions
{
	public static bool IsAsciiDigit(this char chr) => char.IsAsciiDigit(chr);
	public static bool IsAsciiLetter(this char chr) => char.IsAsciiLetter(chr);
	public static bool IsAsciiLetterOrDigit(this char chr) => char.IsAsciiLetterOrDigit(chr);
}

internal static class StringExtensions
{
	extension(string str)
	{
		public static string JoinStrings(char separator, IEnumerable<string> values)
		{
#pragma warning disable RS0030 // Do not use banned APIs
			return string.Join(separator, values);
#pragma warning restore RS0030 // Do not use banned APIs
		}
	}
}

internal static class StringBuilderExtensions
{
	public static StringBuilder AppendBuilder(this StringBuilder sb, StringBuilder? stringBuilder) => sb.Append(stringBuilder);

	public static StringBuilder AppendJoinStrings(this StringBuilder sb, string? separator, IEnumerable<string> values)
	{
#pragma warning disable RS0030 // Do not use banned APIs
		return sb.AppendJoin(separator, values);
#pragma warning restore RS0030 // Do not use banned APIs
	}

	public static StringBuilder InsertBuilder(this StringBuilder sb, int index, StringBuilder? stringBuilder)
	{
#pragma warning disable MA0028 // Optimize StringBuilder usage
		if (stringBuilder?.Length > 0)
			sb.Insert(index, stringBuilder.ToString());
#pragma warning restore MA0028 // Optimize StringBuilder usage
		return sb;
	}
}

#endif
