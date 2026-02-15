namespace LinqToDB.CodeModel
{
	// Types split into groups by node kind.
	// Also note that AST could miss many language elements, because they are not used by code generator right now
	// and we definitely do not have a goal to provide full-scale code generation API for every purpose - we have
	// quite clear scope for this API.
	/// <summary>
	/// Element types for code AST elements.
	/// </summary>
	public enum CodeElementType
	{
		// non-code nodes (infrastructure)

		/// <summary>
		/// Element, corresponding to single code unit (file).
		/// </summary>
		File,

		// nodes, that could be used as top-level nodes in file

		/// <summary>
		/// Commentary node.
		/// </summary>
		Comment,
		/// <summary>
		/// Compiler pragma.
		/// </summary>
		Pragma,
		/// <summary>
		/// Import (using) statement.
		/// </summary>
		Import,
		/// <summary>
		/// Represents empty line (used for formatting puroses only).
		/// </summary>
		EmptyLine,
		/// <summary>
		/// Namespace declaration.
		/// </summary>
		Namespace,
		/// <summary>
		/// Custom attribute.
		/// </summary>
		Attribute,
		/// <summary>
		/// Code region.
		/// </summary>
		Region,

		// types
		// for now we generate only classes, so model doesn't include other types

		/// <summary>
		/// Class declaration.
		/// </summary>
		Class,

		// type members

		/// <summary>
		/// Property declaration.
		/// </summary>
		Property,
		/// <summary>
		/// Instance constructor declaration.
		/// </summary>
		Constructor,
		/// <summary>
		/// Type constructor (static constructor) declaration.
		/// </summary>
		TypeConstructor,
		/// <summary>
		/// Method declaration.
		/// </summary>
		Method,
		/// <summary>
		/// Field declaration.
		/// </summary>
		Field,

		// member/element groups
		// group of elements of same type, that could be e.g. rendered with same alignment

		/// <summary>
		/// Group of code regions.
		/// </summary>
		RegionGroup,
		/// <summary>
		/// Group of pragma directives.
		/// </summary>
		PragmaGroup,
		/// <summary>
		/// Group of classes
		/// </summary>
		ClassGroup,
		/// <summary>
		/// Group of methods.
		/// </summary>
		MethodGroup,
		/// <summary>
		/// Group of construcrtors.
		/// </summary>
		ConstructorGroup,
		/// <summary>
		/// Group of properties.
		/// </summary>
		PropertyGroup,
		/// <summary>
		/// Group of fields.
		/// </summary>
		FieldGroup,

		// some generic elements

		/// <summary>
		/// Identifier (name of code element).
		/// </summary>
		Identifier,
		/// <summary>
		/// Commentary in xml-doc format.
		/// </summary>
		XmlComment,
		/// <summary>
		/// Method of method (including all types of methods like constructors and lamdas).
		/// </summary>
		Parameter,
		/// <summary>
		/// Type reference used in type-only context (e.g. field type or parameter type).
		/// For type reference used as expression see <see cref="TypeReference"/>.
		/// </summary>
		TypeToken,

		// statements

		/// <summary>
		/// Return statement.
		/// </summary>
		ReturnStatement,
		/// <summary>
		/// Throw statement.
		/// </summary>
		ThrowStatement,
		/// <summary>
		/// Method call statement.
		/// </summary>
		CallStatement,
		/// <summary>
		/// Assignment statement.
		/// </summary>
		AssignmentStatement,
		/// <summary>
		/// Await statement.
		/// </summary>
		AwaitStatement,

		// expressions

		/// <summary>
		/// Type reference, used in expression context (e.g. in nameof expression or as object element of static member access).
		/// </summary>
		TypeReference,
		/// <summary>
		/// Lambda function.
		/// </summary>
		Lambda,
		/// <summary>
		/// Constant value/literal (including <see langword="null"/>).
		/// </summary>
		Constant,
		/// <summary>
		/// 'This' object reference in instance type members.
		/// </summary>
		This,
		/// <summary>
		/// Unary operation.
		/// </summary>
		UnaryOperation,
		/// <summary>
		/// Binary operation.
		/// </summary>
		BinaryOperation,
		/// <summary>
		/// Ternary operation.
		/// </summary>
		TernaryOperation,
		/// <summary>
		/// Type member access.
		/// </summary>
		MemberAccess,
		/// <summary>
		/// Parameter or variable reference expression.
		/// </summary>
		Reference,
		/// <summary>
		/// Nameof expression.
		/// </summary>
		NameOf,
		/// <summary>
		/// New object creation expression.
		/// </summary>
		New,
		/// <summary>
		/// Assignment expression.
		/// </summary>
		AssignmentExpression,
		/// <summary>
		/// Default value expression.
		/// </summary>
		Default,
		/// <summary>
		/// Array instantiation (declaration).
		/// </summary>
		Array,
		/// <summary>
		/// Indexed access.
		/// </summary>
		Index,
		/// <summary>
		/// Type cast expression.
		/// </summary>
		Cast,
		/// <summary>
		/// <see langword="as"/> operator expression.
		/// </summary>
		AsOperator,
		/// <summary>
		/// <c>!</c> null-forgiving operator expression.
		/// </summary>
		SuppressNull,
		/// <summary>
		/// Method call expression.
		/// </summary>
		CallExpression,
		/// <summary>
		/// Throw expression.
		/// </summary>
		ThrowExpression,
		/// <summary>
		/// Await expression.
		/// </summary>
		AwaitExpression,

		// l-value nodes

		/// <summary>
		/// Variable declaration.
		/// </summary>
		Variable,

		/// <summary>
		/// External property or field declaration.
		/// </summary>
		ExternalPropertyOrField,
	}
}
