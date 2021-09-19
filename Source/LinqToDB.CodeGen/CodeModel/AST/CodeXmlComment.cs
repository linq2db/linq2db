using System.Collections.Generic;

namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// XML-doc commentary.
	/// </summary>
	public sealed class CodeXmlComment : ICodeElement
	{
		internal CodeXmlComment(string? summary, List<(CodeIdentifier parameter, string text)>? parameters)
		{
			Summary    = summary;
			Parameters = parameters ?? new ();
		}

		public CodeXmlComment()
			: this(null, null)
		{
		}

		/// <summary>
		/// Summary documentation element.
		/// </summary>
		public string?                                       Summary    { get; internal set; }
		/// <summary>
		/// Documentation for method/constructor parameters.
		/// </summary>
		public List<(CodeIdentifier parameter, string text)> Parameters { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.XmlComment;
	}
}
