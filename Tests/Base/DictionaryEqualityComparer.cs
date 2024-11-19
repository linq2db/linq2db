using System.Collections.Generic;
using System.Linq;

namespace Tests
{
	public class DictionaryEqualityComparer<TKey, TValue> : IEqualityComparer<IDictionary<TKey, TValue>> 
		where TKey : notnull
	{
		readonly IEqualityComparer<TKey>   _keyComparer;
		readonly IEqualityComparer<TValue> _valueComparer;

		public DictionaryEqualityComparer() : this(null, null) { }

		public DictionaryEqualityComparer(IEqualityComparer<TKey>? keyComparer, IEqualityComparer<TValue>? valueComparer)
		{
			_keyComparer   = keyComparer   ?? EqualityComparer<TKey>.Default;
			_valueComparer = valueComparer ?? EqualityComparer<TValue>.Default;
		}

		public bool Equals(IDictionary<TKey, TValue>? x, IDictionary<TKey, TValue>? y)
		{
			if (x == null || y == null)
			{
				return x == null && y == null; //if either value is null return false, or true if both are null
			}

			return x.Count == y.Count //unless they have the same number of items, the dictionaries do not match
			       && x.Keys.Intersect(y.Keys, _keyComparer).Count() == x.Count //unless they have the same keys, the dictionaries do not match
			       && x.Keys.Count(key => ValueEquals(x[key], y[key])) == x.Count; //unless each keys' value is the same in both, the dictionaries do not match
		}

		public int GetHashCode(IDictionary<TKey, TValue> obj)
		{
			//I suspect there's a more efficient formula for even distribution, but this does the job for now
			long hashCode = obj.Count;
			foreach (var key in obj.Keys)
			{
				hashCode += key.GetHashCode() + (obj[key]?.GetHashCode() ?? 0);
				hashCode %= int.MaxValue; //ensure we don't go outside the bounds of MinValue-MaxValue
			}

			return (int)hashCode; //safe conversion thanks to the above %
		}

		bool ValueEquals(TValue x, TValue y)
		{
			return _valueComparer.Equals(x, y);
		}
	}
}
