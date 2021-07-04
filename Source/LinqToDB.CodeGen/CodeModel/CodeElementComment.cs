namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeElementComment : ITopLevelCodeElement
	{
		public CodeElementComment(string text, bool inline)
		{
			Text = text;
			Inline = inline;
		}

		public string Text { get; }
		public bool Inline { get; }

		public CodeElementType ElementType => CodeElementType.Comment;
	}
}
