using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Type initializer (static constructor).
	/// </summary>
	public sealed class CodeTypeInitializer : MethodBase
	{
		internal CodeTypeInitializer(
			IEnumerable<CodeAttribute>? customAttributes,
			Modifiers                   attributes,
			CodeBlock?                  body,
			CodeXmlComment?             xmlDoc,
			IEnumerable<CodeParameter>? parameters,
			CodeClass                   type)
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
