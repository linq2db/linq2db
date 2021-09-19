namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Compiler pragma directive.
	/// </summary>
	public sealed class CodePragma : ITopLevelElement, IGroupElement
	{
		public CodePragma(PragmaType type, string[] parameters)
		{
			PragmaType = type;
			Parameters = parameters;
		}

		/// <summary>
		/// Directive type.
		/// </summary>
		public PragmaType PragmaType { get; }
		/// <summary>
		/// Directive parameters.
		/// </summary>
		public string[]   Parameters { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Pragma;
	}
}
