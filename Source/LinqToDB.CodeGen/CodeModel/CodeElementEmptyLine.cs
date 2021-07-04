namespace LinqToDB.CodeGen.CodeModel
{
	// TODO: remove
	public class CodeElementEmptyLine : ITopLevelCodeElement
	{
		public CodeElementType ElementType => CodeElementType.EmptyLine;

		public static readonly CodeElementEmptyLine Instance = new ();
		
		private CodeElementEmptyLine()
		{
		}
	}
}
