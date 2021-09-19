namespace LinqToDB.CodeGen.Model
{
	/// <summary>
	/// Throw expression.
	/// </summary>
	public abstract class CodeThrowBase
	{
		public CodeThrowBase(ICodeExpression exception)
		{
			Exception   = exception;
		}

		/// <summary>
		/// Exception object.
		/// </summary>
		public ICodeExpression Exception { get; }
	}
}
