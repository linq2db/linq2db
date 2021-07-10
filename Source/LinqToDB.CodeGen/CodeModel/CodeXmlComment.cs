using System.Collections.Generic;

namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeXmlComment : ICodeElement
	{
		public string? Summary { get; set; }

		public List<(CodeIdentifier parameter, string text)> Parameters { get; } = new ();

		public CodeElementType ElementType => CodeElementType.XmlComment;
	}
}
