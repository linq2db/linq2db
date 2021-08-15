namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Return statement.
	/// </summary>
	public class CodeReturn : ICodeStatement
	{
		public CodeReturn(ICodeExpression? expression)
		{
			Expression = expression;
		}

		/// <summary>
		/// Optional return value.
		/// </summary>
		public ICodeExpression? Expression { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.ReturnStatement;
	}
}
