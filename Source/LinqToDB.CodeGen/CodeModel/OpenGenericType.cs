using System;

namespace LinqToDB.CodeGen.CodeModel
{
	public class OpenGenericType : RegularType
	{
		private readonly int _size;

		public OpenGenericType(CodeIdentifier[]? @namespace, CodeIdentifier name, bool isAlias, bool isValueType, bool isNullable, int argCount, bool external)
			: base(@namespace, name, isAlias, isValueType, isNullable, external)
		{
			_size = argCount;
		}

		public OpenGenericType(IType parent, CodeIdentifier name, bool isAlias, bool isValueType, bool isNullable, int argCount, bool external)
			: base(parent, name, isAlias, isValueType, isNullable, external)
		{
			_size = argCount;
		}

		public override TypeKind Kind => TypeKind.OpenGeneric;

		public override int? OpenGenericArgCount => _size;

		// is it even valid on open generic?
		public override IType WithNullability(bool nullable)
		{
			if (nullable == IsNullable)
				return this;

			return new OpenGenericType(Namespace, Name, IsAlias, IsValueType, nullable, _size, External);
		}

		public override IType WithTypeArguments(IType[] typeArguments)
		{
			if (typeArguments.Length != _size)
				throw new InvalidOperationException();

			return new GenericType(Namespace, Name, IsAlias, IsValueType, IsNullable, typeArguments, External);
		}
	}
}
