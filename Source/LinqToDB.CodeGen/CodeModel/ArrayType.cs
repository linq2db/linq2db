using System;

namespace LinqToDB.CodeGen.CodeModel
{
	public class ArrayType : IType
	{
		private readonly IType _elementType;
		private readonly int?[] _sizes;
		private readonly bool _isNullable;

		public ArrayType(IType elementType, int?[] sizes, bool nullable)
		{
			_elementType = elementType;
			_sizes = sizes;
			_isNullable = nullable;
		}

		public TypeKind Kind => TypeKind.Array;

		public bool IsNullable => _isNullable;

		public bool IsValueType => false;

		// TODO: in theory we need it for languages without array type syntax
		public CodeIdentifier[]? Namespace => null;

		public bool IsAlias => false;

		public CodeIdentifier? Name => null;

		public int? OpenGenericArgCount => null;

		public IType[]? TypeArguments => null;

		public CodeElementType ElementType => CodeElementType.TypeReference;

		public int?[] ArraySizes => _sizes;

		public IType ArrayElementType => _elementType;

		public IType? Parent => null;

		public bool External => ArrayElementType.External;

		public IType WithNullability(bool nullable)
		{
			return new ArrayType(_elementType, _sizes, nullable);
		}

		public IType WithTypeArguments(IType[] typeArguments)
		{
			throw new InvalidOperationException();
		}
	}
}
