using System;
using System.Collections;
using System.Collections.Generic;

namespace LinqToDB.Mapping
{
	public class DictionaryMapper : MapDataSourceDestinationBase
	{
		public DictionaryMapper(IDictionary dictionary)
		{
			if (dictionary == null) throw new ArgumentNullException("dictionary");

			_dictionary = dictionary;
		}

		private readonly IDictionary _dictionary;
		public           IDictionary  Dictionary
		{
			get { return _dictionary; }
		}

		private int                   _currentIndex;
		private IDictionaryEnumerator _enumerator;

		private void SetEnumerator(int i)
		{
			if (_enumerator == null)
			{
				_enumerator = _dictionary.GetEnumerator();
				_enumerator.MoveNext();
			}

			if (_currentIndex > i)
			{
				_currentIndex = 0;
				_enumerator.Reset();
				_enumerator.MoveNext();
			}

			for (; _currentIndex < i; _currentIndex++)
				_enumerator.MoveNext();
		}

		#region IMapDataSource Members

		public override int Count
		{
			get { return _dictionary.Count; }
		}

		public override Type GetFieldType(int index)
		{
			SetEnumerator(index);
			return _enumerator.Value == null? typeof(object): _enumerator.Value.GetType();
		}

		public override string GetName(int index)
		{
			SetEnumerator(index);
			return _enumerator.Key.ToString();
		}

		public override bool SupportsTypedValues(int index)
		{
			return index < _dictionary.Count;
		}

		public override object GetValue(object o, int index)
		{
			SetEnumerator(index);
			return _enumerator.Value;
		}

		public override object GetValue(object o, string name)
		{
			return _dictionary.Contains(name) ? _dictionary[name] : null;
		}

		#endregion

		#region IMapDataDestination Members

		private List<string> _nameList;

		public override int GetOrdinal(string name)
		{
			if (_nameList == null)
				_nameList = new List<string>();

			var idx = _nameList.IndexOf(name);

			if (idx >= 0)
				return idx;

			_nameList.Add(name);

			return _nameList.Count - 1;
		}

		public override void SetValue(object o, int index, object value)
		{
			SetValue(o, _nameList[index], value);
		}

		public override void SetValue(object o, string name, object value)
		{
			if (_dictionary.Contains(name))
				_dictionary[name] = value;
			else
				_dictionary.Add(name, value);
		}

		#endregion
	}
}
