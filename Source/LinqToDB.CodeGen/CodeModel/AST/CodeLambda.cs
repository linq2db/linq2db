using System.Collections.Generic;

namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Lambda method.
	/// </summary>
	public sealed class CodeLambda : MethodBase, ICodeExpression
	{
		public CodeLambda(
			IEnumerable<CodeAttribute>? customAttributes,
			Modifiers                   attributes,
			CodeBlock?                  body,
			CodeXmlComment?             xmlDoc,
			IEnumerable<CodeParameter>? parameters,
			IType                       targetType,
			bool                        canOmmitTypes)
			: base(customAttributes, attributes, body, xmlDoc, parameters)
		{
			TargetType    = targetType;
			CanOmmitTypes = canOmmitTypes;
		}

		public CodeLambda(IType targetType, bool canOmmitTypes)
			: this(null, default, null, null, null, targetType, canOmmitTypes)
		{
		}

		/// <summary>
		/// Type of lambda expression. Defined by target location (e.g. by type of method parameter, that accepts lambda).
		/// </summary>
		public IType TargetType    { get; }

		/// <summary>
		/// Specify, that generated code could exclude parameter types in generated code.
		/// </summary>
		public bool  CanOmmitTypes { get; }

		IType ICodeExpression.Type => TargetType;

		public override CodeElementType ElementType => CodeElementType.Lambda;
	}
}
