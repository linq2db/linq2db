namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Variable declaration.
	/// </summary>
	public class CodeVariable : ILValue
	{
		public CodeVariable(CodeIdentifier name, IType type, bool rvalueTyped)
		{
			Name        = name;
			Type        = new (type);
			RValueTyped = rvalueTyped;
		}

		/// <summary>
		/// Variable name.
		/// </summary>
		public CodeIdentifier Name        { get; }
		/// <summary>
		/// Variable type.
		/// </summary>
		public CodeTypeToken  Type        { get; }
		/// <summary>
		/// Indicates that variable type could be infered from assigned value.
		/// This could be used to generate <c>var</c> instead of specific type.
		/// </summary>
		public bool           RValueTyped { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Variable;
	}
}
