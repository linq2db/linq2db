using System;
using System.Reflection;

namespace LinqToDB.Metadata
{
	using Mapping;

	public interface IMetadataReader
	{
		/// <summary>
		/// Gets all mapping attributes on specified type.
		/// </summary>
		/// <param name="type">Attributes owner type.</param>
		/// <returns>Array of mapping attributes.</returns>
		MappingAttribute[] GetAttributes(Type type);
		/// <summary>
		/// Gets all mapping attributes on specified type member.
		/// </summary>
		/// <param name="type">Member type. Could be used by some metadata providers to identify actual member owner type.</param>
		/// <param name="memberInfo">Type member for which mapping attributes should be returned.</param>
		/// <returns>Array of attributes.</returns>
		MappingAttribute[] GetAttributes(Type type, MemberInfo memberInfo);

		/// <summary>
		/// Gets the dynamic columns defined on given type.
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns>All dynamic columns defined on given type.</returns>
		MemberInfo[] GetDynamicColumns(Type type);

		/// <summary>
		/// Should return a unique ID for cache purposes. If the implemented Metadata reader returns instance-specific
		/// data you'll need to calculate a unique value based on content. Otherwise just use a static const
		/// e.g. $".{nameof(YourMetadataReader)}."
		/// </summary>
		/// <returns>The object ID as string</returns>
		string GetObjectID();
	}
}
