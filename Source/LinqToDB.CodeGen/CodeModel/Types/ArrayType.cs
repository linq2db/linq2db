using System;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Implementation of array type definition.
	/// </summary>
	public class ArrayType : IType
	{
		private readonly IType  _elementType;
		private readonly int?[] _sizes;
		private readonly bool   _isNullable;

		public ArrayType(IType elementType, int?[] sizes, bool nullable)
		{
			_elementType = elementType;
			_sizes       = sizes;
			_isNullable  = nullable;
		}

		TypeKind IType.Kind             => TypeKind.Array;
		bool     IType.IsNullable       => _isNullable;
		int?[]   IType.ArraySizes       => _sizes;
		IType    IType.ArrayElementType => _elementType;
		bool     IType.External         => _elementType.External;
		bool     IType.IsValueType      => false;

		IType IType.WithNullability(bool nullable) => new ArrayType(_elementType, _sizes, nullable);

		// not applicable to array
		CodeIdentifier[]? IType.Namespace           => null;
		CodeIdentifier?   IType.Name                => null;
		int?              IType.OpenGenericArgCount => null;
		IType[]?          IType.TypeArguments       => null;
		IType?            IType.Parent              => null;
		string?           IType.Alias               => null;

		IType IType.WithTypeArguments(IType[] typeArguments) => throw new InvalidOperationException();
	}
}
