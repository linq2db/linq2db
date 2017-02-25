using System;
using System.Collections.Generic;

namespace System.Collections.Concurrent
{
	internal class ConcurrentDictionary<TKey,TValue> : IDictionary<TKey, TValue>
	{
		private IDictionary<TKey, TValue> _storage;

		public ConcurrentDictionary()
		{
			_storage = new Dictionary<TKey, TValue>();
		}

		public ConcurrentDictionary(IEqualityComparer<TKey> comparer)
		{
			_storage = new Dictionary<TKey, TValue>(comparer);
		}

		public TValue GetOrAdd(TKey key, TValue value)
		{
			if ((object)key == null)
				throw new ArgumentNullException("key");

			lock (_storage)
			{
				TValue val;
				if (TryGetValue(key, out val))
					return val;

				Add(key, value);
				return value;
			}
		}

		public TValue GetOrAdd(TKey key, Func<TKey,TValue> valueFactory)
		{
			if ((object)key == null)
				throw new ArgumentNullException("key");

			if (valueFactory == null)
				throw new ArgumentNullException("valueFactory");

			TValue resultingValue;

			lock (_storage)
			{
				if (TryGetValue(key, out resultingValue))
					return resultingValue;

				this[key] = resultingValue = valueFactory(key);
			}

			return resultingValue;
		}

		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _storage.GetEnumerator();
		}

		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		public void Add(KeyValuePair<TKey, TValue> item)
		{
			lock (_storage)
				_storage.Add(item.Key, item.Value);
		}

		public void Clear()
		{
			lock (_storage)
				_storage.Clear();
		}

		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _storage.Contains(item);
		}

		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			lock (_storage)
				_storage.CopyTo(array, arrayIndex);
		}

		public bool Remove(KeyValuePair<TKey, TValue> item)
		{
			lock (_storage)
				return _storage.Remove(item);
		}

		public int Count { get { return _storage.Count; } }
		public bool IsReadOnly { get { return _storage.IsReadOnly; } }
		public bool ContainsKey(TKey key)
		{
			return _storage.ContainsKey(key);
		}

		public void Add(TKey key, TValue value)
		{
			lock (_storage)
				_storage.Add(key, value);
		}

		public bool Remove(TKey key)
		{
			lock (_storage)
				return _storage.Remove(key);
		}

		public bool TryGetValue(TKey key, out TValue value)
		{
			return _storage.TryGetValue(key, out value);
		}

		public TValue this[TKey key]
		{
			get { return _storage[key]; }
			set { lock (_storage) _storage[key] = value; }
		}

		public ICollection<TKey> Keys { get { return _storage.Keys; } }
		public ICollection<TValue> Values { get { return _storage.Values; } }
	}
}
