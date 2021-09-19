namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Throw statement.
	/// </summary>
	public sealed class CodeThrowStatement : CodeThrowBase, ICodeStatement
	{
		public CodeThrowStatement(ICodeExpression exception)
			: base(exception)
		{
		}

		CodeElementType ICodeElement.ElementType => CodeElementType.ThrowStatement;
	}
}
