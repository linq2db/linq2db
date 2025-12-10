#if !NET8_0_OR_GREATER

#pragma warning disable IDE0130
#pragma warning disable IDE0160
namespace System.Collections.Generic;

internal static class DictionaryExtensions
{
	extension<TKey, TValue>(Dictionary<TKey, TValue> dictionary)
	{
		/// <summary>
		///	    False proxy for `Dictionary.TryAdd()` available in net6+. This matches existing behavior and so does not
		///     reduce performance further.
		/// </summary>
		/// <param name="key">
		///	    The key of the element to add.
		/// </param>
		/// <param name="value">
		///	    The value of the element to add. It can be <see langword="null" />.
		/// </param>
		/// <returns>
		///	    <see langword="true" /> if the key/value pair was added to the dictionary successfully; otherwise, <see
		///     langword="false" />.
		/// </returns>
		public bool TryAdd(TKey key, TValue value)
		{
			if (dictionary.ContainsKey(key))
				return false;

			dictionary[key] = value;
			return true;
		}
	}
}

#endif
