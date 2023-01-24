using System;

namespace LinqToDB.Metadata
{
	sealed class MetaTypeInfo
	{
		public MetaTypeInfo(string name, Dictionary<string,MetaMemberInfo> members, params AttributeInfo[] attributes)
		{
			Name       = name;
			Members    = members;
			Attributes = attributes;
		}

		public string                            Name;
		public Dictionary<string,MetaMemberInfo> Members;
		public AttributeInfo[]                   Attributes;

		public AttributeInfo[] GetAttribute(Type type)
		{
			return Attributes.Where(a => type.IsAssignableFrom(a.Type)).ToArray();
		}
	}
}
