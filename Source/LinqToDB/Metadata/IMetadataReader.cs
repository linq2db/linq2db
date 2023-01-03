using System;
using System.Reflection;
using System.Collections.Generic;

namespace LinqToDB.Metadata
{
	using Mapping;

	public interface IMetadataReader
	{
		T[] GetAttributes<T>(Type type                       ) where T : MappingAttribute;
		T[] GetAttributes<T>(Type type, MemberInfo memberInfo) where T : MappingAttribute;

		/// <summary>
		/// Gets the dynamic columns defined on given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>All dynamic columns defined on given type.</returns>
		MemberInfo[] GetDynamicColumns(Type type);
	}
}
