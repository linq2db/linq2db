#pragma warning disable MA0047 // Declare types in namespaces
#pragma warning disable MA0048 // File name must match type name

using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

#if NETFRAMEWORK || NETSTANDARD2_0

using System;
using System.Data.Common;
using System.Reflection;
using System.Threading.Tasks;

internal static class StringBuilderExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder Append(
		this StringBuilder sb,
		IFormatProvider? provider,
		FormattableString formattableString)
	{
		sb.Append(formattableString.ToString(provider));
		return sb;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder AppendBuilder(this StringBuilder sb, StringBuilder? stringBuilder)
	{
		if (stringBuilder?.Length > 0)
			sb.Append(stringBuilder.ToString());
		return sb;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder AppendLine(
		this StringBuilder sb,
		IFormatProvider? provider,
		FormattableString formattableString)
	{
		sb.AppendLine(formattableString.ToString(provider));
		return sb;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder AppendJoinStrings(this StringBuilder sb, string? separator, IEnumerable<string> values)
	{
		return sb.Append(string.Join(separator, values));
	}
}

internal static class CharExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiDigit(this char chr) => chr is (>= '0' and <= '9');

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiLetter(this char chr) => chr is (>= 'a' and <= 'z') or (>= 'A' and <= 'Z');

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiLetterOrDigit(this char chr) => IsAsciiLetter(chr) || IsAsciiDigit(chr);
}

internal static class DictionaryExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask DisposeAsync(this DbCommand command) => TryDisposeAsync(command);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask DisposeAsync(this DbDataReader dataReader) => TryDisposeAsync(dataReader);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask DisposeAsync(this DbConnection connection) => TryDisposeAsync(connection);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static ValueTask DisposeAsync(this DbTransaction transaction) => TryDisposeAsync(transaction);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
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
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiDigit(this char chr) => char.IsAsciiDigit(chr);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiLetter(this char chr) => char.IsAsciiLetter(chr);
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static bool IsAsciiLetterOrDigit(this char chr) => char.IsAsciiLetterOrDigit(chr);
}

internal static class StringBuilderExtensions
{
	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder AppendBuilder(this StringBuilder sb, StringBuilder? stringBuilder) => sb.Append(stringBuilder);

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	public static StringBuilder AppendJoinStrings(this StringBuilder sb, string? separator, IEnumerable<string> values)
	{
#pragma warning disable RS0030 // Do not use banned APIs
		return sb.AppendJoin(separator, values);
#pragma warning restore RS0030 // Do not use banned APIs
	}
}

#endif
