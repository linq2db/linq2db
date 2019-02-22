using System;
using System.Linq;

namespace LinqToDB.Metadata
{
	class MetaMemberInfo
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
			return
				Attributes.Where(a => a.Name == type.FullName).Concat(
				Attributes.Where(a => a.Name == type.Name)).   Concat(
					type.Name.EndsWith("Attribute") ?
						Attributes.Where(a => a.Name == type.Name.Substring(0, type.Name.Length - "Attribute".Length)) :
						Enumerable.Empty<AttributeInfo>()
				).ToArray();
		}
	}
}
