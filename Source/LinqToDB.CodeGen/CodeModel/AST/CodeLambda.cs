namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Lambda method.
	/// </summary>
	public class CodeLambda : MethodBase, ICodeExpression
	{
		public override CodeElementType ElementType => CodeElementType.Lambda;
	}
}
