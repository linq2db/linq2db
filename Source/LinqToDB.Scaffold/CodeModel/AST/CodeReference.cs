namespace LinqToDB.CodeModel
{
	// right now we don't need to differentiate between variable or parameter in reference
	/// <summary>
	/// Defines reference to parameter or variable inside of current method/property or simple (without owner type/instance) reference to field/property.
	/// </summary>
	public sealed class CodeReference : ICodeExpression, ILValue
	{
		/// <summary>
		/// Create parameter, variable, field or property reference (access expression).
		/// </summary>
		/// <param name="referenced">Parameter, variable, field or property to reference.</param>
		public CodeReference(ITypedName referenced)
		{
			Referenced = referenced;
		}

		/// <summary>
		/// Referenced named object with type (parameter, variable, property or field).
		/// </summary>
		public ITypedName Referenced { get; }

		IType           ICodeExpression.Type        => Referenced.Type.Type;
		CodeElementType ICodeElement   .ElementType => CodeElementType.Reference;
	}
}
