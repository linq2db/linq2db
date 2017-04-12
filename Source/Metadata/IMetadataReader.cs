using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	public interface IMetadataReader
	{
		T[] GetAttributes<T>(Type type,                        bool inherit = true) where T : Attribute;
		T[] GetAttributes<T>(Type type, MemberInfo memberInfo, bool inherit = true) where T : Attribute;
	}
}
