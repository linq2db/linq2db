namespace LinqToDB.CodeGen.CodeModel
{
	public interface IType
	{
		// shared attributes (all kinds)
		TypeKind Kind { get; }
		bool IsNullable { get; }
		bool IsValueType { get; }
		IType WithNullability(bool nullable);

		// kind: regular
		CodeIdentifier[]? Namespace { get; }
		IType? Parent { get; }
		bool IsAlias { get; }
		CodeIdentifier? Name { get; }

		// arrays
		IType? ArrayElementType { get; }
		int?[]? ArraySizes { get; }

		// open generic
		int? OpenGenericArgCount { get; }
		IType WithTypeArguments(IType[] typeArguments);

		// generic
		IType[]? TypeArguments { get; }

		// type, defined in code codel (currently classes only)
		bool External { get; }
	}
}
