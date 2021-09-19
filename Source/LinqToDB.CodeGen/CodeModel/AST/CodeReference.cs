namespace LinqToDB.CodeGen.Model
{
	// right now we don't need to differentiate between variable or parameter in reference
	/// <summary>
	/// Parameter or variable access expression.
	/// </summary>
	public sealed class CodeReference : ICodeExpression, ILValue
	{
		/// <summary>
		/// Create parameter or variable reference (access expression).
		/// </summary>
		/// <param name="referenced">Parameter or variable to reference.</param>
		public CodeReference(ITypedName referenced)
		{
			Referenced = referenced;
		}

		/// <summary>
		/// Referenced named object with type (parameter, variable, property or field).
		/// </summary>
		public ITypedName Referenced { get; }

		IType ICodeExpression.Type => Referenced.Type.Type;

		CodeElementType ICodeElement.ElementType => CodeElementType.Reference;
	}
}
