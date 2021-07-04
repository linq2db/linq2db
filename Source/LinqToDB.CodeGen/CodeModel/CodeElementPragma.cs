namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeElementPragma : ITopLevelCodeElement, IMemberElement
	{
		public CodeElementPragma(PragmaType type, string[] parameters)
		{
			PragmaType = type;
			Parameters = parameters;
		}

		public PragmaType PragmaType { get; }
		public string[] Parameters { get; }

		public CodeElementType ElementType => CodeElementType.Pragma;
	}
}
