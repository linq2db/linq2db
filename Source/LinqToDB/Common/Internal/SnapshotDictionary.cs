using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace LinqToDB.Common.Internal
{
	public class SnapshotDictionary<TKey, TValue> : IDictionary<TKey, TValue>
		where TKey : notnull
	{
		readonly Dictionary<TKey, TValue> _dictionary;
		Stack<HashSet<TKey>>?             _snapshotStack;

		public SnapshotDictionary()
		{
			_dictionary = new Dictionary<TKey, TValue>();
		}

		public SnapshotDictionary(IEqualityComparer<TKey> comparer)
		{
			_dictionary = new Dictionary<TKey, TValue>(comparer);
		}

		public SnapshotDictionary(Dictionary<TKey, TValue> dictionary)
		{
			_dictionary = dictionary;
		}

		public IEqualityComparer<TKey> Comparer => _dictionary.Comparer;

		/// <summary>
		/// Takes a snapshot of the current state.
		/// </summary>
		public void TakeSnapshot()
		{
			_snapshotStack ??= new Stack<HashSet<TKey>>();
			_snapshotStack.Push(new HashSet<TKey>(_dictionary.Comparer));
		}

		/// <summary>
		/// Rolls back to the last snapshot.
		/// </summary>
		public void Rollback()
		{
			if (_snapshotStack == null || _snapshotStack.Count == 0)
				throw new InvalidOperationException("No active snapshot to rollback.");

			var addedKeys = _snapshotStack.Pop();
			foreach (var key in addedKeys)
			{
				_dictionary.Remove(key);
			}
		}

		public void Commit()
		{
			if (_snapshotStack == null || _snapshotStack.Count == 0)
				throw new InvalidOperationException("No active snapshot to commit.");

			_snapshotStack.Pop();
		}

		// IDictionary<TKey, TValue> Implementation

		/// <summary>
		/// Adds an element with the provided key and value to the dictionary.
		/// </summary>
		public void Add(TKey key, TValue value)
		{
			_dictionary.Add(key, value);

			if (_snapshotStack != null && _snapshotStack.Count > 0)
			{
				_snapshotStack.Peek().Add(key);
			}
		}

		/// <summary>
		/// Determines whether the dictionary contains an element with the specified key.
		/// </summary>
		public bool ContainsKey(TKey key)
		{
			return _dictionary.ContainsKey(key);
		}

		/// <summary>
		/// Removes the element with the specified key from the dictionary.
		/// </summary>
		public bool Remove(TKey key)
		{
			// Since only Add is supported after snapshot, removing keys is not supported.
			throw new NotSupportedException("Remove operation is not supported.");
		}

		/// <summary>
		/// Gets the value associated with the specified key.
		/// </summary>
#pragma warning disable CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
		public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
#pragma warning restore CS8767 // Nullability of reference types in type of parameter doesn't match implicitly implemented member (possibly because of nullability attributes).
		{
			return _dictionary.TryGetValue(key, out value);
		}

		/// <summary>
		/// Gets or sets the element with the specified key.
		/// </summary>
		public TValue this[TKey key]
		{
			get => _dictionary[key];
			set
			{
				// Since only Add is supported, updating a value is not supported.
				if (_dictionary.ContainsKey(key))
				{
					throw new NotSupportedException("Update operation is not supported.");
				}
				else
				{
					Add(key, value);
				}
			}
		}

		/// <summary>
		/// Gets a collection containing the keys in the dictionary.
		/// </summary>
		public ICollection<TKey> Keys => _dictionary.Keys;

		/// <summary>
		/// Gets a collection containing the values in the dictionary.
		/// </summary>
		public ICollection<TValue> Values => _dictionary.Values;

		/// <summary>
		/// Gets the number of key/value pairs contained in the dictionary.
		/// </summary>
		public int Count => _dictionary.Count;

		/// <summary>
		/// Gets a value indicating whether the dictionary is read-only.
		/// </summary>
		public bool IsReadOnly => false;

		/// <summary>
		/// Adds an item to the dictionary.
		/// </summary>
		void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)
		{
			Add(item.Key, item.Value);
		}

		/// <summary>
		/// Removes all items from the dictionary.
		/// </summary>
		public void Clear()
		{
			if (_snapshotStack != null && _snapshotStack.Count > 0)
				throw new NotSupportedException("Clear operation is not supported during an active snapshot.");

			_dictionary.Clear();
		}

		/// <summary>
		/// Determines whether the dictionary contains a specific key/value pair.
		/// </summary>
		public bool Contains(KeyValuePair<TKey, TValue> item)
		{
			return _dictionary.ContainsKey(item.Key) && EqualityComparer<TValue>.Default.Equals(_dictionary[item.Key], item.Value);
		}

		/// <summary>
		/// Copies the elements of the dictionary to an array, starting at a particular array index.
		/// </summary>
		public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
		{
			((ICollection<KeyValuePair<TKey, TValue>>)_dictionary).CopyTo(array, arrayIndex);
		}

		/// <summary>
		/// Removes the key/value pair from the dictionary.
		/// </summary>
		bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)
		{
			// Remove is not supported
			throw new NotSupportedException("Remove operation is not supported.");
		}

		/// <summary>
		/// Returns an enumerator that iterates through the dictionary.
		/// </summary>
		public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator()
		{
			return _dictionary.GetEnumerator();
		}

		/// <summary>
		/// Returns an enumerator that iterates through the dictionary.
		/// </summary>
		IEnumerator IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}
	}

}
