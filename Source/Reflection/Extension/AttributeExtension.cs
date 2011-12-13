using System;

namespace LinqToDB.Reflection.Extension
{
	public class AttributeExtension
	{
		public AttributeExtension()
		{
			Values = new ValueCollection();
		}

		private AttributeExtension(ValueCollection values)
		{
			Values = values;
		}

		public string          Name   { get; set; }
		public ValueCollection Values { get; private set; }

		public object Value
		{
			get { return this == _null? null: Values.Value; }
		}

		public object this[string valueName]
		{
			get { return this == _null? null: Values[valueName]; }
		}

		public object this[string valueName, object defaultValue]
		{
			get { return this[valueName] ?? defaultValue; }
		}

		private AttributeNameCollection _attributes;
		public  AttributeNameCollection  Attributes
		{
			get { return _attributes ?? (_attributes = new AttributeNameCollection()); }
		}

		private static readonly AttributeExtension _null = new AttributeExtension(ValueCollection.Null);
		public  static          AttributeExtension  Null
		{
			get { return _null;  }
		}
	}
}
