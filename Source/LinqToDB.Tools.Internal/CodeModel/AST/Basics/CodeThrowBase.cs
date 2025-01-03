namespace LinqToDB.CodeModel
{
	/// <summary>
	/// Throw expression.
	/// </summary>
	public abstract class CodeThrowBase
	{
		protected CodeThrowBase(ICodeExpression exception)
		{
			Exception = exception;
		}

		/// <summary>
		/// Exception object.
		/// </summary>
		public ICodeExpression Exception { get; }
	}
}
