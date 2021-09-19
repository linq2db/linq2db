using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Type initializer (static constructor).
	/// </summary>
	public sealed class CodeTypeInitializer : MethodBase
	{
		internal CodeTypeInitializer(
			List<CodeAttribute>? customAttributes,
			Modifiers            attributes,
			CodeBlock?           body,
			CodeXmlComment?      xmlDoc,
			List<CodeParameter>? parameters,
			CodeClass            type)
			: base(customAttributes, attributes, body, xmlDoc, parameters)
		{
			Type = type;
		}

		public CodeTypeInitializer(CodeClass type)
			: this(null, Modifiers.None, null, null, null, type)
		{
		}

		/// <summary>
		/// Owner class.
		/// </summary>
		public CodeClass Type { get; }

		public override CodeElementType ElementType => CodeElementType.TypeConstructor;
	}
}
