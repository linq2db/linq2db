using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Implementation of array type definition.
	/// </summary>
	internal sealed class ArrayType : IType
	{
		private readonly IType               _elementType;
		private readonly IReadOnlyList<int?> _sizes;
		private readonly bool                _isNullable;

		public ArrayType(IType elementType, IReadOnlyList<int?> sizes, bool nullable)
		{
			_elementType = elementType;
			_sizes       = sizes;
			_isNullable  = nullable;
		}

		TypeKind            IType.Kind             => TypeKind.Array;
		bool                IType.IsNullable       => _isNullable;
		IReadOnlyList<int?> IType.ArraySizes       => _sizes;
		IType               IType.ArrayElementType => _elementType;
		bool                IType.IsValueType      => false;
		bool                IType.External         => true;

		IType IType.WithNullability(bool nullable) => new ArrayType(_elementType, _sizes, nullable);

		// not applicable to array
		IReadOnlyList<CodeIdentifier>? IType.Namespace           => null;
		CodeIdentifier?                IType.Name                => null;
		int?                           IType.OpenGenericArgCount => null;
		IReadOnlyList<IType>?          IType.TypeArguments       => null;
		IType?                         IType.Parent              => null;
		

		IType IType.WithTypeArguments(params IType[] typeArguments) => throw new InvalidOperationException();
	}
}
