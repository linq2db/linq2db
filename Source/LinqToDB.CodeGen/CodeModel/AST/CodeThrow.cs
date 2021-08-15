namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Throw expression or statement.
	/// </summary>
	public class CodeThrow : ICodeExpression, ICodeStatement
	{
		public CodeThrow(ICodeExpression exception)
		{
			Exception = exception;
		}

		/// <summary>
		/// Exception object.
		/// </summary>
		public ICodeExpression Exception { get; }

		CodeElementType ICodeElement.ElementType => CodeElementType.Throw;
	}
}
