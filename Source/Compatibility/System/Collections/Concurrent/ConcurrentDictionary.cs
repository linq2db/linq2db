using System;

namespace System.Collections.Concurrent
{
	internal class ConcurrentDictionary<TKey,TValue> : Generic.Dictionary<TKey,TValue>
	{
		public TValue GetOrAdd(TKey key, Func<TKey,TValue> valueFactory)
		{
			if ((object)key == null)
				throw new ArgumentNullException("key");

			if (valueFactory == null)
				throw new ArgumentNullException("valueFactory");

			TValue resultingValue;

			if (TryGetValue(key, out resultingValue))
				return resultingValue;

			this[key] = resultingValue = valueFactory(key);

			return resultingValue;
		}
	}
}
