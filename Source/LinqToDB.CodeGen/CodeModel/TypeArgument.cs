using System;

namespace LinqToDB.CodeGen.CodeModel
{
	// TODO: add constrains and type arguments support?
	public class TypeArgument : IType
	{
		public TypeArgument(CodeIdentifier name, bool nullable)
		{
			Name = name;
			IsNullable = nullable;
		}

		public TypeKind Kind => TypeKind.TypeArgument;

		public bool IsNullable { get; }

		public bool IsValueType => false;

		public CodeIdentifier[]? Namespace => null;

		public IType? Parent => null;

		public bool IsAlias => false;

		public CodeIdentifier Name { get; }

		public IType? ArrayElementType => null;

		public int?[]? ArraySizes => null;

		public int? OpenGenericArgCount => null;

		public IType[]? TypeArguments => null;

		public bool External => false;

		public IType WithNullability(bool nullable)
		{
			if (IsNullable == nullable)
				return this;

			return new TypeArgument(Name, nullable);
		}

		public IType WithTypeArguments(IType[] typeArguments)
		{
			throw new InvalidOperationException();
		}
	}
}
