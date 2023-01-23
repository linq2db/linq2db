using System;
using System.Reflection;
using System.Collections.Generic;

namespace LinqToDB.Metadata
{
	using Mapping;

	public interface IMetadataReader
	{
		/// <summary>
		/// Gets all mapping attributes on specified type.
		/// </summary>
		/// <typeparam name="T">Specify attribute base type, which should be implemented by returned attributes.</typeparam>
		/// <param name="type">Attributes owner type.</param>
		/// <returns>Array of attributes, derived from <typeparamref name="T"/> type.</returns>
		/// <remarks>
		/// Type parameter <typeparamref name="T"/> could specify <see cref="MappingAttribute"/> (base type for all attributes)
		/// and metadata provider should return all supported mapping attributes for such requests.
		/// </remarks>
		T[] GetAttributes<T>(Type type                       ) where T : MappingAttribute;
		/// <summary>
		/// Gets all mapping attributes on specified type member.
		/// </summary>
		/// <typeparam name="T">Specify attribute base type, which should be implemented by returned attributes.</typeparam>
		/// <param name="type">Member type. Could be used by some metadata providers to identify actual member owner type.</param>
		/// <param name="memberInfo">Type member for which mapping attributes should be returned.</param>
		/// <returns>Array of attributes, derived from <typeparamref name="T"/> type.</returns>
		/// <remarks>
		/// Type parameter <typeparamref name="T"/> could specify <see cref="MappingAttribute"/> (base type for all attributes)
		/// and metadata provider should return all supported mapping attributes for such requests.
		/// </remarks>
		T[] GetAttributes<T>(Type type, MemberInfo memberInfo) where T : MappingAttribute;

		/// <summary>
		/// Gets the dynamic columns defined on given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>All dynamic columns defined on given type.</returns>
		MemberInfo[] GetDynamicColumns(Type type);

		string GetObjectID();
	}
}
