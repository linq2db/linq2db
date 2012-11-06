using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	public interface IMetadataReader
	{
		T[] GetAttributes<T>(Type type)             where T : Attribute;
		T[] GetAttributes<T>(MemberInfo memberInfo) where T : Attribute;
	}
}
