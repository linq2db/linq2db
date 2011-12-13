using System;
using System.Collections;

namespace LinqToDB.Common
{
	public class CompoundValue : IComparable, IEquatable<CompoundValue>
	{
		public CompoundValue(params object[] values)
		{
			if (values == null)
				throw new ArgumentNullException("values");

			// Note that the compound hash is precalculated.
			// This means that CompoundValue can be used only with immutable values.
			// Otherwise the behaviour is undefined.
			//
			_hash   = CalcHashCode(values);
			_values = values;
		}

		private readonly object[] _values;
		private readonly int      _hash;

		public int Count
		{
			get { return _values == null ? 0 : _values.Length; }
		}

		public object this[int index]
		{
			get { return _values == null ? null : _values[index]; }
		}

		private static int CalcHashCode(object[] values)
		{
			if (values.Length == 0)
				return 0;

			object o = values[0];
			int hash = o == null ? 0 : o.GetHashCode();

			for (int i = 1; i < values.Length; i++)
			{
				o = values[i];
				hash = ((hash << 5) + hash) ^ (o == null ? 0 : o.GetHashCode());
			}

			return hash;
		}

		#region IComparable Members

		public int CompareTo(object obj)
		{
			var objValues = ((CompoundValue)obj)._values;

			if (_values.Length != objValues.Length)
				return _values.Length - objValues.Length;

			for (var i = 0; i < _values.Length; i++)
			{
				var n = Comparer.Default.Compare(_values[i], objValues[i]);

				if (n != 0)
					return n;
			}

			return 0;
		}

		#endregion

		#region Object Overrides

		public override int GetHashCode()
		{
			return _hash;
		}

		public override bool Equals(object obj)
		{
			if (!(obj is CompoundValue))
				return false;

			return Equals((CompoundValue)obj);
		}

		#endregion

		#region IEquatable<CompoundValue> Members

		public bool Equals(CompoundValue other)
		{
			if (_hash != other._hash)
				return false;

			object[] values = other._values;

			if (_values.Length != values.Length)
				return false;

			for (int i = 0; i < _values.Length; i++)
			{
				object x = _values[i];
				object y =  values[i];

				if (x == null && y == null)
					continue;

				if (x == null || y == null)
					return false;

				if (x.Equals(y) == false)
					return false;
			}

			return true;
		}

		#endregion
	}
}
