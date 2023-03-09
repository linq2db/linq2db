namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Empty line element. Used for explicit formatting.
	/// </summary>
	public sealed class CodeEmptyLine : ICodeStatement, ITopLevelElement
	{
		public static readonly CodeEmptyLine Instance = new ();
		
		private CodeEmptyLine()
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.EmptyLine;
	}
}
