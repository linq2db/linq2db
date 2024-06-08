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
	}
}
