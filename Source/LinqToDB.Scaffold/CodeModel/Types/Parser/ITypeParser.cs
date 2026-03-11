using System;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Provides helpers to create <see cref="IType"/> type descriptor from <see cref="Type"/> or string with type name.
	/// Also provides helpers to parse namespace names.
	/// </summary>
	public interface ITypeParser
	{
		/// <summary>
		/// Creates <see cref="IType"/> type descriptor from <see cref="Type"/> instance.
		/// </summary>
		/// <param name="type">Type to parse.</param>
		/// <returns><see cref="IType"/> type descriptor.</returns>
		IType Parse(Type type);

		/// <summary>
		/// Creates <see cref="IType"/> type descriptor from <typeparamref name="T"/> type.
		/// </summary>
		/// <typeparam name="T">Type to parse.</typeparam>
		/// <returns><see cref="IType"/> type descriptor.</returns>
		IType Parse<T>();

		/// <summary>
		/// Parse type from type name.
		/// Type name should be a full type name in following format:
		/// <list type="bullet">
		/// <item>namespaces should be separated by dot: <c>ns1.ns2.type</c></item>
		/// <item>nested types should be separated by plus: <c>ns1.ns2.type+nestest_type1+nested_type2</c></item>
		/// <item>genric type arguments should be enclosed in &lt;&gt; and separated by comma: <c>ns.type&lt;ns.type1, ns.type2&lt;ns.type1&gt;, ns.type3&gt;</c></item>
		/// <item>open generic types allowed: <c>ns.type&lt;,,&gt;</c></item>
		/// <item>nullability (including reference types) should be indicated by ?: <c>ns.type&lt;T1?&gt;?</c></item>
		/// <item>type aliases, arrays and dynamic types not supported</item>
		/// <item>generic type arguments should be real types</item>
		/// </list>
		/// </summary>
		/// <param name="typeName">String with type name.</param>
		/// <param name="valueType">Indicate that parsed type is value type or reference type. Applied only to main type. Nested types (array elements and generic type arguments parsed as value types).</param>
		/// <returns>Parsed type descriptor.</returns>
		IType Parse(string typeName, bool valueType);

		/// <summary>
		/// Parse (multi-part) namespace or type name (with namespace) into collection of identifiers for each namespace/type name element.
		/// </summary>
		/// <param name="name">String with namespace or type name.
		/// Type name shouldn't contain anything except namespace and namespace (no array brackets, generic argument list).
		/// Name components should be separated by dot (.).</param>
		/// <param name="generated">When <see langword="true" />, <paramref name="name"/> contains generated namespace/type name and could be modified on name conflict.</param>
		/// <returns>Namespace/type name elements.</returns>
		CodeIdentifier[] ParseNamespaceOrRegularTypeName(string name, bool generated);
	}
}
