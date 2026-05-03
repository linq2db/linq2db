using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Type descriptor interface.
	/// </summary>
	public interface IType
	{
		/// <summary>
		/// Type kind.
		/// </summary>
		TypeKind                       Kind                { get; }

		/// <summary>
		/// Type nullability. E.g. NRT annotation status for reference type and Nullable`T wrapper presence for value type.
		/// </summary>
		bool                           IsNullable          { get; }

		/// <summary>
		/// Value or reference type.
		/// </summary>
		bool                           IsValueType         { get; }

		/// <summary>
		/// Type namespace.
		/// </summary>
		IReadOnlyList<CodeIdentifier>? Namespace           { get; }

		/// <summary>
		/// Parent type for nested types.
		/// </summary>
		IType?                         Parent              { get; }

		/// <summary>
		/// Type name.
		/// </summary>
		CodeIdentifier?                Name                { get; }

		/// <summary>
		/// Type of array element for array type.
		/// </summary>
		IType?                         ArrayElementType    { get; }

		/// <summary>
		/// Optional array sizes for array type.
		/// Use array as property type to support multi-dimensional arrays.
		/// </summary>
		IReadOnlyList<int?>?           ArraySizes          { get; }

		// unused currently
		/// <summary>
		/// Number of type arguments for open generic type.
		/// </summary>
		int?                           OpenGenericArgCount { get; }

		/// <summary>
		/// Type arguments for generic type.
		/// </summary>
		IReadOnlyList<IType>?          TypeArguments       { get; }

		/// <summary>
		/// Returns <see langword="true" /> if type defined in external code and <see langword="false"/>, when type defined in current AST (as class).
		/// </summary>
		bool                           External            { get; }

		/// <summary>
		/// Apply nullability flag to current type.
		/// </summary>
		/// <param name="nullable">New type nullability status.</param>
		/// <returns>New type instance if nullability changed.</returns>
		IType WithNullability(bool nullable);

		/// <summary>
		/// Specify type arguments for open generic type.
		/// Method call valid only on open-generic types.
		/// </summary>
		/// <param name="typeArguments">Types to use as generic type arguments.</param>
		/// <returns>Generic type with provided type arguments.</returns>
		IType WithTypeArguments(params IType[] typeArguments);
	}
}
