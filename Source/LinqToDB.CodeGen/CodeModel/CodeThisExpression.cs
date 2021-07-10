namespace LinqToDB.CodeGen.CodeModel
{
	public class CodeThisExpression : ICodeExpression
	{
		public static readonly CodeThisExpression Instance = new ();

		public CodeElementType ElementType => CodeElementType.This;
	}

}
