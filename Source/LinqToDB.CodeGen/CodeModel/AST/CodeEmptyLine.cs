namespace LinqToDB.CodeGen.Model
{
	// TODO: remove
	/// <summary>
	/// Empty line element. Used for explicit formatting.
	/// </summary>
	public sealed class CodeEmptyLine : ITopLevelElement
	{
		public static readonly ITopLevelElement Instance = new CodeEmptyLine();
		
		private CodeEmptyLine()
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.EmptyLine;
	}
}
