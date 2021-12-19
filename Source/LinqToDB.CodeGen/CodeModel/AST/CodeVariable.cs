namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Variable declaration.
	/// </summary>
	public sealed class CodeVariable : CodeTypedName, ILValue
	{
		public CodeVariable(CodeIdentifier name, CodeTypeToken type, bool rvalueTyped)
			: base(name, type)
		{
			RValueTyped = rvalueTyped;
		}

		public CodeVariable(CodeIdentifier name, IType type, bool rvalueTyped)
			: this(name, new CodeTypeToken(type), rvalueTyped)
		{
		}

		/// <summary>
		/// Indicates that variable type could be infered from assigned value.
		/// This could be used to generate <c>var</c> instead of specific type.
		/// </summary>
		public bool RValueTyped { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Variable;
	}
}
