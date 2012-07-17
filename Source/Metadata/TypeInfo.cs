using System;
using System.Collections.Generic;
using System.Linq;

namespace LinqToDB.Metadata
{
	class TypeInfo
	{
		public TypeInfo(string name, Dictionary<string,MemberInfo> members, params AttributeInfo[] attributes)
		{
			Name       = name;
			Members    = members;
			Attributes = attributes;
		}

		public string                        Name;
		public Dictionary<string,MemberInfo> Members;
		public AttributeInfo[]               Attributes;

		public AttributeInfo[] GetAttribute (Type type)
		{
			return
				Attributes.Where(a => a.Name == type.FullName).Concat(
				Attributes.Where(a => a.Name == type.Name).    Concat(
					type.Name.EndsWith("Attribute") ?
						Attributes.Where(a => a.Name == type.Name.Substring(0, type.Name.Length - "Attribute".Length)) :
						Enumerable.Empty<AttributeInfo>())
				).ToArray();
		}
	}
}
