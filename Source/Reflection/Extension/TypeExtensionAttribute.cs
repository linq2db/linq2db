using System;

namespace LinqToDB.Reflection.Extension
{
	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Enum)]
	public class TypeExtensionAttribute : Attribute
	{
		public TypeExtensionAttribute()
		{
		}

		public TypeExtensionAttribute(string typeName) 
			: this(null, typeName)
		{
		}

		public TypeExtensionAttribute(string fileName, string typeName)
		{
			FileName = fileName;
			TypeName = typeName;
		}

		public string FileName { get; set; }
		public string TypeName { get; set; }
	}
}
