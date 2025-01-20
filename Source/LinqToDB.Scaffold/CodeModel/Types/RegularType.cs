using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Implementation of non-generic type.
	/// </summary>
	internal class RegularType : IType
	{
		private readonly bool                           _nullable;
		private readonly bool                           _valueType;
		private readonly bool                           _external;
		private readonly IReadOnlyList<CodeIdentifier>? _ns;
		private readonly CodeIdentifier                 _name;
		private readonly IType?                         _parent;

		/// <summary>
		/// Creates top-level (no namespace) or namespaced type descriptor.
		/// </summary>
		/// <param name="namespace">Optional containing namespace.</param>
		/// <param name="name">Type name.</param>
		/// <param name="isValueType">Value or reference type.</param>
		/// <param name="isNullable">Nullability status.</param>
		/// <param name="external">Type defined externally or in current AST.</param>
		public RegularType(IReadOnlyList<CodeIdentifier>? @namespace, CodeIdentifier name, bool isValueType, bool isNullable, bool external)
		{
			_ns        = @namespace;
			_name      = name;
			_valueType = isValueType;
			_nullable  = isNullable;
			_external  = external;
		}

		/// <summary>
		/// Creates nested type descriptor.
		/// </summary>
		/// <param name="parent">Parent type.</param>
		/// <param name="name">Type name.</param>
		/// <param name="isValueType">Value or reference type.</param>
		/// <param name="isNullable">Nullability status.</param>
		/// <param name="external">Type defined externally or in current AST.</param>
		public RegularType(IType parent, CodeIdentifier name, bool isValueType, bool isNullable, bool external)
		{
			_parent    = parent;
			_name      = name;
			_valueType = isValueType;
			_nullable  = isNullable;
			_external  = external;
		}

		public virtual TypeKind                       Kind        => TypeKind.Regular;
		public         bool                           IsNullable  => _nullable;
		public         IType?                         Parent      => _parent;
		public         CodeIdentifier                 Name        => _name;
		public         bool                           IsValueType => _valueType;
		public         bool                           External    => _external;
		public         IReadOnlyList<CodeIdentifier>? Namespace   => _ns;

		public virtual IType WithNullability(bool nullable)
		{
			if (nullable == _nullable)
				return this;

			if (_parent != null)
				return new RegularType(_parent, _name, _valueType, nullable, _external);

			return new RegularType(_ns, _name, _valueType, nullable, _external);
		}

		// not valid for current type kind
		public virtual IReadOnlyList<IType>? TypeArguments       => null;
		public virtual int?                  OpenGenericArgCount => null;

		IType?               IType.ArrayElementType => null;
		IReadOnlyList<int?>? IType.ArraySizes       => null;

		public virtual IType WithTypeArguments(params IType[] typeArguments) => throw new InvalidOperationException();
	}
}
