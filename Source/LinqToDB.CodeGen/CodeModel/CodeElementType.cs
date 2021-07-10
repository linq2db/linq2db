namespace LinqToDB.CodeGen.CodeModel
{
	public enum CodeElementType
	{
		File,

		// top-level
		Comment,
		Pragma,
		Import,
		EmptyLine,
		Namespace,
		Attribute,
		Region,

		// types
		Class,

		// members
		Property,
		Constructor,
		Method,
		Lambda,
		Field,

		// generic
		TypeReference,
		Identifier,
		XmlComment,
		Parameter,
		Type,

		RegionGroup,
		MethodGroup,
		ConstructorGroup,
		PropertyGroup,
		ClassGroup,
		FieldGroup,
		PragmaGroup,

		// statements
		ReturnStatement,

		// expressions
		Constant,
		This,
		BinaryOperation,
		MemberAccess,
		NameOf,
		New,
		Assignment,
		Default,
		Variable,
		Array,
		Index,
		Throw,
		Cast,

		// statement/expression
		Call
	}
}
