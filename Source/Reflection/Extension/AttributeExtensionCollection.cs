using System;
using System.Collections.Generic;

namespace LinqToDB.Reflection.Extension
{
	public class AttributeExtensionCollection : List<AttributeExtension>
	{
		public new AttributeExtension this[int index]
		{
			get
			{
				return this == _null || index < 0 || index >= Count ? AttributeExtension.Null : base[index];
			}
		}

		public object Value
		{
			get { return this == _null? null: this[0].Value; }
		}

		public new void Add(AttributeExtension attributeExtension)
		{
			if (this != _null)
				base.Add(attributeExtension);
		}

		private static readonly AttributeExtensionCollection _null = new AttributeExtensionCollection();
		public  static          AttributeExtensionCollection  Null
		{
			get { return _null;  }
		}
	}
}
