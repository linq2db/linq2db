namespace LinqToDB.CodeModel
{
	public sealed class CodeComment : ITopLevelElement
	{
		public CodeComment(string text, bool inline)
		{
			Text   = text;
			Inline = inline;
		}

		/// <summary>
		/// Text of commentary.
		/// </summary>
		public string Text   { get; }
		/// <summary>
		/// Type of comment - inlined or single-line.
		/// </summary>
		public bool   Inline { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Comment;
	}
}
