using System;

namespace LinqToDB.CodeGen.CodeModel
{
	public class RegularType : IType
	{
		private readonly bool _nullable;
		private readonly bool _alias;
		private readonly bool _valueType;
		private readonly CodeIdentifier[]? _ns;
		private readonly CodeIdentifier _name;
		private readonly IType? _parent;

		public RegularType(CodeIdentifier[]? @namespace, CodeIdentifier name, bool isAlias, bool isValueType, bool isNullable, bool external)
		{
			_ns = @namespace;
			_name = name;
			_alias = isAlias;
			_valueType = isValueType;
			_nullable = isNullable;
			External = external;
		}

		public RegularType(IType parent, CodeIdentifier name, bool isAlias, bool isValueType, bool isNullable, bool external)
		{
			_parent = parent;
			_name = name;
			_alias = isAlias;
			_valueType = isValueType;
			_nullable = isNullable;
			External = external;
		}

		public IType? Parent => _parent;

		public virtual TypeKind Kind => TypeKind.Regular;

		public bool IsNullable => _nullable;

		public bool IsValueType => _valueType;

		public CodeIdentifier[]? Namespace => _ns;

		public bool IsAlias => _alias;

		public CodeIdentifier Name => _name;

		public virtual IType[]? TypeArguments => null;

		public virtual int? OpenGenericArgCount => null;

		public IType? ArrayElementType => null;

		public int?[]? ArraySizes => null;

		public bool External { get; }

		public virtual IType WithNullability(bool nullable)
		{
			if (nullable == _nullable)
				return this;

			return new RegularType(_ns, _name, _alias, _valueType, nullable, External);
		}

		public virtual IType WithTypeArguments(IType[] typeArguments)
		{
			throw new InvalidOperationException();
		}
	}
}
