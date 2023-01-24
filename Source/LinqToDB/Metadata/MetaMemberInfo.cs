using System;

namespace LinqToDB.Metadata
{
	sealed class MetaMemberInfo
	{
		public MetaMemberInfo(string name, params AttributeInfo[] attributes)
		{
			Name       = name;
			Attributes = attributes;
		}

		public string          Name;
		public AttributeInfo[] Attributes;

		public AttributeInfo[] GetAttribute(Type type)
		{
			return Attributes.Where(a => type.IsAssignableFrom(a.Type)).ToArray();
		}
	}
}
