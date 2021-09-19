using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Class method definition.
	/// </summary>
	public sealed class CodeMethod : MethodBase, IGroupElement
	{
		public CodeMethod(
			List<CodeAttribute>? customAttributes,
			Modifiers            attributes,
			CodeBlock?           body,
			CodeXmlComment?      xmlDoc,
			List<CodeParameter>? parameters,
			CodeIdentifier       name,
			CodeTypeToken?       returnType,
			List<CodeTypeToken>? typeParameters)
			: base(customAttributes, attributes, body, xmlDoc, parameters)
		{
			Name           = name;
			ReturnType     = returnType;
			TypeParameters = typeParameters ?? new();
		}

		public CodeMethod(CodeIdentifier name)
			: this(null, default, null, null, null, name, null, null)
		{
		}

		/// <summary>
		/// Method name.
		/// </summary>
		public CodeIdentifier      Name           { get; }
		/// <summary>
		/// Method return type.
		/// <c>null</c> for void methods.
		/// </summary>
		public CodeTypeToken?      ReturnType     { get; internal set; }
		/// <summary>
		/// Generic method type parameters.
		/// </summary>
		public List<CodeTypeToken> TypeParameters { get; }

		public override CodeElementType ElementType => CodeElementType.Method;
	}
}
