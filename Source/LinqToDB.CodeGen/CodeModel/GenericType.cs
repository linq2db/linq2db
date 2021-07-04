namespace LinqToDB.CodeGen.CodeModel
{
	public class GenericType : RegularType
	{
		private readonly IType[] _typeArguments;

		public GenericType(CodeIdentifier[]? @namespace, CodeIdentifier name, bool isAlias, bool isValueType, bool isNullable, IType[] typeArguments, bool external)
			: base(@namespace, name, isAlias, isValueType, isNullable, external)
		{
			_typeArguments = typeArguments;
		}

		public GenericType(IType parent, CodeIdentifier name, bool isAlias, bool isValueType, bool isNullable, IType[] typeArguments, bool external)
			: base(parent, name, isAlias, isValueType, isNullable, external)
		{
			_typeArguments = typeArguments;
		}

		public override TypeKind Kind => TypeKind.Generic;

		public override IType[] TypeArguments => _typeArguments;

		// is it even valid on open generic?
		public override IType WithNullability(bool nullable)
		{
			if (nullable == IsNullable)
				return this;

			return new GenericType(Namespace, Name, IsAlias, IsValueType, nullable, _typeArguments, External);
		}
	}
}
