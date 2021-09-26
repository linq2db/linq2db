using System;
using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	// TODO: add constrains and type arguments support?
	/// <summary>
	/// Implementation of generic type argument type definition.
	/// </summary>
	public class TypeArgument : IType
	{
		private readonly bool           _isNullable;
		private readonly CodeIdentifier _name;

		public TypeArgument(CodeIdentifier name, bool nullable)
		{
			_name       = name;
			_isNullable = nullable;
		}

		TypeKind       IType.Kind       => TypeKind.TypeArgument;
		bool           IType.IsNullable => _isNullable;
		CodeIdentifier IType.Name       => _name;

		IType IType.WithNullability(bool nullable)
		{
			if (_isNullable == nullable)
				return this;

			return new TypeArgument(_name, nullable);
		}

		// not applicable to current type
		IType?                         IType.ArrayElementType    => null;
		IReadOnlyList<int?>?           IType.ArraySizes          => null;
		int?                           IType.OpenGenericArgCount => null;
		IReadOnlyList<IType>?          IType.TypeArguments       => null;
		bool                           IType.External            => false;
		bool                           IType.IsValueType         => false;
		IReadOnlyList<CodeIdentifier>? IType.Namespace           => null;
		IType?                         IType.Parent              => null;

		IType IType.WithTypeArguments(params IType[] typeArguments) => throw new InvalidOperationException();
	}
}
