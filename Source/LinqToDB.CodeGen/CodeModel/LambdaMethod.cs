namespace LinqToDB.CodeGen.CodeModel
{
	public class LambdaMethod : CodeElementMethodBase, ICodeExpression
	{
		public override CodeElementType ElementType => CodeElementType.Lambda;
	}
}
