using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Implementation of generic type.
	/// </summary>
	internal sealed class GenericType : RegularType
	{
		private readonly IReadOnlyList<IType> _typeArguments;

		/// <summary>
		/// Creates top-level (no namespace) or namespaced type descriptor.
		/// </summary>
		/// <param name="namespace">Optional containing namespace.</param>
		/// <param name="name">Type name.</param>
		/// <param name="isValueType">Value or reference type.</param>
		/// <param name="isNullable">Nullability status.</param>
		/// <param name="typeArguments">Type arguments.</param>
		/// <param name="external">Type defined externally or in current AST.</param>
		public GenericType(
			IReadOnlyList<CodeIdentifier>? @namespace,
			CodeIdentifier                 name,
			bool                           isValueType,
			bool                           isNullable,
			IReadOnlyList<IType>           typeArguments,
			bool                           external)
			: base(@namespace, name, isValueType, isNullable, external)
		{
			_typeArguments = typeArguments;
		}

		/// <summary>
		/// Creates nested type descriptor.
		/// </summary>
		/// <param name="parent">Parent type.</param>
		/// <param name="name">Type name.</param>
		/// <param name="isValueType">Value or reference type.</param>
		/// <param name="isNullable">Nullability status.</param>
		/// <param name="typeArguments">Type arguments.</param>
		/// <param name="external">Type defined externally or in current AST.</param>
		public GenericType(
			IType                parent,
			CodeIdentifier       name,
			bool                 isValueType,
			bool                 isNullable,
			IReadOnlyList<IType> typeArguments,
			bool                 external)
			: base(parent, name, isValueType, isNullable, external)
		{
			_typeArguments = typeArguments;
		}

		public override TypeKind             Kind          => TypeKind.Generic;
		public override IReadOnlyList<IType> TypeArguments => _typeArguments;

		// is it even valid on open generic?
		public override IType WithNullability(bool nullable)
		{
			if (nullable == IsNullable)
				return this;

			if (Parent != null)
				return new GenericType(Parent, Name, IsValueType, nullable, _typeArguments, External);

			return new GenericType(Namespace, Name, IsValueType, nullable, _typeArguments, External);
		}
	}
}
