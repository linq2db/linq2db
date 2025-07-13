#if NETFRAMEWORK || NETSTANDARD2_0
#pragma warning disable MA0048 // File name must match type name

namespace System
{
	internal static class StringExtensions
	{
		public static bool Contains(this string str, string value, StringComparison comparisonType) => str.IndexOf(value, comparisonType) != -1;
	}
}

namespace System.Collections.Concurrent
{
	internal static class ConcurrentDictionaryExtensions
	{
		public static TValue GetOrAdd<TKey, TValue, TArg>(this ConcurrentDictionary<TKey, TValue> dictionary, TKey key, Func<TKey, TArg, TValue> valueFactory, TArg factoryArgument)
		{
			return dictionary.GetOrAdd(key, key => valueFactory(key, factoryArgument));
		}
	}
}

namespace System.Text
{
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

		public static StringBuilder AppendLine(
			this StringBuilder sb,
			IFormatProvider? provider,
			FormattableString formattableString)
		{
			sb.AppendLine(formattableString.ToString(provider));
			return sb;
		}
	}
}
#endif
