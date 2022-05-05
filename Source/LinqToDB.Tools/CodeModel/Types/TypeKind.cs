namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Type kind.
	/// </summary>
	public enum TypeKind
	{
		/// <summary>
		/// Array type.
		/// </summary>
		Array,
		/// <summary>
		/// Dynamic type.
		/// </summary>
		Dynamic,
		/// <summary>
		/// Non-generic type.
		/// </summary>
		Regular,
		/// <summary>
		/// Generic type with known type arguments.
		/// </summary>
		Generic,
		/// <summary>
		/// Generic type definition (without type arguments).
		/// </summary>
		OpenGeneric,
		/// <summary>
		/// Generic type argument.
		/// </summary>
		TypeArgument
	}
}
