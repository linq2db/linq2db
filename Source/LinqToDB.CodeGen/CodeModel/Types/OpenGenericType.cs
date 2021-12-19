using System;
using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Implementation of open-generic type.
	/// </summary>
	internal sealed class OpenGenericType : RegularType
	{
		private readonly int _size;

		/// <summary>
		/// Creates top-level (no namespace) or namespaced type descriptor.
		/// </summary>
		/// <param name="namespace">Optional containing namespace.</param>
		/// <param name="name">Type name.</param>
		/// <param name="isValueType">Value or reference type.</param>
		/// <param name="isNullable">Nullability status.</param>
		/// <param name="argCount">Number of type arguments.</param>
		/// <param name="external">Type defined externally or in current AST.</param>
		public OpenGenericType(
			IReadOnlyList<CodeIdentifier>? @namespace,
			CodeIdentifier                 name,
			bool                           isValueType,
			bool                           isNullable,
			int                            argCount,
			bool                           external)
			: base(@namespace, name, isValueType, isNullable, external)
		{
			_size = argCount;
		}

		/// <summary>
		/// Creates nested type descriptor.
		/// </summary>
		/// <param name="parent">Parent type.</param>
		/// <param name="name">Type name.</param>
		/// <param name="isValueType">Value or reference type.</param>
		/// <param name="isNullable">Nullability status.</param>
		/// <param name="argCount">Number of type arguments.</param>
		/// <param name="external">Type defined externally or in current AST.</param>
		public OpenGenericType(
			IType          parent,
			CodeIdentifier name,
			bool           isValueType,
			bool           isNullable,
			int            argCount,
			bool           external)
			: base(parent, name, isValueType, isNullable, external)
		{
			_size = argCount;
		}

		public override TypeKind Kind                => TypeKind.OpenGeneric;
		public override int?     OpenGenericArgCount => _size;

		// is it even valid on open generic?
		public override IType WithNullability(bool nullable)
		{
			if (nullable == IsNullable)
				return this;

			if (Parent != null)
				return new OpenGenericType(Parent, Name, IsValueType, nullable, _size, External);

			return new OpenGenericType(Namespace, Name, IsValueType, nullable, _size, External);
		}

		public override IType WithTypeArguments(params IType[] typeArguments)
		{
			if (typeArguments.Length != _size)
				throw new InvalidOperationException();

			return new GenericType(Namespace, Name, IsValueType, IsNullable, typeArguments, External);
		}
	}
}
