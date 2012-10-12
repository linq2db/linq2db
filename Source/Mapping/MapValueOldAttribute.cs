using System;

namespace LinqToDB.Mapping
{
	[AttributeUsage(
		AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Enum |
		AttributeTargets.Class | AttributeTargets.Interface,
		AllowMultiple=true)]
	public class MapValueOldAttribute : Attribute
	{
		public MapValueOldAttribute(object value1)
		{
			SetValues(null, null, value1);
		}

		public MapValueOldAttribute(object[] values)
		{
			SetValues(null, null, values);
		}

		public MapValueOldAttribute(object origValue, object[] values)
		{
			SetValues(null, origValue, values);
		}

		public MapValueOldAttribute(object origValue, object value1)
		{
			SetValues(null, origValue, value1);
		}

		public MapValueOldAttribute(object origValue, object value1, object value2)
		{
			SetValues(null, origValue, value1, value2);
		}

		public MapValueOldAttribute(object origValue, object value1, object value2, object value3)
		{
			SetValues(null, origValue, value1, value2, value3);
		}

		public MapValueOldAttribute(object origValue, object value1, object value2, object value3, object value4)
		{
			SetValues(null, origValue, value1, value2, value3, value4);
		}
		
		public MapValueOldAttribute(object origValue, object value1, object value2, object value3, object value4, object value5)
		{
			SetValues(null, origValue, value1, value2, value3, value4, value5);
		}

		public MapValueOldAttribute(Type type, object origValue, object[] values)
		{
			SetValues(type, origValue, values);
		}

		public MapValueOldAttribute(Type type, object origValue, object value1)
		{
			SetValues(type, origValue, value1);
		}

		public MapValueOldAttribute(Type type, object origValue, object value1, object value2)
		{
			SetValues(type, origValue, value1, value2);
		}

		public MapValueOldAttribute(Type type, object origValue, object value1, object value2, object value3)
		{
			SetValues(type, origValue, value1, value2, value3);
		}

		public MapValueOldAttribute(Type type, object origValue, object value1, object value2, object value3, object value4)
		{
			SetValues(type, origValue, value1, value2, value3, value4);
		}
		
		public MapValueOldAttribute(Type type, object origValue, object value1, object value2, object value3, object value4, object value5)
		{
			SetValues(type, origValue, value1, value2, value3, value4, value5);
		}

		protected void SetValues(Type type, object origValue, params object[] values)
		{
			_type      = type;
			_origValue = origValue;
			_values    = values;
		}

		private Type   _type;
		public  object  Type
		{
			get { return _type; }
		}

		private object _origValue;
		public  object  OrigValue
		{
			get { return _origValue; }
		}

		private object[] _values;
		public  object[]  Values
		{
			get { return _values; }
		}
	}
}
