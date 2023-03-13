namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Empty line element. Used for explicit formatting.
	/// </summary>
	public sealed class CodeEmptyLine : ITopLevelElement
	{
		public static readonly CodeEmptyLine Instance = new ();
		
		private CodeEmptyLine()
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.EmptyLine;
	}
}
