namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// <c>this</c> reference.
	/// </summary>
	public class CodeThis : ICodeExpression
	{
		private CodeThis() { }

		public static readonly ICodeExpression Instance = new CodeThis();

		CodeElementType ICodeElement.ElementType => CodeElementType.This;
	}
}
