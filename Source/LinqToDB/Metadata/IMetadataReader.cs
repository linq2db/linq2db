using System;
using System.Reflection;

using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	public interface IMetadataReader
	{
		/// <summary>
		/// Gets all mapping attributes on specified type, resolved against the active <paramref name="mappingSchema"/>.
		/// </summary>
		/// <param name="mappingSchema">Active combined mapping schema the attributes are resolved for. A reader that
		/// answers a schema-relative question ("is element type X scalar in this schema?", "what <see cref="DataType"/>
		/// maps to CLR type Y?") should consult this schema instead of the static <see cref="MappingSchema.Default"/>.</param>
		/// <param name="type">Attributes owner type.</param>
		/// <returns>Array of mapping attributes.</returns>
		/// <remarks>
		/// Output must be a pure function of <c>(mappingSchema, type)</c>. Under that purity <see cref="GetObjectID"/>
		/// stays schema-independent (different combined schemas already produce different configuration IDs through
		/// their other layers). A reader whose output depends on the schema must not internally memoize keyed by
		/// <c>type</c> alone (that leaks answers across schemas), and must never ask the passed schema for attributes
		/// of the same <c>type</c> it is currently answering (self-recursion).
		/// </remarks>
		MappingAttribute[] GetAttributes(MappingSchema mappingSchema, Type type);
		/// <summary>
		/// Gets all mapping attributes on specified type member, resolved against the active <paramref name="mappingSchema"/>.
		/// </summary>
		/// <param name="mappingSchema">Active combined mapping schema the attributes are resolved for. A reader that
		/// answers a schema-relative question should consult this schema instead of the static <see cref="MappingSchema.Default"/>.</param>
		/// <param name="type">Member type. Could be used by some metadata providers to identify actual member owner type.</param>
		/// <param name="memberInfo">Type member for which mapping attributes should be returned.</param>
		/// <returns>Array of attributes.</returns>
		/// <remarks>
		/// Output must be a pure function of <c>(mappingSchema, type, memberInfo)</c>; see the type overload for the
		/// full purity / caching / self-recursion contract.
		/// </remarks>
		MappingAttribute[] GetAttributes(MappingSchema mappingSchema, Type type, MemberInfo memberInfo);

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
