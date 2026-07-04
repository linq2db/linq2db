using System;
using System.Reflection;

using LinqToDB.Mapping;

namespace LinqToDB.Metadata
{
	/// <summary>
	/// Optional, additive extension of <see cref="IMetadataReader"/> that lets a reader resolve mapping attributes
	/// with access to the active (combined) <see cref="MappingSchema"/> the attributes are being resolved for.
	/// A reader that must answer a schema-relative question - "is element type X scalar in this schema?", "what
	/// <see cref="DataType"/> maps to CLR type Y?" - should implement this interface so it consults the live schema
	/// stack (provider + user layers) instead of the static <see cref="MappingSchema.Default"/>. When a reader does
	/// not implement it, resolution falls back to the schema-less <see cref="IMetadataReader"/> overloads.
	/// </summary>
	/// <remarks>
	/// Implementation contract (in addition to the <see cref="IMetadataReader.GetObjectID"/> guidance):
	/// <list type="bullet">
	/// <item>Output must be a pure function of <c>(mappingSchema, type, memberInfo)</c> - no hidden mutable state.</item>
	/// <item>Under that purity <see cref="IMetadataReader.GetObjectID"/> stays schema-independent (a constant for a
	/// stateless reader): an identical schema configuration ID implies an identical layer stack and therefore identical
	/// reader output, and different combined schemas already produce different configuration IDs through their other
	/// layers.</item>
	/// <item>Do not memoize internally keyed by <c>type</c> alone - that leaks answers across schemas. A per-type cache
	/// is safe only for a reader that stays schema-blind.</item>
	/// <item>A schema-aware reader must never ask the passed schema for attributes of the same <c>(type, memberInfo)</c>
	/// it is currently answering (self-recursion). Querying the schema about <em>other</em> types
	/// (<see cref="MappingSchema.IsScalarType"/>, <see cref="MappingSchema.GetDataType(Type)"/>, ...) is the safe pattern.</item>
	/// </list>
	/// </remarks>
	public interface ISchemaAwareMetadataReader : IMetadataReader
	{
		/// <summary>
		/// Gets all mapping attributes on specified type, resolved against the active <paramref name="mappingSchema"/>.
		/// </summary>
		/// <param name="mappingSchema">Active combined mapping schema the attributes are resolved for.</param>
		/// <param name="type">Attributes owner type.</param>
		/// <returns>Array of mapping attributes.</returns>
		MappingAttribute[] GetAttributes(MappingSchema mappingSchema, Type type);

		/// <summary>
		/// Gets all mapping attributes on specified type member, resolved against the active <paramref name="mappingSchema"/>.
		/// </summary>
		/// <param name="mappingSchema">Active combined mapping schema the attributes are resolved for.</param>
		/// <param name="type">Member type. Could be used by some metadata providers to identify actual member owner type.</param>
		/// <param name="memberInfo">Type member for which mapping attributes should be returned.</param>
		/// <returns>Array of attributes.</returns>
		MappingAttribute[] GetAttributes(MappingSchema mappingSchema, Type type, MemberInfo memberInfo);
	}
}
