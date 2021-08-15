using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// XML-doc commentary.
	/// </summary>
	public class CodeXmlComment : ICodeElement
	{
		/// <summary>
		/// Summary documentation element.
		/// </summary>
		public string?                                       Summary    { get; internal set; }
		/// <summary>
		/// Documentation for method/constructor parameters.
		/// </summary>
		public List<(CodeIdentifier parameter, string text)> Parameters { get; } = new ();

		CodeElementType ICodeElement.ElementType => CodeElementType.XmlComment;
	}
}
